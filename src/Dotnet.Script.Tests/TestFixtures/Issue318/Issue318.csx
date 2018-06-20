#r "nuget: Npgsql, 4.0.0"

using System.Reflection;

var versionLoaded = typeof(Npgsql.NpgsqlCommand).Assembly.GetName().Version;
var name = new AssemblyName("Npgsql, Culture=neutral, PublicKeyToken=null");
var assembly = Assembly.Load(name);
Console.WriteLine("Hello World!");