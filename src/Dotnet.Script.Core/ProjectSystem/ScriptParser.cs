using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dotnet.Script.Core.ProjectSystem
{
    /// <summary>
    /// A class that is capable of parsing a set of script files 
    /// and return information about NuGet references and the target framework.
    /// </summary>
    public class ScriptParser 
    {
        private readonly ScriptLogger _logger;

        /// <summary>
        /// Initializes a new insstance of the <see cref="ScriptParser"/> class.
        /// </summary>        
        public ScriptParser(ScriptLogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public ParseResult ParseFrom(IEnumerable<string> csxFiles)
        {
            HashSet<PackageReference> allPackageReferences = new HashSet<PackageReference>();
            string currentTargetFramework = null;
            foreach (var csxFile in csxFiles)
            {
                _logger.Verbose($"Parsing {csxFile}");
                var fileContent = ReadFile(csxFile);
                var packageReferences = ReadPackageReferences(fileContent);
                allPackageReferences.UnionWith(packageReferences);
                string targetFramework = ReadTargetFramework(fileContent);
                if (targetFramework != null)
                {
                    if (currentTargetFramework != null && targetFramework != currentTargetFramework)
                    {
                        _logger.Verbose($"Found multiple target frameworks. Using {currentTargetFramework}.");
                    }
                    else
                    {
                        currentTargetFramework = targetFramework;
                    }
                }
            }

            return new ParseResult(allPackageReferences, currentTargetFramework);
        }

        private IEnumerable<PackageReference> ReadPackageReferences(string fileContent)
        {
            const string pattern = @"^\s*#r\s*""nuget:\s*(.+)\s*,\s*(.*)""";
            var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (var match in matches.Cast<Match>())
            {
                var id = match.Groups[1].Value;
                var version = match.Groups[2].Value;
                var packageReference = new PackageReference(id, version);
                yield return packageReference;
            }
        }

        private string ReadTargetFramework(string fileContent)
        {
            const string pattern = @"^\s*#!\s*""(.*)""";
            var match = Regex.Match(fileContent, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
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