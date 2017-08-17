$client = New-Object "System.Net.WebClient"
$url = "https://github.com/filipw/dotnet-script/releases/download/0.11.0-beta/dotnet-script.0.11.0-beta.zip"
$file = "$pwd/dotnet-script.zip"
$client.DownloadFile($url,$file)
Expand-Archive $file -DestinationPath $pwd\dotnet-script -Force

