using Dotnet.Script.DependencyModel.Environment;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Process;
using Dotnet.Script.DependencyModel.ProjectSystem;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Reflection;

namespace Dotnet.Script.Core
{
    public class ScriptPublisher
    {
        private const string ScriptingVersion = "3.9.0";

        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ScriptEmitter _scriptEmitter;
        private readonly ScriptConsole _scriptConsole;
        private readonly ScriptEnvironment _scriptEnvironment;

        public ScriptPublisher(ScriptProjectProvider scriptProjectProvider, ScriptEmitter scriptEmitter, ScriptConsole scriptConsole)
        {
            _scriptProjectProvider = scriptProjectProvider ?? throw new ArgumentNullException(nameof(scriptProjectProvider));
            _scriptEmitter = scriptEmitter ?? throw new ArgumentNullException(nameof(scriptEmitter));
            _scriptConsole = scriptConsole ?? throw new ArgumentNullException(nameof(scriptConsole));
            _scriptEnvironment = ScriptEnvironment.Default;
        }

        public ScriptPublisher(LogFactory logFactory, ScriptEmitter scriptEmitter)
            : this
            (
                new ScriptProjectProvider(logFactory),
                scriptEmitter,
                ScriptConsole.Default
            )
        {
        }

        public void CreateAssembly<TReturn, THost>(ScriptContext context, LogFactory logFactory, string assemblyFileName = null)
        {
            Directory.CreateDirectory(context.WorkingDirectory);
            Directory.CreateDirectory(Path.Combine(context.WorkingDirectory, "obj"));

            assemblyFileName = assemblyFileName ?? Path.GetFileNameWithoutExtension(context.FilePath);
            var scriptAssemblyPath = CreateScriptAssembly<TReturn, THost>(context, context.WorkingDirectory, assemblyFileName);
            var tempProjectPath = ScriptProjectProvider.GetPathToProjectFile(Path.GetDirectoryName(context.FilePath), ScriptEnvironment.Default.TargetFramework);
            var tempProjectDirecory = Path.GetDirectoryName(tempProjectPath);

            var sourceProjectAssetsPath = Path.Combine(tempProjectDirecory, "obj", "project.assets.json");
            var destinationProjectAssetsPath = Path.Combine(context.WorkingDirectory, "obj", "project.assets.json");
            File.Copy(sourceProjectAssetsPath, destinationProjectAssetsPath, overwrite: true);

            var sourceNugetPropsPath = Path.Combine(tempProjectDirecory, "obj", "script.csproj.nuget.g.props");
            var destinationNugetPropsPath = Path.Combine(context.WorkingDirectory, "obj", "script.csproj.nuget.g.props");
            File.Copy(sourceNugetPropsPath, destinationNugetPropsPath, overwrite: true);

            // only display published if we aren't auto publishing to temp folder
            if (!scriptAssemblyPath.StartsWith(FileUtils.GetTempPath()))
            {
                _scriptConsole.WriteSuccess($"Published {context.FilePath} to { scriptAssemblyPath}");
            }
        }

        public void CreateExecutable<TReturn, THost>(ScriptContext context, LogFactory logFactory, string runtimeIdentifier, string executableFileName = null)
        {
            if (runtimeIdentifier == null)
            {
                throw new ArgumentNullException(nameof(runtimeIdentifier));
            }

            executableFileName = executableFileName ?? Path.GetFileNameWithoutExtension(context.FilePath);
            const string AssemblyName = "scriptAssembly";

            var tempProjectPath = ScriptProjectProvider.GetPathToProjectFile(Path.GetDirectoryName(context.FilePath), _scriptEnvironment.TargetFramework);
            var renamedProjectPath = ScriptProjectProvider.GetPathToProjectFile(Path.GetDirectoryName(context.FilePath), _scriptEnvironment.TargetFramework, executableFileName);
            var tempProjectDirectory = Path.GetDirectoryName(tempProjectPath);

            var scriptAssemblyPath = CreateScriptAssembly<TReturn, THost>(context, tempProjectDirectory, AssemblyName);

            var projectFile = new ProjectFile(File.ReadAllText(tempProjectPath));
            projectFile.PackageReferences.Add(new PackageReference("Microsoft.CodeAnalysis.Scripting", ScriptingVersion));
            projectFile.AssemblyReferences.Add(new AssemblyReference(scriptAssemblyPath));
            projectFile.Save(renamedProjectPath);

            CopyProgramTemplate(tempProjectDirectory);

            var commandRunner = new CommandRunner(logFactory);
            // todo: may want to add ability to return dotnet.exe errors
            var publishSingleFileArgument = ScriptEnvironment.Default.NetCoreVersion.Major >= 3 ? "/p:PublishSingleFile=true" : string.Empty;
            var includeNativeLibrariesForSelfExtract = ScriptEnvironment.Default.NetCoreVersion.Major >= 5 ? "/p:IncludeNativeLibrariesForSelfExtract=true" : string.Empty;

            var exitcode = commandRunner.Execute("dotnet", $"publish \"{renamedProjectPath}\" -c Release -r {runtimeIdentifier} -o \"{context.WorkingDirectory}\" {publishSingleFileArgument} {includeNativeLibrariesForSelfExtract} /p:DebugType=Embedded");

            if (exitcode != 0)
            {
                throw new Exception($"dotnet publish failed with result '{exitcode}'");
            }

            _scriptConsole.WriteSuccess($"Published {context.FilePath} (executable) to {context.WorkingDirectory}");
        }

        private string CreateScriptAssembly<TReturn, THost>(ScriptContext context, string outputDirectory, string assemblyFileName)
        {
            var emitResult = _scriptEmitter.Emit<TReturn, THost>(context, assemblyFileName);
            var assemblyPath = Path.Combine(outputDirectory, $"{assemblyFileName}.dll");
            using (var peFileStream = new FileStream(assemblyPath, FileMode.Create))
            using (emitResult.PeStream)
            {
                emitResult.PeStream.WriteTo(peFileStream);
            }

            foreach (var reference in emitResult.DirectiveReferences)
            {
                if (reference.Display.EndsWith(".NuGet.dll"))
                {
                    continue;
                }

                var referenceFileInfo = new FileInfo(reference.Display);
                var fullPathToReference = Path.GetFullPath(referenceFileInfo.FullName);
                var fullPathToNewAssembly = Path.GetFullPath(Path.Combine(outputDirectory, referenceFileInfo.Name));

                if (!Equals(fullPathToReference, fullPathToNewAssembly))
                {
                    File.Copy(fullPathToReference, fullPathToNewAssembly, true);
                }
            }

            /*  The following is needed to make native assets work.
                During a regular "dotnet publish" we find these assets in a "runtimes" folder.
                We must copy these binaries up to the same folder as the script library so
                that they can be found during execution.
            */
            foreach (var runtimeDependency in emitResult.RuntimeDependencies)
            {
                if (!runtimeDependency.Name.Contains("microsoft.netcore", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var nativeAsset in runtimeDependency.NativeAssets)
                    {
                        File.Copy(nativeAsset, Path.Combine(outputDirectory, Path.GetFileName(nativeAsset)), true);
                    }
                    foreach (var runtimeAssembly in runtimeDependency.Assemblies)
                    {
                        File.Copy(runtimeAssembly.Path, Path.Combine(outputDirectory, Path.GetFileName(runtimeAssembly.Path)), true);
                        var pathToRuntimeAssemblyFolder = Path.GetDirectoryName(runtimeAssembly.Path);
                        var pdbFileName = $"{Path.GetFileNameWithoutExtension(runtimeAssembly.Path)}.pdb";
                        var pathToPdb = Path.Combine(pathToRuntimeAssemblyFolder, pdbFileName);
                        if (File.Exists(pathToPdb))
                        {
                            File.Copy(pathToPdb, Path.Combine(outputDirectory, Path.GetFileName(pathToPdb)), true);
                        }
                    }
                }
            }

            return assemblyPath;
        }

        private static void CopyProgramTemplate(string tempProjectDirecory)
        {
            const string resourceName = "Dotnet.Script.Core.Templates.program.publish.template";

            var resourceStream = typeof(ScriptPublisher).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                throw new FileNotFoundException($"Unable to locate resource '{resourceName}'");
            }

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
