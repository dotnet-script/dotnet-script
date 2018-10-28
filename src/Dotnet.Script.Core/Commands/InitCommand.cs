using Dotnet.Script.DependencyModel.Logging;

namespace Dotnet.Script.Core.Commands
{
    public class InitCommand
    {
        private readonly Logger _logger;
        private readonly LogFactory _logFactory;

        public InitCommand(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<InitCommand>();
            _logFactory = logFactory;
        }

        public void Execute(InitCommandOptions options)
        {
            var scaffolder = new Scaffolder(_logFactory);
            scaffolder.InitializerFolder(options.FileName, options.WorkingDirectory);
        }
    }
}