#!/usr/bin/env pwsh
# Strict Mode
param(
  [bool]$runAsContainer = $true
)

#Requires -Version 7.4
Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

# Trap to ensure errors get shown as errors in Octopus Deploy, otherwise they'd go to Verbose because the
# 00-cnab-base.sh script sets '##octopus[stderr-progress]' to avoid all stderr showing up as errors.
# We'll assume we're running under Octopus Deploy if the error happens before we've determined $IsOctopusDeploy.
trap {
  if (!(Test-Path Variable:\IsOctopusDeploy) -or $IsOctopusDeploy) {
    Write-Host '##octopus[stderr-default]'
  }
  break
}

Write-Output "$($PSStyle.Bold)Installing opserver...$($PSStyle.BoldOff)"

$action = $env:CNAB_ACTION

if (-not $env:INSTALLATION_METADATA) {
  throw "INSTALLATION_METADATA is not set"
}

Write-Verbose "INSTALLATION_METADATA is set to '$env:INSTALLATION_METADATA'"
$vars = (Get-Content $env:INSTALLATION_METADATA | ConvertFrom-Json) 
. $PSScriptRoot/utils.ps1

if ($runAsContainer) {
  Write-Output "Running as container"
}
else {
  Write-Output "Running locally"
}

$tenant = $vars.pipeline.tenant
$environment = $vars.pipeline.environment
$project = $vars.pipeline.project

$releaseTag = $vars.pipeline.releaseTag

# PR container images are located in `cr-dev` in CloudSmith. As opposed to `cr` which we use for release builds.
$isPr = $releaseTag -match '^pr-[0-9]+$'
if ($isPr) {
  $containerRegistryUrl = 'cr.stackoverflow.software'
  $pullSecretName = 'cloudsmith-cr-prod'
  $forceUpgrade = @('--force') # This'll force pods to be recreated with freshly-pulled images
}
else {
  $containerRegistryUrl = 'cr.stackoverflow.software'
  $pullSecretName = 'cloudsmith-cr-prod'
  $forceUpgrade = @()
}

Write-Output "Container registry: $containerRegistryUrl"
Write-Output "Pull secret name: $pullSecretName"

function Invoke-WithEcho([string]$cmd) {
  Write-Output "$($PSStyle.Dim)> $cmd $args$($PSStyle.Reset)"
  & $cmd @args
}

Write-Output "Tool versions:"
Invoke-WithEcho gcloud version 
Invoke-WithEcho kubectl version --client=true
Invoke-WithEcho helm version --short
Invoke-WithEcho helm plugin list
Invoke-WithEcho helm diff version

# If the release tag starts with a commit hash plus dash "-", strip the extra characters. This lets us easily test Octopus pr's
$releaseTag = $releaseTag -replace '([a-z0-9]{40})-.*', '$1'

Write-MajorStep "Running $action for Tenant: $tenant - Environment: $environment - Project: $project in cloud: $($vars.runtime.name)"

if ($vars.runtime.name -eq "GCP" -and $runAsContainer) {
  
  Write-MajorStep "Setting GCP cluster credentials"

  # Default cluster cred args
  $clusterCredArgs = @("container clusters get-credentials",
    $vars.tenant_metadata.gke_cluster_name,
    "--region", $vars.tenant_metadata.region,
    "--project", $vars.tenant_metadata.project)
  
  # Get cluster credentials
  Start-Process gcloud -ArgumentList $clusterCredArgs -NoNewWindow -Wait
}

switch ($action) {
  "pre-install" { Write-Output "Pre-install action" }
  "install" {
    Write-MajorStep "Install action"

    $app = 'opserver'
    
    $values = @{
      tier                    = $environment
      replicaCount            = $vars.vars.replicaCount
      aspnetcoreEnvironment   = $vars.vars.aspnetcoreEnvironment
      exceptionalDbName       = $vars.vars.exceptionalDbName;
      product                 = "pubplat"

      images                  = @{
        containerRegistry = "$containerRegistryUrl"
        opserver         = @{
          tag = $releaseTag
        }
      }

      requests                = @{
        cpu    = $vars.vars.requestsCPU
        memory = $vars.vars.requestsMemory
      }
  
      limits                  = @{
        memory = $vars.vars.limitsMemory
      }
  
      podDisruptionBudget     = @{
        minAvailable = $vars.vars.podDisruptionBudgetMinAvailable
      }

      exceptional             = @{
        store = @{
          type = $vars.vars.exceptionalStoreType
        }
      }

      datadog                 = @{
        agentHost = $vars.vars.datadogAgentHost
        agentPort = $vars.vars.datadogAgentPort
      }
      
      kestrel                 = @{
        endPoints = @{
          http = @{
            url           = "http://0.0.0.0:8080/"
            containerPort = "8080"
          }
        }
      }

      secretStore             = @{
        fake = $vars.runtime.local
      }

      image                   = @{
        pullSecretName = $pullSecretName
      }

      ingress                 = @{
        className  = "nginx-internal"
        certIssuer = "letsencrypt-dns-prod"
        host       = $vars.vars.opserverSettings.hostUrl
        enabled    = $vars.vars.includeIngress
        secretName = "opserver-tls"
        createTlsCert = $true
      }

      sqlExternalSecret       = @{
        name            = "opserver-sqldb-external-secret"
        refreshInterval = "5m"
        storeRefName    = $vars.vars.secretStore
        targetName      = "sql-secret"
        remoteRefs      = @{
          exceptionalServerName = "ExceptionsSqlServerName"
          exceptionalUsername   = "db-opserver-User"
          exceptionalPassword   = "db-opserver-Password"
        }
      }

      opserverSettings       = $vars.vars.opserverSettings

      adminRolebindingGroupId = $vars.vars.adminRolebindingGroupId
    }
    
    # Helm expects a YAML file but YAML is also a superset of JSON, so we can use ConvertTo-Json here
    $valuesFileContent = $values | ConvertTo-Json -Depth 100
    Write-Output "Populated Helm values:"
    Write-Output $valuesFileContent

    $tmpDir = [System.IO.Directory]::CreateTempSubdirectory($app + '-')
    $valuesFilePath = (Join-Path $tmpDir.FullName 'populated-values.yml')
    
    $valuesFileContent > $valuesFilePath

    $folder = Get-ChildItem -Path $env:PWD -Filter "charts" -Directory -Recurse | Select-Object -First 1
    
    if ($folder) {
      $folder = $folder.FullName
    }
    else {
      throw "No 'charts' folder found in the filesystem."
    }
    
    Write-Output "CNAB Folder: $folder"

    # Generate a Helm chart diff to the console output to see what changes will happen
    Write-MinorStep "Printing Helm diff..."
    Invoke-WithEcho helm diff upgrade --debug $app $folder/$app/ `
      -f $valuesFilePath `
      --namespace $app `
      --install `
      --normalize-manifests
       
    # Invoke Helm upgrade
    Write-MinorStep "Running Helm upgrade..."
    Invoke-WithEcho helm upgrade --debug $app $folder/$app/ `
      -f $valuesFilePath `
      --namespace $app `
      --install `
      --create-namespace `
      --wait `
      --timeout 5m `
      @forceUpgrade

    Write-Output "$($PSStyle.Foreground.BrightGreen)Installation complete!$($PSStyle.Reset)"
  }

  "post-install" { Write-Output "Post-install action" }
  "uninstall" { Write-Output "Uninstall action" }
  "status" { Write-Output "Status action" }
  "human-intervention" {
    Write-Output "Human intervention action. Exiting with 5 as a test"
    exit 5
  }
  default { Write-Output "No action for $action" }
}

Write-Output "Action $action complete"