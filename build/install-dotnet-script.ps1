[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$scriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$client = New-Object "System.Net.WebClient"
$url = "https://github.com/filipw/dotnet-script/releases/download/0.18.0/dotnet-script.0.18.0.zip"
$file = "$scriptRoot/dotnet-script.zip"
$client.DownloadFile($url,$file)
Expand-Archive $file -DestinationPath $scriptRoot -Force