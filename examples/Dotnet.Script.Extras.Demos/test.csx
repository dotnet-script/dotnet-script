#r "paket: nuget Newtonsoft.Json"

using System;
using Newtonsoft.Json;

class Foo
{
    public string Bar { get; set; }
}

Console.WriteLine(JsonConvert.SerializeObject(new Foo { Bar = "hi" }));