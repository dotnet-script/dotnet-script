$scriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$dotnetScriptPath = "$scriptRoot\build\dotnet-script"
if (!(Test-Path $dotnetScriptPath)) {
    Write-Host "Downloading dotnet-script..."
    Invoke-Expression ".\install-dotnet-script.ps1"
}

dotnet "$dotnetScriptPath\dotnet-script.dll" "$scriptRoot\build\build.csx" -- $scriptRoot