#! "netcoreapp1.1"
#r "nuget:NetStandard.Library,1.6.1"
#load "DotNet.csx"

DotNet.Build(@"..\src\Dotnet.Script");
DotNet.Build(@"..\src\Dotnet.Script.Tests");
DotNet.Test(@"..\src\Dotnet.Script.Tests");

