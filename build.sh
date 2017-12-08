#!/bin/sh

SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
DOTNET_SCRIPT="$SCRIPT_DIR/build/dotnet-script"
if [ ! -d "$DOTNET_SCRIPT" ]; then
    echo "Downloading dotnet-script..."
    curl -L https://github.com/filipw/dotnet-script/releases/download/0.14.0/dotnet-script.0.14.0.zip > "$SCRIPT_DIR/build/dotnet-script.zip"
    unzip -o "$SCRIPT_DIR/build/dotnet-script.zip" -d "$SCRIPT_DIR/build/"
    if [ $? -ne 0 ]; then
        echo "An error occured while downloading dotnet-script"
        exit 1
    fi
fi
dotnet "$DOTNET_SCRIPT/dotnet-script.dll" "$SCRIPT_DIR/build/build.csx" -- "$SCRIPT_DIR"