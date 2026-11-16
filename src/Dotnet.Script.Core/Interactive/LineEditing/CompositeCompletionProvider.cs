namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Routes completion between a language service and the built in provider. Directive lines always
    /// go to the built in provider, which also serves as the fallback when the language service is
    /// unavailable.
    /// </summary>
    public sealed class CompositeCompletionProvider : ICompletionProvider
    {
        private readonly ICompletionProvider _primary;
        private readonly ReplCompletionProvider _fallback;

        public CompositeCompletionProvider(ICompletionProvider primary, ReplCompletionProvider fallback)
        {
            _primary = primary;
            _fallback = fallback;
        }

        public CompletionResult GetCompletions(string text, int caret)
        {
            if (_primary == null || ReplCompletionProvider.IsDirectiveLine(text, caret))
            {
                return _fallback.GetCompletions(text, caret);
            }

            // A null result means the language service could not answer; an empty one means it answered
            // that there is nothing here, which the fallback has no business second guessing.
            return _primary.GetCompletions(text, caret) ?? _fallback.GetCompletions(text, caret);
        }
    }
}
