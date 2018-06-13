using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Dotnet.Script.Core
{
    public class ScriptEmitResult
    {
        private ScriptEmitResult() { }

        public ScriptEmitResult(MemoryStream peStream, MemoryStream pdbStream, IEnumerable<MetadataReference> directiveReferences)
        {
            PeStream = peStream;
            PdbStream = pdbStream;
            DirectiveReferences = directiveReferences.ToImmutableArray();
        }

        public MemoryStream PeStream { get; }
        public MemoryStream PdbStream { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; } = ImmutableArray.Create<Diagnostic>();
        public ImmutableArray<MetadataReference> DirectiveReferences { get; } = ImmutableArray.Create<MetadataReference>();
        public bool Success => !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        public static ScriptEmitResult Error(IEnumerable<Diagnostic> diagnostics)
        {
            var result = new ScriptEmitResult
            {
                Diagnostics = diagnostics.ToImmutableArray()
            };
            return result;
        }
    }
}
