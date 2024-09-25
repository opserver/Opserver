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

. $PSScriptRoot/app.ps1
. $PSScriptRoot/container-registry-discovery.ps1
. $PSScriptRoot/utils.ps1
. $PSScriptRoot/gcp-cluster-discovery.ps1


$app = Get-AppName
Write-Output "$($PSStyle.Bold)Installing $app...$($PSStyle.BoldOff)"

$action = $env:CNAB_ACTION

if (-not $env:INSTALLATION_METADATA) {
  throw "INSTALLATION_METADATA is not set"
}

Write-Verbose "INSTALLATION_METADATA is set to '$env:INSTALLATION_METADATA'"
$vars = (Get-Content $env:INSTALLATION_METADATA | ConvertFrom-Json) 
Initialize-Logging

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

$containerRegistryDetails = Find-ContainerRegistry $releaseTag $vars.vars.singleRegistry
$containerRegistryUrl = $containerRegistryDetails.Url
$pullSecretName = $containerRegistryDetails.PullSecretName
$forceUpgrade = $containerRegistryDetails.ForceUpgrade

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

if ($vars.runtime.name -eq "GCP") {
  Write-MajorStep "Finding Deployment Group and Deployment Targetr"
  $deploymentGroup = Find-DeploymentGroup $vars.deploymentDiscovery.deploymentGroupFilter
  $deploymentTarget = Find-DeploymentTarget $vars.deploymentDiscovery.deploymentTargetFilter $deploymentGroup
  
  if ($runAsContainer) {
    Write-MajorStep "Setting GCP cluster credentials"

    # Default cluster cred args
    $clusterCredArgs = @("container clusters get-credentials",
      $deploymentTarget.name,
      "--region", $deploymentTarget.location,
      "--project", $deploymentGroup)
    
    # Get cluster credentials
    Start-Process gcloud -ArgumentList $clusterCredArgs -NoNewWindow -Wait  
  }
}

switch ($action) {
  "pre-install" { Write-Output "Pre-install action" }
  "install" {
    Write-MajorStep "Install action"

    $valuesFileContent = Generate-Values $vars $environment $containerRegistryUrl $releaseTag $pullSecretName

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