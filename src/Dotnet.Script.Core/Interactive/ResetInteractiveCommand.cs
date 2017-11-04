namespace Dotnet.Script.Core
{
    public class ResetInteractiveCommand : IInteractiveCommand
    {
        public string Name => "reset";

        public void Execute(CommandContext commandContext)
        {
            commandContext.Runner.Reset();
        }
    }
}
