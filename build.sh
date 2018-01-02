#!/bin/sh

SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
DOTNET_SCRIPT="$SCRIPT_DIR/build/dotnet-script"
if [ ! -d "$DOTNET_SCRIPT" ]; then
    currentVersion=$(curl https://api.github.com/repos/filipw/dotnet-script/releases/latest | grep -Eo "\"tag_name\":\s*\"(.*)\"" | cut -d'"' -f4)

    echo Debug.... 
    curl --version 
    curl https://api.github.com/repos/filipw/dotnet-script/releases/latest 
    echo "Downloading dotnet-script version $currentVersion..."
    curl -L https://github.com/filipw/dotnet-script/releases/download/$currentVersion/dotnet-script.$currentVersion.zip > "$SCRIPT_DIR/build/dotnet-script.zip"
    unzip -o "$SCRIPT_DIR/build/dotnet-script.zip" -d "$SCRIPT_DIR/build/"
    if [ $? -ne 0 ]; then
        echo "An error occured while downloading dotnet-script"
        exit 1
    fi
fi
dotnet "$DOTNET_SCRIPT/dotnet-script.dll" "$SCRIPT_DIR/build/Build.csx" -- "$SCRIPT_DIR"
