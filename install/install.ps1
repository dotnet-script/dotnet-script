# Create a temporary folder to download to.
$tempFolder = Join-Path $env:TEMP "dotnet-script"
New-Item $tempFolder -ItemType Directory -Force

# Download the zip
$client = New-Object "System.Net.WebClient"
$url = "https://github.com/filipw/dotnet-script/releases/download/0.14.0/dotnet-script.0.14.0.zip"
$zipFile = Join-Path $tempFolder "dotnet-script.zip"
$client.DownloadFile($url,$zipFile)

$installationFolder = Join-Path $env:ProgramData "dotnet-script"
Expand-Archive $zipFile -DestinationPath $installationFolder -Force
Remove-Item $tempFolder -Recurse -Force

$path = [System.Environment]::GetEnvironmentVariable("path", [System.EnvironmentVariableTarget]::User);
# Get all paths except paths to old dotnet.script installations. 
$paths = $path.Split(";") -inotlike "*dotnet.script*" 
# Add the installation folder to the path
$paths += Join-Path $installationFolder "dotnet-script"
# Create the new path string
$path = $paths -join ";"

[System.Environment]::SetEnvironmentVariable("path", $path, [System.EnvironmentVariableTarget]::User)
