using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public class ScriptFilesResolver
    {
        public IList<string> GetScriptFiles(string csxFile)
        {
            return GetScriptFiles(new[] { csxFile });
        }

        public IList<string> GetScriptFilesFromCode(string code)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            return GetScriptFiles(from loadDirective in GetLoadDirectives(code)
                                  select GetFullPath(currentDirectory, loadDirective));
        }

        static IList<string> GetScriptFiles(IEnumerable<string> csxFiles)
        {
            var fileSet = new HashSet<string>(); // potentially unordered
            var result = new List<string>();     // in depth-first order
            WalkLoadTree(csxFiles);
            return result;

            void WalkLoadTree(IEnumerable<string> files)
            {
                foreach (var csxFile in files)
                {
                    if (fileSet.Add(csxFile))
                    {
                        result.Add(csxFile);

                        WalkLoadTree(from loadDirective in GetLoadDirectives(File.ReadAllText(csxFile))
                                     select GetFullPath(Path.GetDirectoryName(csxFile), loadDirective));
                    }
                }
            }
        }

        static string GetFullPath(string basePath, string path)
        {
            return !Path.IsPathRooted(path)
                 ? Path.GetFullPath(new Uri(Path.Combine(basePath, path)).LocalPath)
                 : path;
        }

        private static string[] GetLoadDirectives(string content)
        {            
            var matches = Regex.Matches(content, @"^\s*#load\s*""\s*(.+)\s*""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            List<string> result = new List<string>();
            foreach (var match in matches.Cast<Match>())
            {
                var value = match.Groups[1].Value;
                if (value.StartsWith("nuget", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                result.Add(value);
            }

            return result.ToArray();
        }
    }
}
