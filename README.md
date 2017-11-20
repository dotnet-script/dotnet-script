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

We also provide a PowerShell script for installation.

```powershell
(new-object Net.WebClient).DownloadString("https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.ps1") | iex
```

#### Linux and Mac

```shell
curl -s https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.sh | bash
```

If permission is denied we can try with `sudo`

```shell
curl -s https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.sh | sudo bash
```

#### Docker

A Dockerfile for running dotnet-script in a Linux container is available. Build:

```shell
cd build
docker build -t dotnet-script -f Dockerfile ..
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



### Script Packages

Script packages are a way of organizing reusable scripts into NuGet packages that can be consumed by other scripts. This means that we now can leverage scripting infrastructure without the need for any kind of bootstrapping. 

#### Creating a script package

A script package is just a regular NuGet package that contains script files inside the `content` or `contentFiles` folder.

The following example shows how the scripts are laid out inside the NuGet package according to the [standard convention](https://docs.microsoft.com/en-us/nuget/schema/nuspec#including-content-files) .

```shell
└── contentFiles
    └── csx
        └── netstandard2.0
            └── main.csx
```

This example contains just the `main.csx` file in the root folder, but packages may have multiple script files either in the root folder or in subfolders below the root folder. 

When loading a script package we will look for an entry point script to be loaded. This entry point script is identified by one of the following.

- A script called `main.csx` in the root folder  
- A single script file in the root folder

If the entry point script cannot be determined, we will simply load all the scripts files in the package.

> The advantage with using an entry point script is that we can control loading other scripts from the package. 

#### Consuming a script package

To consume a script package all we need to do specify the NuGet package in the `#load `directive.

The following example loads the [simple-targets](https://www.nuget.org/packages/simple-targets-csx) package that contains script files to be included in our script.

```C#
#! "netcoreapp2.0"
#load "nuget:simple-targets-csx, 6.0.0"

using static SimpleTargets;
var targets = new TargetDictionary();

targets.Add("default", () => Console.WriteLine("Hello, world!"));

Run(Args, targets);
```

> Note: Debugging also works for script packages so that we can easily step into the scripts that are brought in using the `#load` directive. 

 





