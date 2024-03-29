using System.IO;
using System.Linq;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.Runtime;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Dotnet.Script.Core.Commands
{
    public class PublishCommand
    {
        private readonly ScriptConsole _scriptConsole;
        private readonly LogFactory _logFactory;

        public PublishCommand(ScriptConsole scriptConsole, LogFactory logFactory)
        {
            _scriptConsole = scriptConsole;
            _logFactory = logFactory;
        }

        public void Execute(PublishCommandOptions options)
        { 
            Execute<int>(options);
        }

        public void Execute<TReturn>(PublishCommandOptions options)
        {
            var absoluteFilePath = options.File.Path;

            // if a publish directory has been specified, then it is used directly, otherwise:
            // -- for EXE {current dir}/publish/{runtime ID}
            // -- for DLL {current dir}/publish
            var publishDirectory = options.OutputDirectory ??
                (options.PublishType == PublishType.Library ? Path.Combine(Path.GetDirectoryName(absoluteFilePath), "publish") : Path.Combine(Path.GetDirectoryName(absoluteFilePath), "publish", options.RuntimeIdentifier));

            var absolutePublishDirectory = publishDirectory.GetRootedPath();
            var compiler = new ScriptCompiler(_logFactory, !options.NoCache)
            {
#if NETCOREAPP
                AssemblyLoadContext = options.AssemblyLoadContext
#endif
            };
            var scriptEmitter = new ScriptEmitter(_scriptConsole, compiler);
            var publisher = new ScriptPublisher(_logFactory, scriptEmitter);
            var code = absoluteFilePath.ToSourceText();
            var context = new ScriptContext(code, absolutePublishDirectory, Enumerable.Empty<string>(), absoluteFilePath, options.OptimizationLevel, packageSources: options.PackageSources);

            if (options.PublishType == PublishType.Library)
            {
                publisher.CreateAssembly<TReturn, CommandLineScriptGlobals>(context, _logFactory, options.LibraryName);
            }
            else
            {
                publisher.CreateExecutable<TReturn, CommandLineScriptGlobals>(context, _logFactory, options.RuntimeIdentifier, options.LibraryName);
            }
        }
    }
}