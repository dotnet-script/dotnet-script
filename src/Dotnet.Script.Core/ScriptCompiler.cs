using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.DependencyModel;
using System.Runtime.InteropServices;

using System.IO;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp;
using Dotnet.Script.Core.Internal;
using Dotnet.Script.Core.Metadata;
using Dotnet.Script.Core.NuGet;
using Dotnet.Script.Core.ProjectSystem;
using Microsoft.DotNet.PlatformAbstractions;


namespace Dotnet.Script.Core
{
    public class ScriptCompiler
    {
        private readonly ScriptLogger _logger;
        private readonly ScriptProjectProvider _scriptProjectProvider;

        protected virtual IEnumerable<Assembly> ReferencedAssemblies => new[]
        {
            typeof(object).GetTypeInfo().Assembly,
            typeof(Enumerable).GetTypeInfo().Assembly
        };

        protected virtual IEnumerable<string> ImportedNamespaces => new[]
        {
            "System",
            "System.IO",
            "System.Collections.Generic",
            "System.Console",
            "System.Diagnostics",
            "System.Dynamic",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Text",
            "System.Threading.Tasks"
        };

        // see: https://github.com/dotnet/roslyn/issues/5501
        protected virtual IEnumerable<string> SuppressedDiagnosticIds => new[] { "CS1701", "CS1702", "CS1705" };

        public ScriptCompiler(ScriptLogger logger, ScriptProjectProvider scriptProjectProvider)
        {
            _logger = logger;
            _scriptProjectProvider = scriptProjectProvider;

            // reset default scripting mode to latest language version to enable C# 7.1 features
            // this is not needed once https://github.com/dotnet/roslyn/pull/21331 ships
            var csharpScriptCompilerType = typeof(CSharpScript).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScriptCompiler");
            var parseOptionsField = csharpScriptCompilerType?.GetField("s_defaultOptions", BindingFlags.Static | BindingFlags.NonPublic);
            parseOptionsField?.SetValue(null, new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script));
        }

        public virtual ScriptOptions CreateScriptOptions(ScriptContext context)
        {
            var opts = ScriptOptions.Default.AddImports(ImportedNamespaces)
                .AddReferences(ReferencedAssemblies)
                .WithSourceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, context.WorkingDirectory))
                .WithMetadataResolver(new NuGetMetadataReferenceResolver(ScriptMetadataResolver.Default.WithBaseDirectory(context.WorkingDirectory)))
                .WithEmitDebugInformation(true)
                .WithFileEncoding(context.Code.Encoding);

            if (!string.IsNullOrWhiteSpace(context.FilePath))
            {
                opts = opts.WithFilePath(context.FilePath);
            }
            
            return opts;
        }

        public virtual ScriptCompilationContext<TReturn> CreateCompilationContext<TReturn, THost>(ScriptContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var platformIdentifier = RuntimeHelper.GetPlatformIdentifier();
            _logger.Verbose($"Current runtime is '{platformIdentifier}'.");

            var opts = CreateScriptOptions(context);

            var runtimeId = RuntimeHelper.GetRuntimeIdentifier();
            var inheritedAssemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId).Where(x =>
                x.FullName.StartsWith("system.", StringComparison.OrdinalIgnoreCase) ||
                x.FullName.StartsWith("microsoft.codeanalysis", StringComparison.OrdinalIgnoreCase) ||
                x.FullName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase));

            foreach (var inheritedAssemblyName in inheritedAssemblyNames)
            {
                _logger.Verbose("Adding reference to an inherited dependency => " + inheritedAssemblyName.FullName);
                var assembly = Assembly.Load(inheritedAssemblyName);
                opts = opts.AddReferences(assembly);
            }

            var pathToProjectJson = Path.Combine(context.WorkingDirectory, Project.FileName);

            IList<RuntimeDependency> runtimeDependencies = new List<RuntimeDependency>();
            if (!File.Exists(pathToProjectJson))
            {
                _logger.Verbose("Unable to find project context for CSX files. Will default to non-context usage.");
                var pathToCsProj = _scriptProjectProvider.CreateProject(context.WorkingDirectory);
                var dependencyResolver = new DependencyResolver(new CommandRunner(_logger), _logger);
                runtimeDependencies = dependencyResolver.GetRuntimeDependencies(pathToCsProj).ToList();
            }
            else
            {
                _logger.Verbose($"Found runtime context for '{pathToProjectJson}'.");
                var dependencyResolver = new LegacyDependencyResolver(_logger);
                runtimeDependencies = dependencyResolver.GetRuntimeDependencies(pathToProjectJson).ToList();
            }

            AssemblyLoadContext.Default.Resolving +=
                (assemblyLoadContext, assemblyName) => MapUnresolvedAssemblyToRuntimeLibrary(runtimeDependencies.ToList(), assemblyLoadContext, assemblyName);


            foreach (var runtimeDep in runtimeDependencies)
            {
                _logger.Verbose("Adding reference to a runtime dependency => " + runtimeDep);
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDep.Path));
            }

            var loader = new InteractiveAssemblyLoader();
            var script = CSharpScript.Create<TReturn>(context.Code.ToString(), opts, typeof(THost), loader);
            var orderedDiagnostics = script.GetDiagnostics(SuppressedDiagnosticIds);

            if (orderedDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                foreach (var diagnostic in orderedDiagnostics)
                {
                    _logger.Log(diagnostic.ToString());
                }

                throw new CompilationErrorException("Script compilation failed due to one or more errors.",
                    orderedDiagnostics.ToImmutableArray());
            }

            return new ScriptCompilationContext<TReturn>(script, context.Code, loader);
        }

        private Assembly MapUnresolvedAssemblyToRuntimeLibrary(IList<RuntimeDependency> runtimeDependencies, AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            var runtimeDependency = runtimeDependencies.SingleOrDefault(r => r.Name == assemblyName.Name);
            if (runtimeDependency != null)
            {
                _logger.Verbose($"Unresolved assembly {assemblyName}. Loading from resolved runtime dependencies at path: {runtimeDependency.Path}");
                return loadContext.LoadFromAssemblyPath(runtimeDependency.Path);
            }
            return null;
        }       
    }
}