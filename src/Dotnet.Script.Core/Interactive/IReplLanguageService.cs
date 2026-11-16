using System;
using Dotnet.Script.Core.Interactive.LineEditing;
using Microsoft.CodeAnalysis.Scripting;

namespace Dotnet.Script.Core
{
    /// <summary>
    /// Editor services for the REPL. Implementations are fed the state of the running session
    /// so that completions and classifications can see everything the script engine sees.
    /// </summary>
    public interface IReplLanguageService : ICompletionProvider, ISyntaxClassifier, IQuickInfoProvider, IDisposable
    {
        /// <summary>Called whenever references, imports or resolvers change.</summary>
        void ScriptOptionsChanged(ScriptOptions scriptOptions);

        /// <summary>Called after a submission ran successfully, so its declarations become visible.</summary>
        void SubmissionExecuted(string code);

        /// <summary>Called when the session is reset and all previous submissions are discarded.</summary>
        void Reset();
    }
}
