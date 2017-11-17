# Add the binary folder to the users PATH environment variable.
$pathToBinaries = Join-Path -Path $($env:ChocolateyPackageFolder) -ChildPath $($env:ChocolateyPackageName )
Install-ChocolateyPath -PathToInstall "$pathToBinaries" -PathType 'Machine'