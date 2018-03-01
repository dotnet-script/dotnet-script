using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Extensions.DependencyModel;
using Microsoft.CodeAnalysis.CSharp;
using Dotnet.Script.Core.Internal;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.NuGet;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuntimeAssembly = Dotnet.Script.DependencyModel.Runtime.RuntimeAssembly;

namespace Dotnet.Script.Core
{
    public class ScriptCompiler
    {
        // Note: Windows only, Mac and Linux needs something else?
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        protected virtual IEnumerable<Assembly> ReferencedAssemblies => new[]
        {
            typeof(object).GetTypeInfo().Assembly,
            typeof(Enumerable).GetTypeInfo().Assembly
        };

        static ScriptCompiler()
        {
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

        public CSharpParseOptions ParseOptions { get; } = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);

        public RuntimeDependencyResolver RuntimeDependencyResolver { get; }

        public ScriptLogger Logger { get; }

        public ScriptCompiler(ScriptLogger logger, RuntimeDependencyResolver runtimeDependencyResolver)
        {
            Logger = logger;
            RuntimeDependencyResolver = runtimeDependencyResolver;
        }

        public virtual ScriptOptions CreateScriptOptions(ScriptContext context, IList<RuntimeDependency> runtimeDependencies)
        {
            var scriptMap = runtimeDependencies.ToDictionary(rdt => rdt.Name, rdt => rdt.Scripts);
            var opts = ScriptOptions.Default.AddImports(ImportedNamespaces)
                .AddReferences(ReferencedAssemblies)
                .WithSourceResolver(new NuGetSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, context.WorkingDirectory),scriptMap))
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
            Logger.Verbose($"Current runtime is '{platformIdentifier}'.");

            var runtimeDependencies = RuntimeDependencyResolver.GetDependencies(context.WorkingDirectory, context.ScriptMode, context.Code.ToString()).ToArray();
            var opts = CreateScriptOptions(context, runtimeDependencies.ToList());

            var runtimeId = RuntimeHelper.GetRuntimeIdentifier();
            var inheritedAssemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId).Where(x =>
                    x.FullName.StartsWith("microsoft.codeanalysis", StringComparison.OrdinalIgnoreCase)).ToArray();

            // Build up a dependency map that picks runtime assembly with the highest version.
            // This aligns with the CoreCLR that uses the highest version strategy.
            var dependencyMap = runtimeDependencies.SelectMany(rtd => rtd.Assemblies).Distinct().GroupBy(rdt => rdt.Name.Name, rdt => rdt)
                .Select(gr => new { Name = gr.Key, ResolvedRuntimeAssembly = gr.OrderBy(rdt => rdt.Name.Version).Last() })
                .ToDictionary(f => f.Name, f => f.ResolvedRuntimeAssembly, StringComparer.OrdinalIgnoreCase);

            foreach (var runtimeAssembly in dependencyMap.Values)
            {                                
                Logger.Verbose("Adding reference to a runtime dependency => " + runtimeAssembly);
                opts = opts.AddReferences(MetadataReference.CreateFromFile(runtimeAssembly.Path));
            }

            foreach (var nativeAsset in runtimeDependencies.SelectMany(rtd => rtd.NativeAssets).Distinct())
            {
                if (RuntimeHelper.IsWindows())
                {
                    LoadLibrary(nativeAsset);
                }
            }

            foreach (var inheritedAssemblyName in inheritedAssemblyNames)
            {
                // Always prefer the resolved runtime dependency rather than the inherited assembly.
                if(!dependencyMap.ContainsKey(inheritedAssemblyName.Name))                
                {
                    Logger.Verbose($"Adding reference to an inherited dependency => {inheritedAssemblyName.FullName}");
                    var assembly = Assembly.Load(inheritedAssemblyName);
                    opts = opts.AddReferences(assembly);                    
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += 
                (sender, args) => MapUnresolvedAssemblyToRuntimeLibrary(dependencyMap, args);

            // when processing raw code, make sure we inject new lines after preprocessor directives
            string code;
            if (context.FilePath == null)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(context.Code, ParseOptions);
                var syntaxRewriter = new PreprocessorLineRewriter();
                var newSyntaxTree = syntaxRewriter.Visit(syntaxTree.GetRoot());
                code = newSyntaxTree.ToFullString();
            }
            else
            {
                code = context.Code.ToString();
            }

            var loader = new InteractiveAssemblyLoader();
            var script = CSharpScript.Create<TReturn>(code, opts, typeof(THost), loader);

            if (context.OptimizationLevel == OptimizationLevel.Release)
            {
                Logger.Verbose("Configuration/Optimization mode: Release");                
                SetReleaseOptimizationLevel(script.GetCompilation());
            }
            else
            {
                Logger.Verbose("Configuration/Optimization mode: Debug");
            }

            var orderedDiagnostics = script.GetDiagnostics(SuppressedDiagnosticIds);

            if (orderedDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new CompilationErrorException("Script compilation failed due to one or more errors.",
                    orderedDiagnostics.ToImmutableArray());
            }

            return new ScriptCompilationContext<TReturn>(script, context.Code, loader, opts);
        }

        private static void SetReleaseOptimizationLevel(Compilation compilation)
        {            
            var compilationOptionsField = typeof(CSharpCompilation).GetTypeInfo().GetDeclaredField("_options");
            var compilationOptions = (CSharpCompilationOptions)compilationOptionsField.GetValue(compilation);
            compilationOptions = compilationOptions.WithOptimizationLevel(OptimizationLevel.Release);
            compilationOptionsField.SetValue(compilation, compilationOptions);        
        }
        
        private Assembly MapUnresolvedAssemblyToRuntimeLibrary(IDictionary<string, RuntimeAssembly> dependencyMap, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            if (dependencyMap.TryGetValue(assemblyName.Name, out var runtimeAssembly))
            {
                if (runtimeAssembly.Name.Version > assemblyName.Version)
                {
                    Logger.Log($"Redirecting {assemblyName} to {runtimeAssembly.Name}");                    
                    return Assembly.LoadFile(runtimeAssembly.Path);
                }
            }

            return null;
        }
    }
}