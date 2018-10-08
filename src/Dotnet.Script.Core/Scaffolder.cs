﻿using Dotnet.Script.Core.Templates;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Dotnet.Script.Core
{
    public class Scaffolder
    {
        private ScriptEnvironment _scriptEnvironment;
        private const string DefaultScriptFileName = "main.csx";
        private ScriptConsole _scriptConsole = ScriptConsole.Default;
        private CommandRunner _commandRunner;

        public Scaffolder(LogFactory logFactory)
        {
            _commandRunner = new CommandRunner(logFactory);
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        public void InitializerFolder(string fileName, string currentWorkingDirectory)
        {
            CreateLaunchConfiguration(currentWorkingDirectory);
            CreateOmniSharpConfigurationFile(currentWorkingDirectory);
            CreateScriptFile(fileName, currentWorkingDirectory);
        }

        public void CreateNewScriptFile(string fileName, string currentDirectory)
        {
            _scriptConsole.WriteNormal($"Creating '{fileName}'");
            if (!Path.HasExtension(fileName))
            {
                fileName = Path.ChangeExtension(fileName, ".csx");
            }
            var pathToScriptFile = Path.Combine(currentDirectory, fileName);
            if (!File.Exists(pathToScriptFile))
            {
                var scriptFileTemplate = TemplateLoader.ReadTemplate("helloworld.csx.template");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // add a shebang to set dotnet-script as the interpreter for .csx files
                    // and make sure we are using environment newlines, because shebang won't work with windows cr\lf
                    scriptFileTemplate = $"#!/usr/bin/env dotnet-script" + Environment.NewLine + scriptFileTemplate.Replace("\r\n", Environment.NewLine);
                }

                File.WriteAllText(pathToScriptFile, scriptFileTemplate, new UTF8Encoding(false /* Linux shebang can't handle BOM */));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // mark .csx file as executable, this activates the shebang to run dotnet-script as interpreter
                    _commandRunner.Execute($"/bin/chmod", $"+x {pathToScriptFile}");
                }
                _scriptConsole.WriteSuccess($"...'{pathToScriptFile}' [Created]");
            }
            else
            {
                _scriptConsole.WriteHighlighted($"...'{pathToScriptFile}' already exists [Skipping]");
            }
        }

        private void CreateScriptFile(string fileName, string currentWorkingDirectory)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                CreateDefaultScriptFile(currentWorkingDirectory);
            }
            else
            {
                CreateNewScriptFile(fileName, currentWorkingDirectory);
            }
        }

        private void CreateDefaultScriptFile(string currentWorkingDirectory)
        {
            _scriptConsole.Out.WriteLine($"Creating default script file '{DefaultScriptFileName}'");
            if (Directory.GetFiles(currentWorkingDirectory, "*.csx").Any())
            {
                _scriptConsole.WriteHighlighted("...Folder already contains one or more script files [Skipping]");
            }
            else
            {
                CreateNewScriptFile(DefaultScriptFileName, currentWorkingDirectory);
            }
        }

        private void CreateOmniSharpConfigurationFile(string currentWorkingDirectory)
        {
            _scriptConsole.WriteNormal("Creating OmniSharp configuration file");
            string pathToOmniSharpJson = Path.Combine(currentWorkingDirectory, "omnisharp.json");
            if (!File.Exists(pathToOmniSharpJson))
            {
                var omniSharpFileTemplate = TemplateLoader.ReadTemplate("omnisharp.json.template");
                JObject settings = JObject.Parse(omniSharpFileTemplate);
                settings["script"]["defaultTargetFramework"] = _scriptEnvironment.TargetFramework;
                File.WriteAllText(pathToOmniSharpJson, settings.ToString());
                _scriptConsole.WriteSuccess($"...'{pathToOmniSharpJson}' [Created]");
            }
            else
            {
                _scriptConsole.WriteHighlighted($"...'{pathToOmniSharpJson} already exists' [Skipping]");
            }
        }

        private void CreateLaunchConfiguration(string currentWorkingDirectory)
        {
            string vsCodeDirectory = Path.Combine(currentWorkingDirectory, ".vscode");
            if (!Directory.Exists(vsCodeDirectory))
            {
                Directory.CreateDirectory(vsCodeDirectory);
            }

            // on windows we use this as opportunity to make the file association for .csx -> dotnet-script
            // (If/when dotnet install command provides us an install time hook this code should move there)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // check to see if .csx is mapped to dotnet-script
                if (_commandRunner.Execute("reg", @"query HKCU\Software\classes\.csx") != 0)
                {
                    // register dotnet-script as the tool to process .csx files
                    _commandRunner.Execute("reg", @"add HKCU\Software\classes\.csx /f /ve /t REG_SZ -d dotnetscript");
                }

                if (_commandRunner.Execute("reg", @"query HKCU\Software\classes\dotnetscript") != 0)
                {
                    _commandRunner.Execute("reg", $@"add HKCU\Software\Classes\dotnetscript\Shell\Open\Command /f /ve /t REG_EXPAND_SZ /d ""\""%ProgramFiles%\dotnet\dotnet.exe\"" exec \""{Path.Combine(_scriptEnvironment.InstallLocation, "dotnet-script.dll")}\"" \""%1\"" -- %*""");
                }
            }

            _scriptConsole.WriteNormal("Creating VS Code launch configuration file");
            string pathToLaunchFile = Path.Combine(vsCodeDirectory, "launch.json");
            string installLocation = _scriptEnvironment.InstallLocation;
            string dotnetScriptPath = Path.Combine(installLocation, "dotnet-script.dll").Replace(@"\", "/");
            if (!File.Exists(pathToLaunchFile))
            {
                string launchFileTemplate = TemplateLoader.ReadTemplate("launch.json.template");
                string launchFileContent = launchFileTemplate.Replace("PATH_TO_DOTNET-SCRIPT", dotnetScriptPath);
                File.WriteAllText(pathToLaunchFile, launchFileContent);
                _scriptConsole.WriteSuccess($"...'{pathToLaunchFile}' [Created]");
            }
            else
            {
                _scriptConsole.WriteHighlighted($"...'{pathToLaunchFile}' already exists' [Skipping]");
                var launchFileContent = File.ReadAllText(pathToLaunchFile);
                string pattern = @"^(\s*"")(.*dotnet-script.dll)("").*$";
                if (Regex.IsMatch(launchFileContent, pattern, RegexOptions.Multiline))
                {
                    var newLaunchFileContent = Regex.Replace(launchFileContent, pattern, $"$1{dotnetScriptPath}$3", RegexOptions.Multiline);
                    if (launchFileContent != newLaunchFileContent)
                    {
                        _scriptConsole.WriteHighlighted($"...Fixed path to dotnet-script: '{dotnetScriptPath}' [Updated]");
                        File.WriteAllText(pathToLaunchFile, newLaunchFileContent);
                    }
                }
            }
        }
    }
}
