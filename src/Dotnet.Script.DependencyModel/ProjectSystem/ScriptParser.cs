using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    public partial class ScriptParser
    {
        private readonly Logger _logger;

        public ScriptParser(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<ScriptParser>();
        }

        public ParseResult ParseFromCode(string code)
        {
            var allPackageReferences = new HashSet<PackageReference>();
            allPackageReferences.UnionWith(ReadPackageReferencesFromReferenceDirective(code));
            allPackageReferences.UnionWith(ReadPackageReferencesFromLoadDirective(code));
            return new ParseResult(allPackageReferences);
        }

        public ParseResult ParseFromFiles(IEnumerable<string> csxFiles)
        {
            var allPackageReferences = new HashSet<PackageReference>();
            foreach (var csxFile in csxFiles)
            {
                _logger.Debug($"Parsing {csxFile}");
                var fileContent = File.ReadAllText(csxFile);
                allPackageReferences.UnionWith(ReadPackageReferencesFromReferenceDirective(fileContent));
                allPackageReferences.UnionWith(ReadPackageReferencesFromLoadDirective(fileContent));
            }

            return new ParseResult(allPackageReferences);
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromReferenceDirective(string fileContent)
        {
            const string pattern = DirectivePatternPrefix + "r" + DirectivePatternSuffix;
            return ReadPackageReferencesFromDirective(pattern, fileContent);
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromLoadDirective(string fileContent)
        {
            const string pattern = DirectivePatternPrefix + "load" + DirectivePatternSuffix;
            return ReadPackageReferencesFromDirective(pattern, fileContent);
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromDirective(
            string pattern, string fileContent)
        {
            var matches = Regex.Matches(fileContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (var match in matches.Cast<Match>())
            {
                var id = match.Groups[1].Value;
                var version = match.Groups[2].Value;
                var packageReference = new PackageReference(id, version);
                yield return packageReference;
            }
        }
    }
}