## dotnet script

Run C# scripts from the .NET CLI.

### Build status

| Build server | Platform    | Build status                             |
| ------------ | ----------- | ---------------------------------------- |
| AppVeyor     | Windows     | [![](https://img.shields.io/appveyor/ci/filipw/dotnet-script/master.svg)](https://ci.appveyor.com/project/filipw/dotnet-script/branch/master) |
| Travis       | Linux/ OS X | [![](https://travis-ci.org/filipw/dotnet-script.svg?branch=master)](https://travis-ci.org/filipw/dotnet-script) |

### Installing

#### Prerequisites

The only thing we need to install is [.Net Core](https://www.microsoft.com/net/download/core)

#### Windows

```powershell
choco install dotnet.script
```

#### Linux and Mac

Download and unzip the latest [release](https://github.com/filipw/dotnet-script/releases) and make sure that `dotnet-script.sh` is in your PATH.

#### Docker

A Dockerfile for running dotnet-script in a Linux container is available. Build:

```shell
cd build
docker build -t dotnet-script ..
```

And run:

```
docker run -it dotnet-script --version

```
### Usage

Our typical `helloworld.csx` might look like this

```
#! "netcoreapp1.1"
#r "nuget:NetStandard.Library,1.6.1"

Console.WriteLine("Hello world!");
```

Let us take a quick look at what is going on here.

`#! "netcoreapp1.1"` tells OmniSharp to resolve metadata in the context of a`netcoreapp1.1` application.

`#r "nuget:NetStandard.Library,1.6.1"` brings in the the [NetStandard.Library 1.6.1](https://www.nuget.org/packages/NETStandard.Library/1.6.1) from NuGet.

That is all it takes and we can execute the script

```
dotnet script helloworld.csx
```

#### Scaffolding

Simply create a folder somewhere on your system and issue the following command.

```shell
dotnet script init
```

This will create `Helloworld.csx` along with the launch configuration needed to debug the script in VS Code.

```shell
.
├── .vscode
│   └── launch.json
├── helloworld.csx
└── omnisharp.json
```

#### Passing arguments to scripts

All arguments after `--` are passed to the script in the following way:

```shell
dotnet script foo.csx -- arg1 arg2 arg3
```

Then you can access the arguments in the script context using the global `Args` collection:

```c#
foreach (var arg in Args)
{
    Console.WriteLine(arg);
}
```

All arguments before `--` are processed by `dotnet script`. For example, the following command-line

```shell
dotnet script -d foo.csx -- -d
```

will pass the `-d` before `--` to `dotnet script` and enable the debug mode whereas the `-d` after `--` is passed to script for its own interpretation of the argument.

#### NuGet Packages

`dotnet script` has built-in support for referencing NuGet packages directly from within the script.

```c#
#r "nuget: AutoMapper, 9.1.0"
```



![package](https://user-images.githubusercontent.com/1034073/30176983-98a6b85e-9404-11e7-8855-4ae65a20d6b1.gif)

> Note: Omnisharp needs to be restarted after adding a new package reference

#### Debugging

The days of debugging scripts using `Console.WriteLine` are over. One major feature of `dotnet script` is the ability to debug scripts directly in VS Code. Just set a breakpoint anywhere in your script file(s) and hit F5(start debugging)



![debug](https://user-images.githubusercontent.com/1034073/30173509-2f31596c-93f8-11e7-9124-ca884cf6564e.gif)
