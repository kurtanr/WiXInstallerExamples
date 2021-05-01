param
(
    [Parameter(Mandatory=$false)][string]$installationStage,
    [Parameter(Mandatory=$false)][string]$propertyValue,
    [Parameter(Mandatory=$false)][string]$version,
    [Parameter(Mandatory=$false)][string]$installLocation
)

Push-Location $PSScriptRoot

$fileName = "InstallationStages.txt"
$time = Get-Date -Format "HH:mm:ss.fff"
$installLocation = $installLocation.Trim()

Add-Content -Path $fileName -Value "$time - $installLocation - $version - $propertyValue - $installationStage"

Pop-Location