FROM microsoft/dotnet:2.1-sdk as builder
COPY . /dotnet-script
WORKDIR /dotnet-script

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN apt update
RUN apt install apt-transport-https
RUN echo "deb https://download.mono-project.com/repo/debian stable-stretch main" | tee /etc/apt/sources.list.d/mono-official-stable.list

RUN apt update
RUN apt install mono-devel -y
RUN apt install nuget -y

RUN dotnet restore
RUN dotnet test src/Dotnet.Script.Tests/Dotnet.Script.Tests.csproj
RUN dotnet publish -c Release src/Dotnet.Script/Dotnet.Script.csproj -f netcoreapp2.1

FROM microsoft/dotnet:2.1-sdk

COPY --from=builder /dotnet-script/src/Dotnet.Script/bin/Release/netcoreapp2.1/publish/ /dotnet-script/

WORKDIR /scripts

ENTRYPOINT ["dotnet", "/dotnet-script/dotnet-script.dll"]
