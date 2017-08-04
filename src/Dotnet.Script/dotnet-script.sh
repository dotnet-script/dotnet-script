#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
dotnet exec $DIR/dotnet-script.dll "$@"