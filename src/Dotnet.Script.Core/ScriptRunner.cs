using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script.Core
{
    public class ScriptRunner
    {
        protected ScriptLogger Logger;
        protected ScriptCompiler ScriptCompiler;
        protected ScriptConsole ScriptConsole;

        public ScriptRunner(ScriptCompiler scriptCompiler, ScriptLogger logger, ScriptConsole scriptConsole)
        {
            Logger = logger;
            ScriptCompiler = scriptCompiler;
            ScriptConsole = scriptConsole;
        }

        public async Task<TReturn> Execute<TReturn>(string dllPath)
        {
            var assembly = Assembly.LoadFrom(dllPath);
            var refs = assembly.GetReferencedAssemblies();
            //var tempProjectPath = ScriptProjectProvider.GetPathToProjectFile(Path.GetDirectoryName(dllPath));
            //var projectFile = new ProjectFile();

            //foreach (var @ref in refs)
            //{
            //    projectFile.AddPackageReference(new PackageReference("AutoMapper", "6.1.0", PackageOrigin.ReferenceDirective));
            //}
            //projectFile.Save(tempProjectPath);

            //var deps = ScriptCompiler.RuntimeDependencyResolver.GetDependencies();
            //var @ref = Assembly.LoadFrom(@"C:\Users\u403598\Desktop\temp\nuget_testing\AutoMapper.dll");
            //AppDomain.CurrentDomain.Load(@ref.GetName());

            var rootDir = Path.GetDirectoryName(dllPath);
            var temp = ScriptDependencyInfoProvider.ReadDependencyContextFromAssets(rootDir);
            var rootNuget = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), ".nuget", "packages");
            var runtimeDeps = ScriptCompiler.RuntimeDependencyResolver.GetDependenciesCustom(rootDir, new string[] { rootNuget, @"C:\Program Files\dotnet\sdk\NuGetFallbackFolder" });
            var runtimeDepsMap = ScriptCompiler.CreateScriptDependenciesMap(runtimeDeps);

            AppDomain.CurrentDomain.AssemblyResolve +=
                (sender, args) =>
                {
                    var assemblyName = new AssemblyName(args.Name);
                    var result = runtimeDepsMap.TryGetValue(assemblyName.Name, out RuntimeAssembly runtimeAssembly);
                    if (!result) throw new Exception($"Unable to locate assembly '{assemblyName.Name}: {assemblyName.Version}");
                    var loadedAssembly = Assembly.LoadFrom(runtimeAssembly.Path);
                    return loadedAssembly;
                };

            //foreach (var @ref in refs)
            //{
            //    AppDomain.CurrentDomain.Load(@ref);
            //}

            var type = assembly.GetType("Submission#0");
            var method = type.GetMethod("<Factory>", BindingFlags.Static | BindingFlags.Public);

            var submissionStates = new object[2];
            submissionStates[0] = new CommandLineScriptGlobals(ScriptConsole.Out, CSharpObjectFormatter.Instance);
            var resultTask = method.Invoke(null, new[] { submissionStates }) as Task<TReturn>;
            return await resultTask;
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
            try
            {
                var compilationContext = ScriptCompiler.CreateCompilationContext<TReturn, THost>(context);
                return Execute(compilationContext, host);
            }
            catch (CompilationErrorException e)
            {
                foreach (var diagnostic in e.Diagnostics)
                {
                    ScriptConsole.WritePrettyError(diagnostic.ToString());
                }

                throw;
            }
        }

        public virtual async Task<TReturn> Execute<TReturn, THost>(ScriptCompilationContext<TReturn> compilationContext, THost host)
        {
            var scriptResult = await compilationContext.Script.RunAsync(host, ex => true).ConfigureAwait(false);
            return ProcessScriptState(scriptResult);
        }

        protected TReturn ProcessScriptState<TReturn>(ScriptState<TReturn> scriptState)
        {
            if (scriptState.Exception != null)
            {
                // once Roslyn ships with this, we can format he exception using CSharpObjectFormatter
                // https://github.com/dotnet/roslyn/blob/4175350b87f928e136cbb14c2668b7cb3338d5a1/src/Scripting/Core/Hosting/CommonMemberFilter.cs#L18
                ScriptConsole.WritePrettyError(scriptState.Exception.ToString());
                throw new ScriptRuntimeException("Script execution resulted in an exception.", scriptState.Exception);
            }

            return scriptState.ReturnValue;
        }
    }
}
