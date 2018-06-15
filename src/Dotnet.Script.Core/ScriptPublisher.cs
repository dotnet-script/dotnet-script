using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Reflection;

namespace Dotnet.Script.Core
{
    public class ScriptPublisher
    {
        const string AssemblyName = "scriptAssembly";
        const string ScriptingVersion = "2.8.2";

        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptEmitter _scriptEmitter;
        private readonly ScriptConsole _scriptConsole;
        private readonly ScriptEnvironment _scriptEnvironment;
        private readonly ScriptLogger _logger;

        public ScriptPublisher(ScriptProjectProvider scriptProjectProvider, ScriptEmitter scriptEmitter, ScriptConsole scriptConsole,
            ScriptLogger scriptLogger)
        {
            _scriptProjectProvider = scriptProjectProvider ?? throw new ArgumentNullException(nameof(scriptProjectProvider));
            _scriptEmitter = scriptEmitter ?? throw new ArgumentNullException(nameof(scriptEmitter));
            _scriptConsole = scriptConsole ?? throw new ArgumentNullException(nameof(scriptConsole));
            _scriptEnvironment = ScriptEnvironment.Default;
            _logger = scriptLogger;
        }

        public ScriptPublisher(LogFactory logFactory, ScriptEmitter scriptEmitter, ScriptLogger scriptLogger)
            : this
            (
                new ScriptProjectProvider(logFactory),
                scriptEmitter,
                ScriptConsole.Default,
                scriptLogger
            )
        {
        }

        public void CreateAssembly(ScriptContext context, LogFactory logFactory)
        {
            Directory.CreateDirectory(context.WorkingDirectory);
            Directory.CreateDirectory(Path.Combine(context.WorkingDirectory, "obj"));

            _logger.Verbose("Publishing dll");
            CreateScriptAssembly(context, context.WorkingDirectory);

            var tempProjectPath = ScriptProjectProvider.GetPathToProjectFile(Path.GetDirectoryName(context.FilePath));
            var tempProjectDirecory = Path.GetDirectoryName(tempProjectPath);

            var sourceProjectAssetsPath = Path.Combine(tempProjectDirecory, "obj", "project.assets.json");
            var destinationProjectAssetsPath = Path.Combine(context.WorkingDirectory, "obj", "project.assets.json");
            File.Copy(sourceProjectAssetsPath, destinationProjectAssetsPath, overwrite: true);

            var sourceNugetPropsPath = Path.Combine(tempProjectDirecory, "obj", "script.csproj.nuget.g.props");
            var destinationNugetPropsPath = Path.Combine(context.WorkingDirectory, "obj", "script.csproj.nuget.g.props");
            File.Copy(sourceNugetPropsPath, destinationNugetPropsPath, overwrite: true);
        }

        public void CreateExecutable(ScriptContext context, LogFactory logFactory)
        {
            var tempProjectPath = ScriptProjectProvider.GetPathToProjectFile(Path.GetDirectoryName(context.FilePath));
            var tempProjectDirecory = Path.GetDirectoryName(tempProjectPath);

            var scriptAssemblyPath = CreateScriptAssembly(context, tempProjectDirecory);

            var projectFile = new ProjectFile(File.ReadAllText(tempProjectPath));
            projectFile.AddPackageReference(new PackageReference("Microsoft.CodeAnalysis.Scripting", ScriptingVersion, PackageOrigin.ReferenceDirective));
            projectFile.AddAssemblyReference(scriptAssemblyPath);
            projectFile.Save(tempProjectPath);

            CopyProgramTemplate(tempProjectDirecory);

            var runtimeIdentifier = _scriptEnvironment.RuntimeIdentifier;

            var commandRunner = new CommandRunner(logFactory);
            // todo: may want to add ability to return dotnet.exe errors
            _logger.Verbose("Publishing exe");
            var exitcode = commandRunner.Execute("dotnet", $"publish \"{tempProjectPath}\" -c Release -r {runtimeIdentifier} -o {context.WorkingDirectory}");
            if (exitcode != 0) throw new Exception($"dotnet publish failed with result '{exitcode}'");
        }

        private string CreateScriptAssembly(ScriptContext context, string outputDirectory)
        {
            try
            {
                var emitResult = _scriptEmitter.Emit<int>(context);
                if (!emitResult.Success)
                {
                    throw new CompilationErrorException("One or more errors occurred when emitting the assembly", emitResult.Diagnostics);
                }

                var assemblyPath = Path.Combine(outputDirectory, $"{AssemblyName}.dll");
                using (var peFileStream = new FileStream(assemblyPath, FileMode.Create))
                using (emitResult.PeStream)
                {
                    emitResult.PeStream.WriteTo(peFileStream);
                }

                foreach (var reference in emitResult.DirectiveReferences)
                {
                    if (reference.Display.EndsWith(".NuGet.dll")) continue;
                    var refInfo = new FileInfo(reference.Display);
                    var newAssemblyPath = Path.Combine(outputDirectory, refInfo.Name);
                    File.Copy(refInfo.FullName, newAssemblyPath, true);
                }

                return assemblyPath;
            }
            catch (CompilationErrorException ex)
            {
                _scriptConsole.WritePrettyError(ex.Message);
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
            var programcsPath = Path.Combine(tempProjectDirecory, "Program.cs");
            File.WriteAllText(programcsPath, program);
        }
    }
}
