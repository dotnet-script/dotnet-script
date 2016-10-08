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

## Advanced usage

You can also reference a script from a script - this is achieved via the `#load` directive.

Imagine having the following 2 CSX files side by side - `bar.csx` and `foo.csx`:

```csharp
Console.WriteLine("Hello from bar.csx");
```

```csharp
#load "bar.csx"
Console.WriteLine("Hello from foo.csx");
```

Running `dotnet script foo.csx` will produce:

```shell
Hello from bar.csx
Hello from foo.csx
```

Even better, `Dotnet.Script` supports loading CSX references over HTTP too. You could now modify the `foo.csx` accordingly:

```csharp
#load "https://gist.githubusercontent.com/filipw/9a79bb00e4905dfb1f48757a3ff12314/raw/adbfe5fade49c1b35e871c49491e17e6675dd43c/foo.csx"
#load "bar.csx"

Console.WriteLine("Hello from foo.csx");
```

In this case, the first dependency is loaded as `string` and parsed from an HTTP source - in this case a [gist](https://gist.githubusercontent.com/filipw/9a79bb00e4905dfb1f48757a3ff12314/raw/adbfe5fade49c1b35e871c49491e17e6675dd43c/foo.csx) I set up beforehand.

Running `dotnet script foo.csx` now, will produce:

```shell
Hello from a gist
Hello from bar.csx
Hello from foo.csx
```

## Issues and problems

![](http://lh6.ggpht.com/-z_BeRqTrtJE/T2sLYAo-WmI/AAAAAAAAAck/0Co6XilSmNU/WorksOnMyMachine_thumb%25255B4%25255D.png?imgmax=800)

![](http://i110.photobucket.com/albums/n86/MCRfreek92/i-have-no-idea-what-im-doing-dog.jpg)

## License

[MIT](https://github.com/filipw/dotnet-script/blob/master/LICENSE)
