namespace Dotnet.Script.Core
{
    public interface IInteractiveCommand
    {
        string Name { get; }
        void Execute(CommandContext commandContext);
    }
}
