using System.Collections.Generic;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public class CompilationDependencyResolver
    {
        private readonly ScriptProjectProvider _scriptProjectProvider;
        private readonly ICompilationReferenceReader _compilationReferenceReader;

        public CompilationDependencyResolver(LogFactory logFactory) : this(new ScriptProjectProvider(logFactory), new CompilationReferencesReader(logFactory), logFactory)
        {
        }

        public CompilationDependencyResolver(ScriptProjectProvider scriptProjectProvider, ICompilationReferenceReader compilationReferenceReader, LogFactory logFactory)
        {
            _scriptProjectProvider = scriptProjectProvider;
            _compilationReferenceReader = compilationReferenceReader;
        }

        public IEnumerable<CompilationReference> GetDependencies(string targetDirectory, IEnumerable<string> scriptFiles, bool enableScriptNugetReferences, string defaultTargetFramework = "net46")
        {
            var projectFileInfo = _scriptProjectProvider.CreateProject(targetDirectory, scriptFiles, defaultTargetFramework, enableScriptNugetReferences);
            return _compilationReferenceReader.Read(projectFileInfo);
        }
    }
}