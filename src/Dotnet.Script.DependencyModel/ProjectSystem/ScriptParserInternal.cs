using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.ProjectSystem
{
    partial class ScriptParser
    {
        const string Hws = @"[\x20\t]*"; // hws = horizontal whitespace

        const string NuGetPattern = @"nuget:"
                                    // https://github.com/NuGet/docs.microsoft.com-nuget/issues/543#issue-270039223
                                  + Hws + @"(\w+(?:[_.-]\w+)*)"
                                  + @"(?:" + Hws + "," + Hws + @"(.+?))?";

        const string WholeNuGetPattern = @"^" + NuGetPattern + @"$";

        const string DirectivePatternPrefix = @"^" + Hws + @"#";
        const string DirectivePatternSuffix = Hws + @"""" + NuGetPattern + @"""";

        internal static bool TryParseNuGetPackageReference(string input,
                                                           out string id, out string version)
        {
            bool success;
            (success, id, version) =
                Regex.Match(input, WholeNuGetPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                is {} match && match.Success
                ? (true, match.Groups[1].Value, match.Groups[2].Value)
                : default;
            return success;
        }
    }
}
