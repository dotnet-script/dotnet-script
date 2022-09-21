[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Create a temporary folder to download to.
$tempFolder = Join-Path $env:TEMP "dotnet-script"
New-Item $tempFolder -ItemType Directory -Force

# Get the latest release
$latestRelease = Invoke-WebRequest "https://api.github.com/repos/dotnet-script/dotnet-script/releases/latest" |
ConvertFrom-Json |
Select-Object tag_name
$tag_name =  $latestRelease.tag_name

# Download the zip
Write-Host "Downloading latest version ($tag_name)"
$client = New-Object "System.Net.WebClient"
$url = "https://github.com/dotnet-script/dotnet-script/releases/download/$tag_name/dotnet-script.$tag_name.zip"
$zipFile = Join-Path $tempFolder "dotnet-script.zip"
$client.DownloadFile($url,$zipFile)

$installationFolder = Join-Path $env:ProgramData "dotnet-script"
Microsoft.PowerShell.Archive\Expand-Archive $zipFile -DestinationPath $installationFolder -Force
Remove-Item $tempFolder -Recurse -Force

$path = [System.Environment]::GetEnvironmentVariable("path", [System.EnvironmentVariableTarget]::User);
# Get all paths except paths to old dotnet.script installations. 
$paths = $path.Split(";") -inotlike "*dotnet.script*" 
# Add the installation folder to the path
$paths += Join-Path $installationFolder "dotnet-script"
# Create the new path string
$path = $paths -join ";"

[System.Environment]::SetEnvironmentVariable("path", $path, [System.EnvironmentVariableTarget]::User)

Write-Host "Successfully installed version ($tag_name)"