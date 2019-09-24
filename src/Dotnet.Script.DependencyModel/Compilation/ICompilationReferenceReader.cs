using System.Collections.Generic;
using Dotnet.Script.DependencyModel.ProjectSystem;

namespace Dotnet.Script.DependencyModel.Compilation
{
    public interface ICompilationReferenceReader
    {
        IEnumerable<CompilationReference> Read(ProjectFileInfo projectFile);
    }
}