namespace Dotnet.Script.Core
{
    public class ClsCommand : IInteractiveCommand
    {
        public string Name => "cls";

        public void Execute(CommandContext commandContext)
        {
            commandContext.Console.Clear();
        }
    }
}
