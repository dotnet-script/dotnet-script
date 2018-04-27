#r "nuget: System.Configuration.ConfigurationManager, 4.4.1"

using System.Configuration;

var value = ConfigurationManager.AppSettings.Get("SomeValue");
Console.WriteLine("value: " + value);