using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.LanguageServices
{
    /// <summary>
    /// Builds the Roslyn <see cref="ProjectInfo"/> that backs a REPL submission. The shape mirrors what
    /// OmniSharp does for CSX files, except that the references, imports and resolvers are taken from the
    /// <see cref="ScriptOptions"/> the running session already resolved.
    /// </summary>
    internal static class ReplProjectFactory
    {
        private const string BinderFlagsTypeName = "Microsoft.CodeAnalysis.CSharp.BinderFlags";
        private const string TopLevelBinderFlagsPropertyName = "TopLevelBinderFlags";
        private const string IgnoreCorLibraryDuplicatedTypesFieldName = "IgnoreCorLibraryDuplicatedTypes";
        private const string ReferencesSupersedeLowerVersionsPropertyName = "ReferencesSupersedeLowerVersions_internal_protected_set";

        private static readonly CSharpParseOptions ParseOptions =
            new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Parse, SourceCodeKind.Script);

        // Assembly version mismatches are expected when scripts pull in NuGet packages.
        private static readonly KeyValuePair<string, ReportDiagnostic>[] SuppressedDiagnostics =
        {
            new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
            new KeyValuePair<string, ReportDiagnostic>("CS1702", ReportDiagnostic.Suppress),
            new KeyValuePair<string, ReportDiagnostic>("CS1705", ReportDiagnostic.Suppress)
        };

        public static ProjectInfo Create(string name, string code, ScriptOptions scriptOptions, MetadataReferenceResolver metadataResolver, ProjectId previousSubmission)
        {
            var projectId = ProjectId.CreateNewId(name);
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(projectId, name),
                name,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code ?? string.Empty), VersionStamp.Create())),
                sourceCodeKind: SourceCodeKind.Script);

            var projectReferences = previousSubmission != null
                ? new[] { new ProjectReference(previousSubmission) }
                : Array.Empty<ProjectReference>();

            return ProjectInfo.Create(
                id: projectId,
                version: VersionStamp.Create(),
                name: name,
                assemblyName: name,
                language: LanguageNames.CSharp,
                compilationOptions: CreateCompilationOptions(scriptOptions, metadataResolver),
                parseOptions: ParseOptions,
                documents: new[] { documentInfo },
                projectReferences: projectReferences,
                metadataReferences: ResolveReferences(scriptOptions, metadataResolver),
                isSubmission: true,
                hostObjectType: typeof(InteractiveScriptGlobals));
        }

        /// <summary>
        /// The scripting API resolves references by name lazily while compiling; a workspace compilation
        /// rejects anything still unresolved, so they are resolved up front and dropped when they cannot be.
        /// </summary>
        private static IEnumerable<MetadataReference> ResolveReferences(ScriptOptions scriptOptions, MetadataReferenceResolver resolver)
        {
            var references = new List<MetadataReference>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var reference in scriptOptions.MetadataReferences)
            {
                if (reference is UnresolvedMetadataReference unresolved)
                {
                    foreach (var resolved in resolver.ResolveReference(unresolved.Reference, null, unresolved.Properties))
                    {
                        Add(references, seen, resolved);
                    }
                }
                else
                {
                    Add(references, seen, reference);
                }
            }

            return references;
        }

        private static void Add(List<MetadataReference> references, HashSet<string> seen, MetadataReference reference)
        {
            if (reference == null || (reference.Display != null && !seen.Add(reference.Display)))
            {
                return;
            }

            references.Add(reference);
        }

        private static CSharpCompilationOptions CreateCompilationOptions(ScriptOptions scriptOptions, MetadataReferenceResolver metadataResolver)
        {
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                usings: scriptOptions.Imports,
                allowUnsafe: true,
                specificDiagnosticOptions: SuppressedDiagnostics,
                sourceReferenceResolver: scriptOptions.SourceResolver,
                metadataReferenceResolver: metadataResolver,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                nullableContextOptions: NullableContextOptions.Annotations);

            ApplyScriptBinderFlags(options);

            return options;
        }

        /// <summary>
        /// Roslyn only enables these two script specific behaviours on the scripting API path, and there is
        /// no public way to ask for them, so they are poked in the same way OmniSharp does it. Failing to
        /// apply them only costs duplicate type diagnostics, so any error here is deliberately ignored.
        /// </summary>
        private static void ApplyScriptBinderFlags(CSharpCompilationOptions options)
        {
            try
            {
                var binderFlagsType = typeof(CSharpCompilationOptions).GetTypeInfo().Assembly.GetType(BinderFlagsTypeName);
                var ignoreCorLibraryDuplicatedTypes = binderFlagsType
                    ?.GetField(IgnoreCorLibraryDuplicatedTypesFieldName, BindingFlags.Static | BindingFlags.Public)
                    ?.GetValue(null);

                if (ignoreCorLibraryDuplicatedTypes != null)
                {
                    typeof(CSharpCompilationOptions)
                        .GetProperty(TopLevelBinderFlagsPropertyName, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.SetValue(options, ignoreCorLibraryDuplicatedTypes);
                }

                typeof(CompilationOptions)
                    .GetProperty(ReferencesSupersedeLowerVersionsPropertyName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(options, true);
            }
            catch (Exception)
            {
                // Best effort - the workspace still works without these flags.
            }
        }
    }
}
