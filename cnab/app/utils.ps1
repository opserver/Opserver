# Helper functions for the CNAB scripts. dot-source this file (. $PSScriptRoot/utils.ps1) to include it.
# $Vars must be available before inclusion so we can determine runtime info (e.g. are we running under Octopus?)
if (!(Test-Path Variable:\vars)) {
    throw '$vars must be defined, containing the deserialized INSTALLATION_METADATA JSON.'
}

function Write-MajorStep {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline=$true)][string]$message)
    begin {}
    process {
        Write-Host "$($PSStyle.Foreground.BrightCyan)# $message$($PSStyle.Reset)"
    }
    end {}
}

function Write-MinorStep {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline=$true)][string]$message)
    begin {}
    process {
        Write-Host "$($PSStyle.Foreground.BrightMagenta)## $message$($PSStyle.Reset)"
    }
    end {}
}

# Ensure Warning, Verbose, and Debug get the correct Octopus service messages sent
# Output, Error, and Information already have the correct handling
# These are copied from https://github.com/OctopusDeploy/Calamari/blob/master/source/Calamari.Common/Features/Scripting/WindowsPowerShell/PowerShellBootstrapper.cs
$IsOctopusDeploy = $vars.runtime.name -eq 'GCP'
if ($IsOctopusDeploy) {
    function Write-Warning {
        [CmdletBinding()]
        param([Parameter(ValueFromPipeline=$true)][string]$message)
        begin {
            Write-Host "##octopus[stdout-warning]"
        }
        process {
            if($WarningPreference -ne 'SilentlyContinue')
            {
                Write-Host $message
            }
        }
        end {
            Write-Host "##octopus[stdout-default]"
        }
    }

    function Write-Verbose {
        [CmdletBinding()]
        param([Parameter(ValueFromPipeline=$true)][string]$message)
        begin {
            Write-Host "##octopus[stdout-verbose]"

        }
        process {
            Write-Host $message
        }
        end {
            Write-Host "##octopus[stdout-default]"
        }
    }

    function Write-Debug {
        [CmdletBinding()]
        param([Parameter(ValueFromPipeline=$true)][string]$message)
        begin {}
        process {
            Write-Verbose $message
        }
        end {}
    }
}
