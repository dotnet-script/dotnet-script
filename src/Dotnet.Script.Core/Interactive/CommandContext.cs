namespace Dotnet.Script.Core
{
    public class CommandContext
    {
        public CommandContext(ScriptConsole console, InteractiveRunner runner)
        {
            Console = console;
            Runner = runner;
        }

        public ScriptConsole Console { get; }
        public InteractiveRunner Runner { get; }
    }
}
