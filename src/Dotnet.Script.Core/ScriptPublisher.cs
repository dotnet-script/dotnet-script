using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace Dotnet.Script.Core
{
    public class ScriptPublisher
    {
        const string AssemblyName = "scriptAssembly";

        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptCompiler _scriptCompiler;
        private readonly ScriptConsole _scriptConsole;
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptPublisher(ScriptProjectProvider scriptProjectProvider, ScriptCompiler scriptCompiler,
            ScriptConsole scriptConsole)
        {
            _scriptProjectProvider = scriptProjectProvider ?? throw new ArgumentNullException(nameof(scriptProjectProvider));
            _scriptCompiler = scriptCompiler ?? throw new ArgumentNullException(nameof(scriptCompiler));
            _scriptConsole = scriptConsole ?? throw new ArgumentNullException(nameof(scriptConsole));
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        public ScriptPublisher(LogFactory logFactory, ScriptCompiler scriptCompiler)
            : this
            (
                new ScriptProjectProvider(logFactory),
                scriptCompiler,
                ScriptConsole.Default
            )
        {
        }

        public void CreateExecutable(ScriptContext context, LogFactory logFactory)
        {
            var tempProjectPath = _scriptProjectProvider.CreateProjectForScriptFile(context.FilePath);
            var tempProjectDirecory = Path.GetDirectoryName(tempProjectPath);

            var scriptAssemblyPath = CreateScriptAssembly(context, tempProjectDirecory);

            var projectFile = new ProjectFile(File.ReadAllText(tempProjectPath));
            // todo: grab version in a better way?
            projectFile.AddPackageReference(new PackageReference("Microsoft.CodeAnalysis.Scripting", "2.8.2", PackageOrigin.ReferenceDirective));
            projectFile.AddAssemblyReference(scriptAssemblyPath);
            projectFile.Save(tempProjectPath);

            CopyProgramTemplate(tempProjectDirecory);

            var runtimeIdentifier = _scriptEnvironment.RuntimeIdentifier;

            var commandRunner = new CommandRunner(logFactory);
            // todo: may want to add ability to return dotnet.exe errors
            var exitcode = commandRunner.Execute("dotnet", $"publish \"{tempProjectPath}\" -c Release -r {runtimeIdentifier} -o {context.WorkingDirectory}");
            if (exitcode != 0) throw new Exception($"dotnet publish failed with result '{exitcode}'");
        }

        private string CreateScriptAssembly(ScriptContext context, string tempProjectDirecory)
        {
            try
            {
                var compilationContext = _scriptCompiler.CreateCompilationContext<int, CommandLineScriptGlobals>(context);
                var scriptOptions = compilationContext.Script.GetCompilation().Options
                    .WithScriptClassName(AssemblyName);
                var scriptCompilation = compilationContext.Script.GetCompilation()
                      .WithOptions(scriptOptions)
                    .WithAssemblyName(AssemblyName);

                foreach (var reference in scriptCompilation.DirectiveReferences)
                {
                    var refInfo = new FileInfo(reference.Display);
                    File.Copy(refInfo.FullName, $"{tempProjectDirecory}/{refInfo.Name}", true);
                }

                var assemblyPath = $"{tempProjectDirecory}/{AssemblyName}.dll";
                var emitResult = scriptCompilation.Emit(assemblyPath);
                if (!emitResult.Success) throw new Exception("Failed while emitting the generated script assembly");

                return assemblyPath;
            }
            catch (CompilationErrorException ex)
            {
                foreach (var diagnostic in ex.Diagnostics)
                {
                    _scriptConsole.WritePrettyError(diagnostic.ToString());
                }
                throw;
            }
        }

        private void CopyProgramTemplate(string tempProjectDirecory)
        {
            const string resourceName = "Dotnet.Script.Core.Templates.program.publish.template";

            var resourceStream = typeof(ScriptPublisher).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null) throw new FileNotFoundException($"Unable to locate resource '{resourceName}'");

            string program;
            using (var streamReader = new StreamReader(resourceStream))
            {
                program = streamReader.ReadToEnd();
            }
            File.WriteAllText($"{tempProjectDirecory}/Program.cs", program);
        }
    }
}
