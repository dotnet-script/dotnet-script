using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script
{
    public static class ScriptAssemblyExtensions
    {
        public static Assembly GetScriptAssembly<TReturn>(this Script<TReturn> script, InteractiveAssemblyLoader loader)
        {
            // force creation of lazy evaluator
            // this does not execute the script code but it will emit the assembly
            // and load it via InteractiveAssemblyLoader
            var scriptDelegate = script.CreateDelegate();

            // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Scripting/Core/Hosting/AssemblyLoader/InteractiveAssemblyLoader.cs#L52
            var loadedAssembliesBySimpleNameProperty = loader.GetType().GetField("_loadedAssembliesBySimpleName", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadedAssembliesBySimpleName = loadedAssembliesBySimpleNameProperty?.GetValue(loader) as IDictionary;
            if (loadedAssembliesBySimpleName == null)
                return null;

            var assemblies =
                from entry in loadedAssembliesBySimpleName.GetEntries()
                select entry.Value as IList into infoList
                where infoList != null
                from object infoObject in infoList
                // https://github.com/dotnet/roslyn/blob/version-2.0.0-beta4/src/Scripting/Core/Hosting/AssemblyLoader/InteractiveAssemblyLoader.cs#L77
                select (Assembly) infoObject.GetType().GetField("Assembly")?.GetValue(infoObject) into asm
                where asm != null
                select asm;

            return assemblies.FirstOrDefault(asm => asm.FullName.StartsWith("\u211B*", StringComparison.Ordinal));
        }
    }
}
