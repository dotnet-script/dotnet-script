# dotnet-script

Run C# scripts from the .NET CLI.

## Usage

1> Create a `project.json` file with your dependencies and reference `Dotnet.Script` as a `tool`:

```
{
  "dependencies": {
    "Automapper": "5.1.1",
    "Newtonsoft.Json": "9.0.1"
  },

  "frameworks": {
    "netcoreapp1.0": {
    }
  },
  "tools": {
    "Dotnet.Script": {
      "version": "0.1.0",
      "imports": [
        "portable-net45+win8",
        "dnxcore50"
      ]
    }
  }
}
```

In the above case we will pull in `Automapper` and `Newtonsoft.Json` from nuget into our script.

2> Run `dotnet restore`

3> Now, create a C# script beside the `project.json`. You can use any types from the packages you listed in your dependencies. You can also use anything that is part of [Microsoft.NETCore.App](https://www.nuget.org/packages/Microsoft.NETCore.App/). Your script will essentially be a `netcoreapp1.0` app.

For example:

```csharp
using Newtonsoft.Json;
using AutoMapper;

Console.WriteLine("hello!");

var test = new { hi = "i'm json!" };
Console.WriteLine(JsonConvert.SerializeObject(test));

Console.WriteLine(typeof(MapperConfiguration));
```

4> You can now execute your script using `dotnet script foo.csx`. 

> CSX script could also be located elsewhere and referenced by absolute path - what's important is that the `project.json` with its dependencies is located next to the script file, and that restore was run beforehand.

This should produce the following output:

```shell
λ dotnet script foo.csx
hello!
{"hi":"i'm json!"}
AutoMapper.MapperConfiguration
```
