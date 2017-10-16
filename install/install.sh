mkdir /tmp/dotnet-script
curl -L https://github.com/filipw/dotnet-script/releases/download/0.14.0/dotnet-script.0.14.0.zip > /tmp/dotnet-script/dotnet-script.zip
unzip -o /tmp/dotnet-script/dotnet-script.zip -d /usr/local/lib
chmod +x /usr/local/lib/dotnet-script/dotnet-script.sh
cd /usr/local/bin
ln -sfn /usr/local/lib/dotnet-script/dotnet-script.sh dotnet-script
rm -rf /tmp/dotnet-script