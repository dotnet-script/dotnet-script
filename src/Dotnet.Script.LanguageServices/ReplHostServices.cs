using System;
using System.Collections.Generic;
using System.Reflection;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Dotnet.Script.LanguageServices
{
    internal static class ReplHostServices
    {
        private static readonly object Gate = new object();
        private static MefHostServices _instance;

        public static MefHostServices GetOrCreate(Logger logger)
        {
            lock (Gate)
            {
                return _instance ?? (_instance = Compose(logger));
            }
        }

        private static MefHostServices Compose(Logger logger)
        {
            var assemblies = new List<Assembly>
            {
                typeof(Microsoft.CodeAnalysis.Workspace).Assembly,
                typeof(CSharpFormattingOptions).Assembly,
                typeof(CompletionService).Assembly
            };

            // CSharp.Features exposes no public type to hang a typeof() on, but its completion,
            // classification and quick info providers are exactly what makes this worthwhile.
            var csharpFeatures = TryLoad("Microsoft.CodeAnalysis.CSharp.Features", logger);
            if (csharpFeatures != null)
            {
                assemblies.Add(csharpFeatures);
            }

            return MefHostServices.Create(assemblies);
        }

        private static Assembly TryLoad(string name, Logger logger)
        {
            try
            {
                return Assembly.Load(new AssemblyName(name));
            }
            catch (Exception exception)
            {
                logger.Warning($"Could not load '{name}'. REPL completion and colorization will be limited. {exception.Message}");
                return null;
            }
        }
    }
}
