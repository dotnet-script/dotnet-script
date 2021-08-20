#r "nuget:Newtonsoft.Json, 10.0.1"

using Newtonsoft.Json;

var version = typeof(JsonConvert).Assembly.GetName().Version;
Console.WriteLine(version);
