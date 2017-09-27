FROM microsoft/dotnet:1.1-sdk 
COPY . /dotnet-script
WORKDIR /dotnet-script

RUN dotnet restore
RUN dotnet test src/Dotnet.Script.Tests/Dotnet.Script.Tests.csproj 
RUN dotnet publish 

WORKDIR /scripts

ENTRYPOINT ["dotnet", "/dotnet-script/src/Dotnet.Script/bin/Debug/netcoreapp1.1/publish/dotnet-script.dll"] 
