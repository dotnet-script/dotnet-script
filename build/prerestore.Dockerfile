FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# https://www.nuget.org/packages/dotnet-script/
RUN dotnet tool install dotnet-script --tool-path /usr/bin

# Create a simple script, execute it and cleanup after
# to create the script project dir, which requires
# 'dotnet restore' to be run.
# This is necessary if you want to run this in a networkless
# docker container.
RUN echo "return;" > tmpscript.cs
RUN dotnet script tmpscript.cs
RUN rm tmpscript.cs

ENTRYPOINT [ "dotnet", "script" ]