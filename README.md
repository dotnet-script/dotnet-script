# dotnet script

Run C# scripts from the .NET CLI.

## Build status

| Build server | Platform    | Build status                             |
| ------------ | ----------- | ---------------------------------------- |
| AppVeyor     | Windows     | [![](https://img.shields.io/appveyor/ci/filipw/dotnet-script/master.svg)](https://ci.appveyor.com/project/filipw/dotnet-script/branch/master) |
| Travis       | Linux/ OS X | [![](https://travis-ci.org/filipw/dotnet-script.svg?branch=master)](https://travis-ci.org/filipw/dotnet-script) |

## Installing

### Prerequisites

The only thing we need to install is [.Net Core](https://www.microsoft.com/net/download/core)

### Windows

```powershell
choco install dotnet.script
```

We also provide a PowerShell script for installation.

```powershell
(new-object Net.WebClient).DownloadString("https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.ps1") | iex
```

### Linux and Mac

```shell
curl -s https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.sh | bash
```

If permission is denied we can try with `sudo`

```shell
curl -s https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.sh | sudo bash
```

### Docker

A Dockerfile for running dotnet-script in a Linux container is available. Build:

```shell
cd build
docker build -t dotnet-script -f Dockerfile ..
```

And run:

```
docker run -it dotnet-script --version

```
## Usage

Our typical `helloworld.csx` might look like this

```
#! "netcoreapp2.0"
Console.WriteLine("Hello world!");
```

Let us take a quick look at what is going on here.

`#! "netcoreapp2.0"` tells OmniSharp to resolve metadata in the context of a`netcoreapp2.0` application.

This will bring in all assemblies from [Microsoft.NETCore.App](https://www.nuget.org/packages/Microsoft.NETCore.App/2.0.0) and should cover most scripting needs. 

That is all it takes and we can execute the script

```
dotnet script helloworld.csx
```



### Scaffolding

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

### Passing arguments to scripts

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

### NuGet Packages

`dotnet script` has built-in support for referencing NuGet packages directly from within the script.

```c#
#r "nuget: AutoMapper, 9.1.0"
```

![package](https://user-images.githubusercontent.com/1034073/30176983-98a6b85e-9404-11e7-8855-4ae65a20d6b1.gif)

> Note: Omnisharp needs to be restarted after adding a new package reference

### Debugging

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

## REPL

This release contains a C# REPL (Read-Evaluate-Print-Loop). The REPL mode ("interactive mode") is started by executing `dotnet-script` without any arguments.

The interactive mode allows you to supply individual C# code blocks and have them executed as soon as you press <kbd>Enter</kbd>. The REPL is configured with the same default set of assembly references and using statements as regular CSX script execution.

### Basic usage

Once `dotnet-script` starts you will see a prompt for input. You can start typing C# code there.

```
~$ dotnet script
> var x = 1;
> x+x
2
```

If you submit an unterminated expression into the REPL (no `;` at the end), it will be evaluated and the result will be serialized using a formatter and printed in the output. This is a bit more interesting than just calling `ToString()` on the object, because it attempts to capture the actual structure of the object. For example:

```
~$ dotnet script
> var x = new List<string>();
> x.Add("foo");
> x
List<string>(1) { "foo" }
> x.Add("bar");
> x
List<string>(2) { "foo", "bar" }
>
```

### Inline Nuget packages

REPL also supports inline Nuget packages - meaning the Nuget packages can be installed into the REPL from *within the REPL*. This is done via our `#r` and `#load` from Nuget support and uses identical syntax.

```
~$ dotnet script
> #r "nuget: Automapper, 6.1.1"
> using AutoMapper;
> typeof(MapperConfiguration)
[AutoMapper.MapperConfiguration]
> #load "nuget: simple-targets-csx, 6.0.0";
> using static SimpleTargets;
> typeof(TargetDictionary)
[Submission#0+SimpleTargets+TargetDictionary]
```

### Multiline mode

Using Roslyn syntax parsing, we also support multiline REPL mode. This means that if you have an uncompleted code block and press <kbd>Enter</kbd>, we will automatically enter the multline mode. The mode is indicated by the `*` character. This is particularly useful for declaring classes and other more complex constructs.

```
~$ dotnet script
> class Foo {
* public string Bar {get; set;}
* }
> var foo = new Foo();
```

### REPL commands

Aside from the regular C# script code, you can invoke the following commands (directives) from within the REPL:

| Command  | Description                              |
| -------- | ---------------------------------------- |
| `#load`  | Load a script into the REPL (same as `#load` usage in CSX) |
| `#r`     | Load an assembly into the REPL (same as `#r` usage in CSX) |
| `#reset` | Reset the REPL back to initial state (without restarting it) |
| `#cls`   | Clear the console screen without resetting the REPL state |
| `#exit`  | Exits the REPL                           |

### Seeding REPL with a script

You can execute a CSX script and, at the end of it, drop yourself into the context of the REPL. This way, the REPL becomes "seeded" with your code - all the classes, methods or variables are available in the REPL context. This is achieved by running a script with an `-i` flag.

For example, given the following CSX script:

```csharp
var msg = "Hello World";
Console.WriteLine(msg);
```

When you run this with the `-i` flag, `Hello World` is printed, REPL starts and `msg` variable is available in the REPL context.

```
~$ dotnet script foo.csx -i
Hello World
> 
```

You can also seed the REPL from inside the REPL - at any point - by invoking a `#load` directive pointed at a specific file. For example:

```
~$ dotnet script
> #load "foo.csx"
Hello World
>
```

## Piping 

The following example shows how we can pipe data in and out of a script.

The `UpperCase.csx` script simply converts the standard input to upper case and writes it back out to standard output.

```csharp
#! "netcoreapp2.0"
#r "nuget: NetStandard.Library, 2.0.0"

using (var streamReader = new StreamReader(Console.OpenStandardInput()))
{    
    Write(streamReader.ReadToEnd().ToUpper()); 
}
```

We can now simply pipe the output from one command into our script like this.

```shell
echo "This is some text" | dotnet script UpperCase.csx 
THIS IS SOME TEXT
```

### Debugging

The first thing we need to do add the following to the `launch.config` file that allows VS Code to debug a running process.

```JSON
{
    "name": ".NET Core Attach",
    "type": "coreclr",
    "request": "attach",
    "processId": "${command:pickProcess}"
}
```

To debug this script we need a way to attach the debugger in VS Code and to the simplest thing we can do here is to wait for the debugger to attach by adding this method somewhere.

```c#
public static void WaitForDebugger()
{
    Console.WriteLine("Attach Debugger (VS Code)");
    while(!Debugger.IsAttached)
    {
    }
}
```

To debug the script when executing it from the command line we can do something like 

````c#
#! "netcoreapp2.0"
#r "nuget: NetStandard.Library, 2.0.0"
WaitForDebugger();
using (var streamReader = new StreamReader(Console.OpenStandardInput()))
{    
    Write(streamReader.ReadToEnd().ToUpper()); // <- SET BREAKPOINT HERE
}
````

Now when we run the script from the command line we will get 

```shell
$ echo "This is some text" | dotnet script UpperCase.csx
Attach Debugger (VS Code)
```

This now gives us a chance to attach the debugger before stepping into the script and from VS Code, select the  `.NET Core Attach` debugger and pick the process that represents the executing script. 

Once that is done we should see out breakpoint being hit.

## Team

* [Bernhard Richter](https://github.com/seesharper) ([@bernhardrichter](https://twitter.com/bernhardrichter))
* [Filip W](https://github.com/filipw) ([@filip_woj](https://twitter.com/filip_woj))

## License 

[MIT License](https://github.com/filipw/dotnet-script/blob/master/LICENSE)
