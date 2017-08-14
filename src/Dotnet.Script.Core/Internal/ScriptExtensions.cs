using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dotnet.Script.Core.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Dotnet.Script.Core.Internal
{
    internal static class ScriptExtensions
    {
        public static IEnumerable<Diagnostic> GetDiagnostics<T>(this Script<T> script, IEnumerable<string> suppressedDiagnosticIds)
        {
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics().Where(d => !suppressedDiagnosticIds.Contains(d.Id));
            var orderedDiagnostics = diagnostics.OrderBy((d1, d2) =>
            {
                var severityDiff = (int)d2.Severity - (int)d1.Severity;
                return severityDiff != 0 ? severityDiff : d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start;
            });

            return orderedDiagnostics;
        }
    }
}