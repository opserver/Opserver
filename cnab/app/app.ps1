function Get-AppName() {  
    $app = 'opserver'
    return $app
}

function Generate-Values($vars, $environment, $containerRegistryUrl, $releaseTag, $pullSecretName) {  
    Write-MajorStep "Generating Helm values"
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
            storeRefName    = $vars.vars.secretStore
        }

        opserverExternalSecret       = @{
            storeRefName    = $vars.vars.secretStore
        }

        opserverSettings       = $vars.vars.opserverSettings

        adminRolebindingGroupId = $vars.vars.adminRolebindingGroupId
    }
    
    # Helm expects a YAML file but YAML is also a superset of JSON, so we can use ConvertTo-Json here
    $valuesFileContent = $values | ConvertTo-Json -Depth 100
    Write-MinorStep "Populated Helm values:"
    Write-MinorStep $valuesFileContent
    return $valuesFileContent
}