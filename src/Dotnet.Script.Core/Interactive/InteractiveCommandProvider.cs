using System.Linq;

namespace Dotnet.Script.Core
{
    public class InteractiveCommandProvider
    {
        private readonly IInteractiveCommand[] _commands = new IInteractiveCommand[] 
        {
            new ResetInteractiveCommand(),
            new ClsCommand(),
            new ExitCommand()
        };

        public bool TryProvideCommand(string code, out IInteractiveCommand command)
        {
            command = null;

            // not a REPL command
            if (!code.StartsWith("#")) return false;

            // compiler level directive
            if (code.StartsWith("#r ") || code.StartsWith("#load ")) return false;

            var commandName = code?.Split(' ')[0]?.Substring(1)?.Trim();
            if (commandName == null) return false;

            command = _commands.FirstOrDefault(x => x.Name == commandName);

            return command != null;
        }
    }
}
