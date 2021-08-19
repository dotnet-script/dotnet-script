using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;
using Gapotchenko.FX.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script.Core
{
    public class ScriptRunner
    {
        protected Logger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole ScriptConsole;
        protected ScriptEnvironment _scriptEnvironment;

        public ScriptRunner(ScriptCompiler scriptCompiler, LogFactory logFactory, ScriptConsole scriptConsole)
        {
            Logger = logFactory.CreateLogger<ScriptRunner>();
            ScriptCompiler = scriptCompiler;
            ScriptConsole = scriptConsole;
            _scriptEnvironment = ScriptEnvironment.Default;
        }

#if NETCOREAPP
#nullable enable
        /// <summary>
        /// Gets or sets a custom assembly load context to use for script execution.
        /// </summary>
        public AssemblyLoadContext? AssemblyLoadContext { get; init; }
#nullable restore
#endif

        public async Task<TReturn> Execute<TReturn>(string dllPath, IEnumerable<string> commandLineArgs)
        {
#if NETCOREAPP
            var assemblyLoadContext = AssemblyLoadContext;
            var assemblyLoadPal = assemblyLoadContext != null ? new AssemblyLoadPal(assemblyLoadContext) : AssemblyLoadPal.ForCurrentAppDomain;
#else
            var assemblyLoadPal = AssemblyLoadPal.ForCurrentAppDomain;            
#endif

            var runtimeDeps = ScriptCompiler.RuntimeDependencyResolver.GetDependenciesForLibrary(dllPath);
            var runtimeDepsMap = ScriptCompiler.CreateScriptDependenciesMap(runtimeDeps);
            var assembly = assemblyLoadPal.LoadFrom(dllPath); // this needs to be called prior to 'AssemblyLoadPal.Resolving' event handler added

#if NETCOREAPP
            using var assemblyAutoLoader = assemblyLoadContext != null ? new AssemblyAutoLoader(assemblyLoadContext) : null;
            assemblyAutoLoader?.AddAssembly(assembly);
#endif

#if NETCOREAPP3_0_OR_GREATER
            using var contextualReflectionScope = assemblyLoadContext != null ? assemblyLoadContext.EnterContextualReflection() : default;
#endif

            Assembly OnResolve(AssemblyLoadPal sender, AssemblyLoadPal.ResolvingEventArgs args) => ResolveAssembly(sender, args, runtimeDepsMap);

            assemblyLoadPal.Resolving += OnResolve;
            try
            {
                var type = assembly.GetType("Submission#0");
                var method = type.GetMethod("<Factory>", BindingFlags.Static | BindingFlags.Public);

                var globals = new CommandLineScriptGlobals(ScriptConsole.Out, CSharpObjectFormatter.Instance);
                foreach (var arg in commandLineArgs)
                    globals.Args.Add(arg);

                var submissionStates = new object[2];
                submissionStates[0] = globals;

                var resultTask = method.Invoke(null, new[] { submissionStates }) as Task<TReturn>;
                try
                {
                    _ = await resultTask;
                }
                catch (System.Exception ex)
                {
                    ScriptConsole.WriteError(ex.ToString());
                    throw new ScriptRuntimeException("Script execution resulted in an exception.", ex);
                }

                return await resultTask;
            }
            finally
            {
                assemblyLoadPal.Resolving -= OnResolve;
            }
        }

        public Task<TReturn> Execute<TReturn>(ScriptContext context)
        {
            var globals = new CommandLineScriptGlobals(ScriptConsole.Out, CSharpObjectFormatter.Instance);

            foreach (var arg in context.Args)
                globals.Args.Add(arg);

            return Execute<TReturn, CommandLineScriptGlobals>(context, globals);
        }

        public virtual Task<TReturn> Execute<TReturn, THost>(ScriptContext context, THost host)
        {
            var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, THost>(context);
            ScriptConsole.WriteDiagnostics(compilationContext.Warnings, compilationContext.Errors);

            if (compilationContext.Errors.Any())
            {
                throw new CompilationErrorException("Script compilation failed due to one or more errors.", compilationContext.Errors.ToImmutableArray());
            }

            return Execute(compilationContext, host);
        }

        public virtual async Task<TReturn> Execute<TReturn, THost>(ScriptCompilationContext<TReturn> compilationContext, THost host)
        {
            var scriptResult = await compilationContext.Script.RunAsync(host, ex => true).ConfigureAwait(false);
            return ProcessScriptState(scriptResult);
        }

        internal Assembly ResolveAssembly(AssemblyLoadPal pal, AssemblyLoadPal.ResolvingEventArgs args, Dictionary<string, RuntimeAssembly> runtimeDepsMap)
        {
            var result = runtimeDepsMap.TryGetValue(args.Name.Name, out RuntimeAssembly runtimeAssembly);
            if (!result) return null;
            var loadedAssembly = pal.LoadFrom(runtimeAssembly.Path);
            return loadedAssembly;
        }

        protected TReturn ProcessScriptState<TReturn>(ScriptState<TReturn> scriptState)
        {
            if (scriptState.Exception != null)
            {
                // once Roslyn ships with this, we can format he exception using CSharpObjectFormatter
                // https://github.com/dotnet/roslyn/blob/4175350b87f928e136cbb14c2668b7cb3338d5a1/src/Scripting/Core/Hosting/CommonMemberFilter.cs#L18
                ScriptConsole.WriteError(scriptState.Exception.ToString());
                throw new ScriptRuntimeException("Script execution resulted in an exception.", scriptState.Exception);
            }

            return scriptState.ReturnValue;
        }
    }
}
