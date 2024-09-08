using System;
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
            var sdkReference = ReadSdkFromReferenceDirective(code);
            string sdk = string.Empty;
            if (!string.IsNullOrWhiteSpace(sdkReference))
            {
                sdk = sdkReference;
            }
            return new ParseResult(allPackageReferences) { Sdk = sdk };
        }

        public ParseResult ParseFromFiles(IEnumerable<string> csxFiles)
        {
            var allPackageReferences = new HashSet<PackageReference>();
            string sdk = string.Empty;
            foreach (var csxFile in csxFiles)
            {
                _logger.Debug($"Parsing {csxFile}");
                var fileContent = File.ReadAllText(csxFile);
                allPackageReferences.UnionWith(ReadPackageReferencesFromReferenceDirective(fileContent));
                allPackageReferences.UnionWith(ReadPackageReferencesFromLoadDirective(fileContent));
                var sdkReference = ReadSdkFromReferenceDirective(fileContent);
                if (!string.IsNullOrWhiteSpace(sdkReference))
                {
                    sdk = sdkReference;
                }
            }

            return new ParseResult(allPackageReferences) { Sdk = sdk };
        }

        private static string ReadSdkFromReferenceDirective(string fileContent)
        {
            const string pattern = DirectivePatternPrefix + "r" + SdkDirectivePatternSuffix;
            var match = Regex.Match(fileContent, pattern, RegexOptions.Multiline);
            if (match.Success)
            {
                var sdk = match.Groups[1].Value;
                if (!string.Equals(sdk, "Microsoft.NET.Sdk.Web", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new NotSupportedException($"The sdk '{sdk}' is not supported. Currently 'Microsoft.NET.Sdk.Web' is the only sdk supported.");
                }
            }
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromReferenceDirective(string fileContent)
        {
            const string pattern = DirectivePatternPrefix + "r" + NuGetDirectivePatternSuffix;
            return ReadPackageReferencesFromDirective(pattern, fileContent);
        }

        private static IEnumerable<PackageReference> ReadPackageReferencesFromLoadDirective(string fileContent)
        {
            const string pattern = DirectivePatternPrefix + "load" + NuGetDirectivePatternSuffix;
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