FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# https://www.nuget.org/packages/dotnet-script/
RUN dotnet tool install dotnet-script --tool-path /usr/bin

ENTRYPOINT [ "dotnet", "script" ]
