using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Script.DependencyModel.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.LanguageServices
{
    /// <summary>
    /// A Roslyn workspace that mirrors the REPL session: every executed submission becomes a submission
    /// project referencing the previous one, so declarations stay visible. The text being typed lives in a
    /// separate scratch project that is rebuilt whenever the session moves on.
    /// </summary>
    public sealed class ReplWorkspace : IDisposable
    {
        private const string ScratchName = "Submission#scratch";

        private readonly object _gate = new object();
        private readonly List<string> _submissions = new List<string>();
        private readonly Logger _logger;

        private AdhocWorkspace _workspace;
        private ScriptOptions _scriptOptions;
        private CachingScriptMetadataResolver _metadataResolver;
        private ProjectId _head;
        private ProjectId _scratchProject;
        private Document _scratchDocument;
        private bool _disposed;

        public ReplWorkspace(Logger logger = null)
        {
            _logger = logger ?? ((level, message, exception) => { });
        }

        public bool IsInitialized
        {
            get
            {
                lock (_gate)
                {
                    return !_disposed && _scriptOptions != null;
                }
            }
        }

        public void ScriptOptionsChanged(ScriptOptions scriptOptions)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _scriptOptions = scriptOptions;
                _metadataResolver = new CachingScriptMetadataResolver(scriptOptions.MetadataResolver);

                // Committed submissions carry their own reference set, so a reference added by #r only
                // reaches them if they are rebuilt.
                var committed = _submissions.ToArray();
                DropScratch();
                _workspace?.ClearSolution();
                _head = null;
                _submissions.Clear();

                foreach (var submission in committed)
                {
                    AddSubmission(submission);
                }
            }
        }

        public void SubmissionExecuted(string code)
        {
            lock (_gate)
            {
                if (_disposed || _scriptOptions == null || string.IsNullOrWhiteSpace(code))
                {
                    return;
                }

                // A submission may only be referenced once, so the scratch project has to let go first.
                DropScratch();
                AddSubmission(code);
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                _scratchProject = null;
                _scratchDocument = null;
                _head = null;
                _submissions.Clear();
                _scriptOptions = null;
                _metadataResolver = null;
                _workspace?.ClearSolution();
            }
        }

        /// <summary>
        /// Returns a document holding <paramref name="text"/>, or <c>null</c> when the session has not
        /// produced any script options yet.
        /// </summary>
        public Document GetDocument(string text)
        {
            lock (_gate)
            {
                if (_disposed || _scriptOptions == null)
                {
                    return null;
                }

                if (_scratchDocument == null)
                {
                    var info = ReplProjectFactory.Create(ScratchName, string.Empty, _scriptOptions, _metadataResolver, _head);
                    var project = EnsureWorkspace().AddProject(info);
                    _scratchProject = project.Id;
                    _scratchDocument = project.Documents.Single();
                }

                return _scratchDocument.WithText(SourceText.From(text ?? string.Empty));
            }
        }

        /// <summary>
        /// Forces the compilation and a first classification so that neither the cost of reading metadata
        /// nor the one-off cost of spinning up the classifier lands on a key stroke.
        /// </summary>
        public async Task WarmUpAsync(CancellationToken cancellationToken)
        {
            const string sample = "var warmUp = 1;";

            var document = GetDocument(sample);
            if (document == null)
            {
                return;
            }

            await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            await Classifier.GetClassifiedSpansAsync(document, new TextSpan(0, sample.Length), cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                _workspace?.Dispose();
                _workspace = null;
                _scratchProject = null;
                _scratchDocument = null;
                _head = null;
                _scriptOptions = null;
                _metadataResolver = null;
                _submissions.Clear();
            }
        }

        private void AddSubmission(string code)
        {
            var info = ReplProjectFactory.Create($"Submission#{_submissions.Count}", code, _scriptOptions, _metadataResolver, _head);
            EnsureWorkspace().AddProject(info);
            _head = info.Id;
            _submissions.Add(code);
        }

        private AdhocWorkspace EnsureWorkspace() =>
            _workspace ?? (_workspace = new AdhocWorkspace(ReplHostServices.GetOrCreate(_logger)));

        private void DropScratch()
        {
            if (_scratchProject != null && _workspace != null &&
                !_workspace.TryApplyChanges(_workspace.CurrentSolution.RemoveProject(_scratchProject)))
            {
                // Leaving it behind gives the next submission a second reference to the same project,
                // which Roslyn rejects outright.
                _logger.Warning($"Could not remove the scratch project '{_scratchProject}' from the REPL workspace.");
            }

            _scratchProject = null;
            _scratchDocument = null;
        }
    }
}
