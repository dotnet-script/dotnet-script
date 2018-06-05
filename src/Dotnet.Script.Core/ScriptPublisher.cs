using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dotnet.Script.Core
{
    public class ScriptPublisher
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly CommandRunner _commandRunner;
        private readonly ScriptCompiler _scriptCompiler;

        public ScriptPublisher(ScriptProjectProvider scriptProjectProvider, CommandRunner commandRunner, ScriptCompiler scriptCompiler)
        {
            _scriptProjectProvider = scriptProjectProvider ?? throw new ArgumentNullException(nameof(scriptProjectProvider));
            _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
            _scriptCompiler = scriptCompiler ?? throw new ArgumentNullException(nameof(scriptCompiler));
        }

        public void CreateExecutable(string fullFilePath, string publishDirectory)
        {
            const string outputAssemblyName = "scriptAssembly";
            var projectPath = _scriptProjectProvider.CreateProjectForScriptFile(fullFilePath, new PackageReference[] { new PackageReference("Microsoft.CodeAnalysis.Scripting", "2.8.2", PackageOrigin.ReferenceDirective) });
            var projectDirecory = Path.GetDirectoryName(projectPath);

            CreateScriptLibrary(fullFilePath, projectDirecory);

            var projectFile = new ProjectFile(File.ReadAllText(projectPath));
            projectFile.AddReference(outputAssemblyName);
            projectFile.Save(projectPath);

            CopyProgram(projectDirecory);

            var exitcode = _commandRunner.Execute("dotnet", $"publish \"{projectPath}\" -c Release -r win10-x64 -o {publishDirectory}");
        }

        private void CreateScriptLibrary(string fullFilePath, string projectDirectory)
        {
            const string assemblyName = "scriptAssembly";

            var sourceText = SourceText.From(File.ReadAllText(fullFilePath));
            var context = new ScriptContext(sourceText, projectDirectory, Enumerable.Empty<string>(), fullFilePath, OptimizationLevel.Debug);

            var compilationContext = _scriptCompiler.CreateCompilationContext<int, CommandLineScriptGlobals>(context);
            var tempOptions = compilationContext.Script.GetCompilation().Options
                .WithScriptClassName(assemblyName);
            var scriptCompilation = compilationContext.Script.GetCompilation()
                  .WithOptions(tempOptions)
                .WithAssemblyName(assemblyName);
            var tempCompilationDiagnostics = scriptCompilation.GetDiagnostics();
            scriptCompilation.Emit($"{projectDirectory}/{assemblyName}.dll");
        }

        private void CopyProgram(string projectDirecory)
        {
            var program = ReadTemplate("program.publish.template");
            File.WriteAllText($"{projectDirecory}/Program.cs", program);
        }

        private static string ReadTemplate(string name)
        {
            var allResources = typeof(ScriptPublisher).GetTypeInfo().Assembly.GetManifestResourceNames();
            var resourceStream = typeof(ScriptPublisher).GetTypeInfo().Assembly.GetManifestResourceStream($"Dotnet.Script.Core.Templates.{name}");
            using (var streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
