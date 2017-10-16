using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Extensions.DependencyModel;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp;
using Dotnet.Script.Core.Internal;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.NuGet;
using Dotnet.Script.DependencyModel.Runtime;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using System.Threading.Tasks;
using System.Threading;

namespace Dotnet.Script.Core
{
    public class ScriptCompiler
    {
        private readonly ScriptLogger _logger;
        private readonly RuntimeDependencyResolver _runtimeDependencyResolver;

        protected virtual IEnumerable<Assembly> ReferencedAssemblies => new[]
        {
            typeof(object).GetTypeInfo().Assembly,
            typeof(Enumerable).GetTypeInfo().Assembly
        };

        static ScriptCompiler()
        {
            // reset default scripting mode to latest language version to enable C# 7.1 features
            // this is not needed once https://github.com/dotnet/roslyn/pull/21331 ships
            var csharpScriptCompilerType = typeof(CSharpScript).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScriptCompiler");
            var parseOptionsField = csharpScriptCompilerType?.GetField("s_defaultOptions", BindingFlags.Static | BindingFlags.NonPublic);
            parseOptionsField?.SetValue(null, new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script));

            // force Roslyn to use ReferenceManager for the first time
            Task.Run(() =>
            {
                CSharpScript.Create<object>("1", ScriptOptions.Default, typeof(CommandLineScriptGlobals), new InteractiveAssemblyLoader()).RunAsync(new CommandLineScriptGlobals(Console.Out, CSharpObjectFormatter.Instance)).GetAwaiter().GetResult();
            });
        }

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

        public ScriptCompiler(ScriptLogger logger, RuntimeDependencyResolver runtimeDependencyResolver)
        {
            _logger = logger;
            _runtimeDependencyResolver = runtimeDependencyResolver;
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


            IList<RuntimeDependency> runtimeDependencies =
                _runtimeDependencyResolver.GetDependencies(context.WorkingDirectory).ToList();

            foreach (var runtimeDependency in runtimeDependencies)
            {
                var runtimeDependencyAssemblyName = runtimeDependency.Name;
                if (!inheritedAssemblyNames.Any(an => an.Name == runtimeDependencyAssemblyName.Name))
                {
                    _logger.Verbose("Adding reference to a runtime dependency => " + runtimeDependency);
                    opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeDependency.Path));
                }
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
    }
}