#r "nuget:NuGet.Client,*"
#r "nuget:NuGet.Configuration,*"

using System.Linq;
using NuGet;
using NuGet.Configuration;
using NuGet.Repositories;

var repo = new NuGetv3LocalRepository(SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null)));
var packages = repo.FindPackagesById("NuGet.Client");
var pkg = packages.Last();
var nuspec = pkg.Nuspec;
Write(nuspec.GetId());