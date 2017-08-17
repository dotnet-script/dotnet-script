# dotnet-script

Run C# scripts from the .NET CLI.

[![Nuget](http://img.shields.io/nuget/v/Dotnet.Script.svg?maxAge=3600)](https://www.nuget.org/packages/Dotnet.Script/)

## Build status

| Build server | Platform     | Build status                                                                                                                     
|--------------|--------------|--------------
| AppVeyor     | Windows      | [![](https://img.shields.io/appveyor/ci/filipw/dotnet-script/master.svg)](https://ci.appveyor.com/project/filipw/dotnet-script/branch/master)
| Travis       | Linux / OS X | TBD

## Prerequisites

> What do I need to install? 

Nothing - everything is self contained from the `project.json` level. Just make sure you have .NET Core installed and `dotnet` available in your PATH.

## Usage

1> Create a `project.json` file with your dependencies and reference `Dotnet.Script` as a `tool`:

```
{
  "dependencies": {
    "Automapper": "5.1.1",
    "Newtonsoft.Json": "9.0.1",
    "NetStandard.Library": "1.6.0"
  },

  "frameworks": {
    "netcoreapp1.0": {
    }
  },
  "tools": {
    "Dotnet.Script": {
      "version": "0.7.0-beta",
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

## Debugging

`dotnet-script` supports debugging C# scripts. In order to do that we'll need to be able to invoke `dotnet-script.dll` directly - rather than via `dotnet` CLI. You will also need to have [C# Extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) installed.

Normally, the nuget package with the `dotnet-script.dll` will be globally located on your machine (on Windows) in `C:\Users\{user}\.nuget\packages\` folder let's refer to that location as `<NUGET_ROOT>`. 

To debug a script using Visual Studio Code, create a folder `.vscode` next to your script and put the following `launch.json` file inside (make sure to replace `<NUGET_ROOT>` with your global nuget packages path, ensure that the path to `dotnet.exe` is correct and that `dotnet-script` version matches the one you have got installed!). This technique works on Windows, as well as on OS X and Linux (make sure to use Unix paths there).

```
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Script Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "C:\\Program Files\\dotnet\\dotnet.exe", // path to your dotnet.exe installation
            "args": [
              "exec", 
              "--additionalprobingpath", "<NUGET_ROOT>", 
              "--depsfile", "<NUGET_ROOT>\\.tools\\Dotnet.Script\\0.9.0-beta\\lib\\netcoreapp1.0\\Dotnet.Script.deps.json", 
              "<NUGET_ROOT>\\Dotnet.Script\\0.9.0-beta\\lib\\netcoreapp1.0\\dotnet-script.dll", 
              "${workspaceRoot}\\foo.csx",
              "-d"],
            "cwd": "${workspaceRoot}",
            "externalConsole": false,
            "stopAtEntry": true,
            "internalConsoleOptions": "openOnSessionStart",
            "requireExactSource": false // required to step through the #loaded files
        },
        {
            "name": ".NET Core Attach",
            "type": "coreclr",
            "request": "attach",
            "processId": "${command.pickProcess}"
        }
    ]
}
```

You can now set breakpoints inside your CSX file and launch the debugger using F5.

![](http://i.imgur.com/YzBkVil.png)

## Intellisense

Intellisense and C# language services for `dotnet-script` are supported out of the box by OmniSharp.

![](https://camo.githubusercontent.com/a1981f7da5de7ca6f181096b6c469f4c79f37d43/687474703a2f2f672e7265636f726469742e636f2f4b766f544336334869372e676966)

## Advanced usage

### File watcher

`dotnet script` is compatible with the official [dotnet CLI file watcher](https://www.nuget.org/packages/Microsoft.DotNet.Watcher.Tools/1.0.0-preview2-final) tool. To use the file watcher (re-run the script automatically upon each change) you will need to add the following `Microsoft.DotNet.Watcher.Tools` reference to your `project.json`:

```json 
  "tools": {
    "Dotnet.Script": {
      "version": "0.7.0-beta",
      "imports": [
        "portable-net45+win8",
        "dnxcore50"
      ]
    },
    "Microsoft.DotNet.Watcher.Tools": {
      "version": "1.0.0-preview2-final"
    }
  }
```

Additionally, you need to instruct the watcher to monitor `csx` files. This is done via `buildOptions` in `project.json`:

```json
  "buildOptions": {
    "compile": "**/*.csx"
  },
```

After restoring `Microsoft.DotNet.Watcher.Tools`, you can now run your script as follows:

```
dotnet watch script foo.csx
```

This will run `foo.csx` and watch for changes in it, automatically re-running it any time you make any changes.

### Referencing local script from a script

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

### Passing arguments to scripts

All arguments after `--` are passed to the script in the following way:

```
dotnet script foo.csx -- arg1 arg2 arg3
```

Then you can access the arguments in the script context using the global `Args` collection:

```
foreach (var arg in Args)
{
    Console.WriteLine(arg);
}
```

All arguments before `--` are processed by `dotnet script`. For example, the following command-line

```
dotnet script -d foo.csx -- -d
```

will pass the `-d` before `--` to `dotnet script` and enable the debug mode whereas the `-d` after `--` is passed to script for its own interpretation of the argument.

## Extras

Beyond the standard scripting dialect support (compatible with `csi.exe`), `Dotnet.Script` provides some extra features, located in the `Dotnet.Script.Extras` package.

### Referencing an HTTP-based script from a script

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

## Contributing

You will need Visual Studio 2017 or Visual Studio Code with C# extension 1.7.0+ to build the project, as well as .NET Core SDK 1.0.0.

## Issues and problems

![](http://lh6.ggpht.com/-z_BeRqTrtJE/T2sLYAo-WmI/AAAAAAAAAck/0Co6XilSmNU/WorksOnMyMachine_thumb%25255B4%25255D.png?imgmax=800)

![](http://i110.photobucket.com/albums/n86/MCRfreek92/i-have-no-idea-what-im-doing-dog.jpg)

Due to [this .NET CLI bug](https://github.com/dotnet/cli/issues/4198) in order to debug the cloned solution, comment out the `buildOptions > outputName` property in `project.json`.

## Credits

Special thanks to [Bernhard Richter](https://twitter.com/bernhardrichter?lang=en) for his help with .NET Core debugging.

## License

[MIT](https://github.com/filipw/dotnet-script/blob/master/LICENSE)
