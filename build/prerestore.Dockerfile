FROM mcr.microsoft.com/dotnet/sdk:7.0

# https://www.nuget.org/packages/dotnet-script/
RUN dotnet tool install dotnet-script --tool-path /usr/bin

# Create a simple script, execute it and cleanup after
# to create the script project dir, which requires
# 'dotnet restore' to be run.
# This is necessary if you want to run this in a networkless
# docker container.
RUN dotnet script eval "Console.WriteLine(\"☑️ Prepared env for offline usage\")"

ENTRYPOINT [ "dotnet", "script" ]
