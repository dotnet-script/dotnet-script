using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{    
    public class ScriptParser 
    {
        private readonly Logger _logger;
        
        public ScriptParser(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<ScriptParser>();
        }

        public ParseResult ParseFromCode(string code)
        {
            string currentTargetFramework = null;
            var allPackageReferences = new HashSet<PackageReference>();            
            allPackageReferences.UnionWith(ReadPackageReferencesFromReferenceDirective(code));
            allPackageReferences.UnionWith(ReadPackageReferencesFromLoadDirective(code));
            string targetFramework = ReadTargetFramework(code);
            if (targetFramework != null)
            {
                if (currentTargetFramework != null && targetFramework != currentTargetFramework)
                {
                    _logger.Debug($"Found multiple target frameworks. Using {currentTargetFramework}.");
                }
                else
                {
                    currentTargetFramework = targetFramework;
                }
            }

            return new ParseResult(allPackageReferences, currentTargetFramework);
        }

        public ParseResult ParseFromFiles(IEnumerable<string> csxFiles)
        {
            var allPackageReferences = new HashSet<PackageReference>();
            string currentTargetFramework = null;
            foreach (var csxFile in csxFiles)
            {
                _logger.Debug($"Parsing {csxFile}");
                var fileContent = File.ReadAllText(csxFile);
                allPackageReferences.UnionWith(ReadPackageReferencesFromReferenceDirective(fileContent));
                allPackageReferences.UnionWith(ReadPackageReferencesFromLoadDirective(fileContent));
                string targetFramework = ReadTargetFramework(fileContent);
                if (targetFramework != null)
                {
                    if (currentTargetFramework != null && targetFramework != currentTargetFramework)
                    {
                        _logger.Debug($"Found multiple target frameworks. Using {currentTargetFramework}.");
                    }
                    else
                    {
                        currentTargetFramework = targetFramework;
                    }
                }
            }

            return new ParseResult(allPackageReferences, currentTargetFramework);
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromReferenceDirective(string fileContent)
        {
            const string pattern = @"^\s*#r\s*""nuget:\s*(.+)\s*,\s*(.*)""";
            var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (var match in matches.Cast<Match>())
            {
                var id = match.Groups[1].Value;
                var version = match.Groups[2].Value;
                var packageReference = new PackageReference(id, version, PackageOrigin.ReferenceDirective);
                yield return packageReference;
            }
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromLoadDirective(string fileContent)
        {
            const string pattern = @"^\s*#load\s*""nuget:\s*(.+)\s*,\s*(.*)""";
            var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (var match in matches.Cast<Match>())
            {
                var id = match.Groups[1].Value;
                var version = match.Groups[2].Value;
                var packageReference = new PackageReference(id, version, PackageOrigin.LoadDirective);
                yield return packageReference;
            }
        }

        private static string ReadTargetFramework(string fileContent)
        {
            const string pattern = @"^\s*#!\s*""(.*)""";
            var match = Regex.Match(fileContent, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
    }
}