function Find-ContainerRegistry([string]$imageTag, [bool]$singleRegistry) {
    Write-MinorStep "Finding Container Registry for tag: $imageTag"
    
    # default container registry;  for PRs in some apps we still use cr-dev and we will override
    # below if we detect a PR and singleRegistry is false
    $containerRegistryUrl = 'cr.stackoverflow.software'
    $pullSecretName = 'cloudsmith-cr-prod'

    $isPr = IsPr $imageTag

    if ($isPr) {
        if (-not $singleRegistry) {
            $containerRegistryUrl = 'cr-dev.stackoverflow.software'
            $pullSecretName = 'cloudsmith-cr-dev'
        }
        $forceUpgrade = @('--force') # This'll force pods to be recreated with freshly-pulled images
    }
    else {
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

  # Image tags that are PRs are in the format pr-xxx or yyyy.mm.dd.vv-pr
  # where:
  # - xxx is the github pull request number
  # - yyyy.mm.dd is the date of the release
  # - vv is the github run number
  # - pr- and -pr are literals
  function IsPr([string]$imageTag) {
    $isPr = $imageTag -match '(^pr-[0-9]+(-[0-9]+)?$)|([0-9\.]*-pr?$)'
    Write-MinorStep "Is PR: $isPr"
    return $isPr
  }
  