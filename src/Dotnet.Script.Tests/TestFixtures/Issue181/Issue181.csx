#! "netcoreapp2.0"


#r "nuget: Microsoft.CodeAnalysis.Scripting, 2.4.0"
#r "nuget: NetStandard.Library, 2.0.0"

List<string> list = new List<string>(new[] { "42" });
Write(list.FirstOrDefault());

