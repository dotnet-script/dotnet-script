using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.ScriptPackage
{
    public class ScriptFilesDependencyResolver
    {
        private readonly Logger _logger;

        private static readonly Regex TargetFrameworkMatcher =
            new Regex(@"^.*(?:contentFiles|content)[\/,\\]csx[\/,\\](.*)[\/,\\].*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RootPathMatcher =
            new Regex(@"(^.*(?:contentFiles|content)[\/,\\]csx[\/,\\].*[\/,\\]).*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ScriptFilesDependencyResolver(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<ScriptFilesDependencyResolver>();
        }



        public string[] GetScriptFileDependencies(string packagePath)
        {
            var allScriptFiles = Directory.GetFiles(packagePath, "*.csx", SearchOption.AllDirectories);

            if (allScriptFiles.Length == 0)
            {
                return Array.Empty<string>();
            }

            _logger.Debug($"Processing script file dependencies from '{packagePath}'");
            return ProcessScriptFiles(allScriptFiles);
        }

        private string[] ProcessScriptFiles(string[] allScriptFiles)
        {
            var result = new List<string>();
            var scriptFilesPerTargetFramework = GetScriptFilesPerTargetFramework(allScriptFiles);
            var scriptFiles = GetScriptFilesMatchingCurrentRuntime(scriptFilesPerTargetFramework);
            if (scriptFiles.Length == 0)
            {
                _logger.Debug("No script files found matching the current runtime.");
                return Array.Empty<string>();
            }

            var rootPath = GetRootPath(scriptFiles[0]);
            string entryPointScriptFile = GetEntryPointScript(rootPath);
            if (entryPointScriptFile != null)
            {
                _logger.Debug($"Adding entry point script file '{entryPointScriptFile}'");
                result.Add(entryPointScriptFile);
            }
            else
            {
                _logger.Debug($"Unable to determine entry point script file. Adding all files from {rootPath}");
                result.AddRange(scriptFiles);
            }
            return result.ToArray();
        }

        private string GetEntryPointScript(string rootPath)
        {
            var rootfiles = Directory.GetFiles(rootPath, "*.csx");
            if (rootfiles.Length == 1)
            {
                return rootfiles[0];
            }

            if (rootfiles.Length > 1)
            {
                return rootfiles.SingleOrDefault(rf => rf.ToLower() == "main.csx");
            }

            return null;
        }

        private string[] GetScriptFilesMatchingCurrentRuntime(IDictionary<string, List<string>> filesPerTargetFramework)
        {
            //We keep this super simple for now

            if (filesPerTargetFramework.TryGetValue("any", out var files))
            {
                return files.ToArray();
            }

            if (filesPerTargetFramework.TryGetValue("netstandard2.0", out files))
            {
                return files.ToArray();
            }

            return Array.Empty<string>();
        }


        private static IDictionary<string, List<string>> GetScriptFilesPerTargetFramework(string[] scriptFiles)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var scriptFile in scriptFiles)
            {
                var match = TargetFrameworkMatcher.Match(scriptFile);
                if (match.Success)
                {
                    var targetFramework = match.Groups[1].Value;
                    if (!result.TryGetValue(targetFramework, out var files))
                    {
                        files = new List<string>();
                        result.Add(targetFramework, files);
                    }
                    result[targetFramework].Add(match.Groups[0].Value);
                }
            }
            return result;
        }

        private static string GetRootPath(string pathToScriptFile)
        {
            var match = RootPathMatcher.Match(pathToScriptFile);
            return match.Groups[1].Value;
        }
    }
}