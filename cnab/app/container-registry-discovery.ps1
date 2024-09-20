function Find-ContainerRegistry([string]$imageTag) {
    Write-MinorStep "Finding Container Registry for tag: $imageTag"
    # PR container images are located in `cr-dev` in CloudSmith. As opposed to `cr` which we use for release builds.
    $isPr = IsPr $imageTag
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

    $containerRegistryDetails = new-object psobject -property @{
        Url = $containerRegistryUrl
        PullSecretName = $pullSecretName
        ForceUpgrade = $forceUpgrade
    }
    Write-MinorStep "Registry Details: $($containerRegistryDetails | ConvertTo-Json)"
  
    return $containerRegistryDetails
  }

  function IsPr([string]$imageTag) {
    $isPr = $imageTag -match '(^pr-[0-9]+(-[0-9]+)?$)|([0-9\.]*-pr?$)'
    Write-MinorStep "Is PR: $isPr"
    return $isPr
  }
  