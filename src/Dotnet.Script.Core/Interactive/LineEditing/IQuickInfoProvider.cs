namespace Dotnet.Script.Core.Interactive.LineEditing
{
    public interface IQuickInfoProvider
    {
        /// <summary>
        /// Describes the symbol at the caret in a single line, or <c>null</c> when there is nothing to show.
        /// </summary>
        string GetQuickInfo(string text, int caret);
    }
}
