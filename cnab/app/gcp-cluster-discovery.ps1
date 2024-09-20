function FindDeploymentGroup([string]$filter) {  
  Write-MinorStep "Finding GCP deployment group (project) using filter: $filter"
  $projects = (gcloud projects list --filter=$filter --format=json | ConvertFrom-Json)
  if ($null -eq $projects -Or $projects.Count -eq 0) {
    Write-MinorStep "No projects found"
    exit 1
  }
  elseif ($projects.Count -gt 1) {
    Write-MinorStep "$project_count projects found, cannot continue"
    exit 1
  }
  $project = $projects[0].projectId
  Write-MinorStep "Project: $project"
  return $project
}

function FindDeploymentTarget([string]$filter, [string]$deploymentGroup) {
  Write-MinorStep "Finding GCP deployment target (cluster) using filter: $filter and deployment group (project): $deploymentGroup"

  $clusters = (gcloud container clusters list --project=$deploymentGroup --format=json | ConvertFrom-Json)
  $cluster_count = $clusters.Count
  if ($cluster_count -eq 0) {
    Write-MinorStep "No clusters found"
    exit 1
  }
  elseif ($cluster_count -gt 1) {
    Write-MinorStep "$cluster_count clusters found, cannot continue"
    exit 1
  }
  $firstCluster = $clusters[0]
  $target = new-object psobject -property @{
    Name = $firstCluster.name
    Location = $firstCluster.location
  }
  Write-MinorStep "DeploymentTarget: $($target | ConvertTo-Json)"

  return $target
}
