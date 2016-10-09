using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script
{
    public static class ScriptAssemblyExtensions
    {
        public static Assembly GetScriptAssembly(this Script<object> script, InteractiveAssemblyLoader loader)
        {
            // force creation of lazy evaluator
            var scriptDelegate = script.CreateDelegate();

            var loadedAssembliesBySimpleNameProperty = loader.GetType().GetField("_loadedAssembliesBySimpleName", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadedAssembliesBySimpleName = loadedAssembliesBySimpleNameProperty?.GetValue(loader) as dynamic;

            if (loadedAssembliesBySimpleName != null)
            {
                foreach (var loadedAssembly in loadedAssembliesBySimpleName)
                {
                    var loadedAssemblyValue = loadedAssembly.GetType().GetProperty("Value");
                    var infoList = loadedAssemblyValue?.GetValue(loadedAssembly) as dynamic;

                    if (infoList != null)
                    {
                        foreach (var infoObject in infoList)
                        {
                            var assemblyProperty = infoObject.GetType().GetField("Assembly");
                            var assembly = assemblyProperty.GetValue(infoObject) as Assembly;
                            if (assembly.FullName.StartsWith("\u211B*"))
                            {
                                return assembly;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static Assembly GetScriptAssembly(this ScriptState<object> scriptState)
        {
            var executionStateProperty = scriptState.GetType().GetProperty("ExecutionState", BindingFlags.NonPublic | BindingFlags.Instance);
            var executionState = executionStateProperty?.GetValue(scriptState);
            var submissionStatesField = executionState?.GetType().GetField("_submissionStates", BindingFlags.NonPublic | BindingFlags.Instance);
            var submissions = submissionStatesField?.GetValue(executionState) as object[];

            if (submissions == null || submissions.Length < 2)
            {
                return null;
            }

            // boohooo
            var scriptAssembly = submissions[1].GetType().GetTypeInfo().Assembly;
            return scriptAssembly;
        }
    }
}