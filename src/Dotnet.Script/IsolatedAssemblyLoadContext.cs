using System.Reflection;
using System.Runtime.Loader;

namespace Dotnet.Script
{
    sealed class IsolatedAssemblyLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName) => null;
    }
}
