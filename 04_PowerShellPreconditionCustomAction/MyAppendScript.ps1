param
(
    [Parameter(Mandatory=$false)][string]$propertyValue
)

Push-Location $PSScriptRoot

$fileName = "FileToInstall.txt"
$dateTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"

if(Test-Path $fileName)
{
    Add-Content -Path $fileName -Value "$dateTime - $propertyValue"
}
else
{
    throw "File $fileName does not exist"
}

Pop-Location