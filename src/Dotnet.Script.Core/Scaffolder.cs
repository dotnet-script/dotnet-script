using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dotnet.Script.Core.Templates;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Newtonsoft.Json.Linq;

namespace Dotnet.Script.Core
{
    public class Scaffolder
    {
        private ScriptEnvironment _scriptEnvironment;        
        private const string DefaultScriptFileName = "main.csx";
        private ScriptConsole _scriptConsole = ScriptConsole.Default;

        public Scaffolder()
        {
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
            _scriptConsole.Out.WriteLine($"Creating '{fileName}'");
            if(!Path.HasExtension(fileName))
            {
                fileName = Path.ChangeExtension(fileName, ".csx");
            }
            var pathToScriptFile = Path.Combine(currentDirectory, fileName);
            if (!File.Exists(pathToScriptFile))
            {
                var scriptFileTemplate = TemplateLoader.ReadTemplate("helloworld.csx.template");
                File.WriteAllText(pathToScriptFile, scriptFileTemplate);
                _scriptConsole.Out.WriteLine($"...'{pathToScriptFile}' [Created]");
            }
            else
            {
                _scriptConsole.Out.WriteLine($"...'{pathToScriptFile}' already exists [Skipping]");
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
                CreateNewScriptFile(fileName,currentWorkingDirectory);
            }
        }

        private void CreateDefaultScriptFile(string currentWorkingDirectory)
        {
            _scriptConsole.Out.WriteLine($"Creating default script file '{DefaultScriptFileName}'");
            if (Directory.GetFiles(currentWorkingDirectory, "*.csx").Any())
            {
                _scriptConsole.Out.WriteLine("...Folder already contains one or more script files [Skipping]");
            }
            else
            {
                CreateNewScriptFile(DefaultScriptFileName, currentWorkingDirectory);
            }
        }

        private void CreateOmniSharpConfigurationFile(string currentWorkingDirectory)
        {
            _scriptConsole.Out.WriteLine("Creating OmniSharp configuration file");
            string pathToOmniSharpJson = Path.Combine(currentWorkingDirectory, "omnisharp.json");
            if (!File.Exists(pathToOmniSharpJson))
            {
                var omniSharpFileTemplate = TemplateLoader.ReadTemplate("omnisharp.json.template");
                JObject settings = JObject.Parse(omniSharpFileTemplate);
                settings["script"]["defaultTargetFramework"] = _scriptEnvironment.TargetFramework;
                File.WriteAllText(pathToOmniSharpJson, settings.ToString());
                _scriptConsole.Out.WriteLine($"...'{pathToOmniSharpJson}' [Created]");
            }
            else
            {
                _scriptConsole.Out.WriteLine($"...'{pathToOmniSharpJson} already exists' [Skipping]");
            }
        }

        private void CreateLaunchConfiguration(string currentWorkingDirectory)
        {
            string vsCodeDirectory = Path.Combine(currentWorkingDirectory, ".vscode");
            if (!Directory.Exists(vsCodeDirectory))
            {
                Directory.CreateDirectory(vsCodeDirectory);
            }

            _scriptConsole.Out.WriteLine("Creating VS Code launch configuration file");
            string pathToLaunchFile = Path.Combine(vsCodeDirectory, "launch.json");
            string installLocation = _scriptEnvironment.InstallLocation;
            string dotnetScriptPath = Path.Combine(installLocation, "dotnet-script.dll").Replace(@"\", "/");
            if (!File.Exists(pathToLaunchFile))
            {                
                string lauchFileTemplate = TemplateLoader.ReadTemplate("launch.json.template");
                string launchFileContent = lauchFileTemplate.Replace("PATH_TO_DOTNET-SCRIPT", dotnetScriptPath);
                File.WriteAllText(pathToLaunchFile, launchFileContent);
                _scriptConsole.Out.WriteLine($"...'{pathToLaunchFile}' [Created]");
            }
            else
            {
                _scriptConsole.Out.WriteLine($"...'{pathToLaunchFile}' already exists' [Skipping]");
                var launchFileContent = File.ReadAllText(pathToLaunchFile);
                string pattern = @"^(\s*"")(.*dotnet-script.dll)("").*$";
                if (Regex.IsMatch(launchFileContent, pattern, RegexOptions.Multiline))
                {
                    var newLaunchFileContent = Regex.Replace(launchFileContent, pattern, $"$1{dotnetScriptPath}$3", RegexOptions.Multiline);
                    if (launchFileContent != newLaunchFileContent)
                    {
                        _scriptConsole.Out.WriteLine($"...Fixed path to dotnet-script: '{dotnetScriptPath}' [Updated]");
                        File.WriteAllText(pathToLaunchFile, newLaunchFileContent);
                    }
                }
            }
        }
    }
}
