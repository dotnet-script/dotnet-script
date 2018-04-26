using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public class ScriptFilesResolver
    {
        public HashSet<string> GetScriptFiles(string csxFile)
        {
            HashSet<string> result = new HashSet<string>();
            Process(csxFile, result);
            return result;
        }

        private void Process(string csxFile, HashSet<string> result)
        {
            if (result.Add(csxFile))
            {
                var loadDirectives = GetLoadDirectives(csxFile);
                foreach (var loadDirective in loadDirectives)
                {
                    string referencedScript;
                    if (!Path.IsPathRooted(loadDirective))
                    {
                        referencedScript = Path.GetFullPath((new Uri(Path.Combine(Path.GetDirectoryName(csxFile), loadDirective))).LocalPath);
                    }
                    else
                    {
                        referencedScript = loadDirective;
                    }

                    Process(referencedScript, result);
                }
            }
        }

        private static string[] GetLoadDirectives(string csxFile)
        {
            var content = ReadFile(csxFile);
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


        private static string ReadFile(string pathToFile)
        {
            using (var fileStream = new FileStream(pathToFile, FileMode.Open))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
