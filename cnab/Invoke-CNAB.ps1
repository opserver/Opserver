[CmdletBinding()]
param (
    [ValidateSet("pre-install", "install")]
    [string]
    $Action = "install",
    [bool]
    $RunAsContainer = $false,
    [ValidateSet("GCP", "DockerDesktop")]
    [string]
    $target = "GCP",
    [string]
    $CNABImage = "opserver-cnab:local"
)

# Function to check if a command exists
function Check-CommandExists {
    param (
        [string]$Command
    )

    if ($null -eq (Get-Command "$Command.exe" -ErrorAction SilentlyContinue)) { 
        Write-Host "Unable to find $Command. Please install $Command before continuing"
        exit 1
    }
}

function Setup-GCP {
    Check-CommandExists -Command "gcloud-crc32c"
    
    # Check if user is signed in to gcloud
    $gcloudAuthStatus = gcloud auth list --format="value(account)"
    if (-not $gcloudAuthStatus) {
        & gcloud auth login
    }
}

function Setup-DockerDesktop {
    Check-CommandExists -Command "kubectl"
    Check-CommandExists -Command "helm"

    & kubectl config use-context "docker-desktop"

    Write-Host "Check if Kubernetes is enabled in Docker Desktop"
    $output = & kubectl get node docker-desktop  2>&1 | Out-String
    
    if ( $output.Trim() -eq "Unable to connect to the server: EOF") {
        Write-Error "Kubernetes is not enabled in Docker Desktop. Please enable Kubernetes before continuing or change the target."
        exit 1
    }

    Write-Host "Kubernetes is enabled in Docker Desktop"
    Write-Host "Making sure prerequisites are installed"

    $repoExists = (helm repo list | Select-String -Pattern "external-secrets")
    $chartExists = (helm list -n external-secrets | Select-String -Pattern "external-secrets")

    if (-not $repoExists) {
        Write-Host "Adding external-secrets repo to Kubernetes"
        & helm repo add external-secrets https://charts.external-secrets.io
    }

    if (-not $chartExists) {
        Write-Host "Adding external-secrets chart to Kubernetes"
        & helm install external-secrets external-secrets/external-secrets -n external-secrets --create-namespace --set installCRDs=true
    }
}

$MetaJsonPath = "$PSScriptRoot/app/variables.$target.json"

if (-not (Test-Path $MetaJsonPath)) {
    Write-Error "File not found: $MetaJsonPath"
    exit 1
}

if ($target -eq "DockerDesktop") {
    Setup-DockerDesktop
}
elseif ($target -eq "GCP") {
    Setup-GCP
}

if ($RunAsContainer) {
    # Build a local copy of CNAB image
    docker build -t $CNABImage -f $PSScriptRoot/build/Dockerfile .

    $dockerRunArgs = @()

    if ($target -eq "GCP") {
        # Get current config path
        $gcloudConfigDir = gcloud info --format='value(config.paths.global_config_dir)'
        $dockerRunArgs += @(
            "-v", "$($gcloudConfigDir):/root/.config/gcloud",
            "--env", "GOOGLE_APPLICATION_CREDENTIALS=/gcp/creds.json"
        )
    }
    elseif ($target -eq "DockerDesktop") {

        if ($IsWindows) {
            $kubeConfigPath = "$env:USERPROFILE\.kube\config"
        } else {
            $kubeConfigPath = "~/.kube/config"
        }

        $dockerRunArgs += @(
            "-v", "$($kubeConfigPath):/.kube/config:ro",
            "--env", "KUBECONFIG=/.kube/config"
        )
    }
    
    $dockerRunArgs += @(
        "-v", "$($MetaJsonPath):/variables.json",
        "--env", "CNAB_ACTION=$Action",
        "--env", "INSTALLATION_METADATA=/variables.json",
        "--rm", "$CNABImage", "/cnab/app/run.ps1"
    )    

    docker run $dockerRunArgs
}
else {
    Check-CommandExists -Command "kubectl"

    $env:CNAB_ACTION = "install"
    $env:INSTALLATION_METADATA = $MetaJsonPath
    
    if ($target -eq "GCP") {
        $vars = (Get-Content $env:INSTALLATION_METADATA | ConvertFrom-Json)
        $tenantData = $vars.tenant_metadata
        $contextName = "gke_" + $tenantData.project + "_" + $tenantData.region + "_" + $tenantData.gke_cluster_name

        & kubectl config use-context $contextName
    }
    elseif ($target -eq "DockerDesktop") {
        & kubectl config use-context "docker-desktop"
    }   

    & ".\cnab\app\run.ps1" -runAsContainer $false
}

