FROM microsoft/dotnet:2.0.0-sdk 
COPY . /dotnet-script
WORKDIR /dotnet-script

RUN dotnet restore
RUN dotnet test src/Dotnet.Script.Tests/Dotnet.Script.Tests.csproj 
RUN dotnet publish src/Dotnet.Script/Dotnet.Script.csproj -f netcoreapp2.0

WORKDIR /scripts

ENTRYPOINT ["dotnet", "/dotnet-script/src/Dotnet.Script/bin/Debug/netcoreapp2.0/publish/dotnet-script.dll"] 
