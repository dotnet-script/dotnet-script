#!/bin/bash
mkdir /tmp/dotnet-script
if [[ -z $1 ]]; then
    version=$(curl https://api.github.com/repos/filipw/dotnet-script/releases/latest | grep -Eo "\"tag_name\":\s*\"(.*)\"" | cut -d'"' -f4)
else
    version=$1
fi    
currentVersion=$(dotnet script -v | tr -d '\n') 

if [[ $? -eq 0 ]]; then
    if [ "$version" == "$currentVersion" ]; then
        echo $version already installed
        exit 0
    fi   
fi

echo "Installing $version..."
curl -L https://github.com/filipw/dotnet-script/releases/download/$version/dotnet-script.$version.zip > /tmp/dotnet-script/dotnet-script.zip
unzip -o /tmp/dotnet-script/dotnet-script.zip -d /usr/local/lib
chmod +x /usr/local/lib/dotnet-script/dotnet-script.sh
cd /usr/local/bin
ln -sfn /usr/local/lib/dotnet-script/dotnet-script.sh dotnet-script
rm -rf /tmp/dotnet-script
echo Installation Finished