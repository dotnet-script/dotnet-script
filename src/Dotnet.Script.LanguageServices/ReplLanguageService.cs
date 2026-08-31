using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.Core.Interactive.LineEditing;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.CodeAnalysis.Scripting;

namespace Dotnet.Script.LanguageServices
{
    /// <summary>
    /// Roslyn backed editor services for the REPL. Every entry point degrades to nothing rather than
    /// disturbing the session, so a broken workspace only costs completion quality.
    /// </summary>
    public sealed class ReplLanguageService : IReplLanguageService
    {
        private readonly Logger _logger;
        private readonly ReplWorkspace _workspace;
        private readonly RoslynCompletionProvider _completion;
        private readonly RoslynClassifier _classifier;
        private readonly RoslynQuickInfoProvider _quickInfo;
        private readonly SyntaxHighlighter _lexer = new SyntaxHighlighter();

        private CancellationTokenSource _warmUp;
        private bool _disabled;

        public ReplLanguageService(LogFactory logFactory)
        {
            _logger = logFactory.CreateLogger<ReplLanguageService>();
            _workspace = new ReplWorkspace(_logger);
            _completion = new RoslynCompletionProvider(_workspace);
            _classifier = new RoslynClassifier(_workspace, _lexer);
            _quickInfo = new RoslynQuickInfoProvider(_workspace);
        }

        public CompletionResult GetCompletions(string text, int caret) =>
            Guarded(() => _completion.GetCompletions(text, caret), () => null);

        public IReadOnlyList<ClassifiedSpan> Classify(string text) =>
            Guarded(() => _classifier.Classify(text), () => _lexer.Classify(text));

        public string GetQuickInfo(string text, int caret) =>
            Guarded(() => _quickInfo.GetQuickInfo(text, caret), () => null);

        public void ScriptOptionsChanged(ScriptOptions scriptOptions)
        {
            // A new reference set is quite likely to be what fixes whatever failed before.
            _disabled = false;

            if (Guarded(() => { _workspace.ScriptOptionsChanged(scriptOptions); return true; }, () => false))
            {
                StartWarmUp();
            }
        }

        public void SubmissionExecuted(string code)
        {
            Guarded(() => { _workspace.SubmissionExecuted(code); return true; }, () => false);
        }

        public void Reset()
        {
            _disabled = false;
            CancelWarmUp();
            Guarded(() => { _workspace.Reset(); return true; }, () => false);
        }

        public void Dispose()
        {
            CancelWarmUp();
            _workspace.Dispose();
        }

        /// <summary>
        /// Reading metadata for the whole reference set takes seconds; doing it up front keeps the first
        /// key stroke responsive.
        /// </summary>
        private void StartWarmUp()
        {
            CancelWarmUp();
            _warmUp = new CancellationTokenSource();
            var token = _warmUp.Token;

            Task.Run(async () =>
            {
                try
                {
                    await _workspace.WarmUpAsync(token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.Debug($"Failed to warm up the language services workspace. {exception}");
                }
            }, token);
        }

        private void CancelWarmUp()
        {
            try
            {
                _warmUp?.Cancel();
                _warmUp?.Dispose();
            }
            catch (Exception)
            {
                // Nothing useful to do if the warm up refuses to stop.
            }

            _warmUp = null;
        }

        private T Guarded<T>(Func<T> operation, Func<T> fallback)
        {
            if (_disabled)
            {
                return fallback();
            }

            try
            {
                return operation();
            }
            catch (Exception exception)
            {
                // One failure usually means the workspace cannot be built at all here; stop paying for it.
                _disabled = true;
                _logger.Debug($"Language services disabled after an unexpected failure. {exception}");
                return fallback();
            }
        }
    }
}
