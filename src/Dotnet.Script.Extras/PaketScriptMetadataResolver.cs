using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

namespace Dotnet.Script.Extras
{
    public class PaketScriptMetadataResolver : MetadataReferenceResolver
    {
        private readonly StringBuilder _paketDependencies = new StringBuilder().AppendLine("source https://api.nuget.org/v3/index.json");
        private static readonly string PaketDependencies = "paket.dependencies";
        private static readonly string PaketLoadFolder = ".paket/load/";
        private static readonly string PaketPrefix = "paket: ";
        private readonly ScriptMetadataResolver _inner;
        private readonly HashSet<string> _resolvedReferences = new HashSet<string>();

        public PaketScriptMetadataResolver(string code, string workingDirectory = null, string paketPath = ".paket/paket.exe", string tfm = "netstandard16")
        {
            workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
            _inner = ScriptMetadataResolver.Default;

            var requiredReferences = GetReferenceDefinitions(code).Where(x => x.StartsWith(PaketPrefix));
            foreach (var requiredReference in requiredReferences)
            {
                _paketDependencies.AppendLine(requiredReference.Replace(PaketPrefix, "nuget "));
            }

            File.WriteAllText(Path.Combine(workingDirectory, PaketDependencies), _paketDependencies.ToString());

            var processStartInfo = new ProcessStartInfo(paketPath, $"install --generate-load-scripts load-script-framework {tfm}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = _inner.BaseDirectory
            };

            using (var process = new Process() { StartInfo = processStartInfo })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    Console.WriteLine(e.Data);
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    Console.Error.WriteLine(e.Data);
                };
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();
            }

            var restoredReferencesDefinition = Path.Combine(workingDirectory, $"{PaketLoadFolder}{tfm}/main.group.csx");
            if (File.Exists(restoredReferencesDefinition))
            {
                var fileContent = File.ReadAllText(restoredReferencesDefinition);
                var restoredRefs = GetReferenceDefinitions(fileContent);
                foreach (var restoredRef in restoredRefs)
                {
                    if (restoredRef.StartsWith(".."))
                    {
                        _resolvedReferences.Add(Path.Combine(workingDirectory, $"{PaketLoadFolder}{tfm}", restoredRef));
                    }
                    else
                    {
                        //skip GAC by design
                    }
                }
            }

        }

        private static IEnumerable<string> GetReferenceDefinitions(string fileContent)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script));
            return syntaxTree.GetCompilationUnitRoot().GetReferenceDirectives().Select(x => x.File.ToString().Replace("\"", string.Empty));
        }

        public ScriptOptions CreateScriptOptions(ScriptOptions scriptOptions)
        {
            return scriptOptions.
                WithMetadataResolver(this).
                WithReferences(_resolvedReferences.Select(x => MetadataReference.CreateFromFile(x)));
        }

        public override bool Equals(object other)
        {
            return _inner.Equals(other);
        }

        public override int GetHashCode()
        {
            return _inner.GetHashCode();
        }

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            return _inner.ResolveMissingAssembly(definition, referenceIdentity);
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            if (reference.StartsWith(PaketPrefix))
            {
                // dummy reference
                // this needs to return somehting or the compiler will complain
                return ImmutableArray.Create(MetadataReference.CreateFromFile(typeof(PaketScriptMetadataResolver).GetTypeInfo().Assembly.Location));
            }
            else
            {
                return _inner.ResolveReference(reference, baseFilePath, properties);
            }
        }
    }
}