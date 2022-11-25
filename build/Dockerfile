FROM mcr.microsoft.com/dotnet/sdk:7.0

# https://www.nuget.org/packages/dotnet-script/
RUN dotnet tool install dotnet-script --tool-path /usr/bin

ENTRYPOINT [ "dotnet", "script" ]
