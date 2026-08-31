using System;
using System.Collections.Generic;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Interactive line editor for the REPL: multi-line aware editing, history,
    /// syntax highlighting, bracket matching and tab completion.
    /// </summary>
    public sealed class LineEditor
    {
        private static readonly ClassifiedSpan[] NoSpans = new ClassifiedSpan[0];

        private readonly IConsoleDevice _device;
        private readonly LineRenderer _renderer;
        private readonly LineBuffer _buffer = new LineBuffer();

        private string _cachedText;
        private IReadOnlyList<ClassifiedSpan> _cachedSpans;
        private bool[] _cachedMask;

        private CompletionState _completion;
        private bool _escapeArmed;
        private bool _quickInfoArmed;
        private string _quickInfoText;
        private string _quickInfoKey;
        private bool _searchActive;
        private string _searchTerm;
        private int _searchIndex;
        private string _searchSavedText;

        public LineEditor(IConsoleDevice device = null, ReplColorScheme colors = null, ReplHistory history = null)
        {
            _device = device ?? new SystemConsoleDevice();
            Colors = colors ?? ReplColorScheme.Default;
            History = history ?? new ReplHistory();
            _renderer = new LineRenderer(_device, Colors);
        }

        public ReplHistory History { get; }

        public ReplColorScheme Colors { get; }

        public ICompletionProvider CompletionProvider { get; set; }

        public ISyntaxClassifier Classifier { get; set; } = new SyntaxHighlighter();

        public IQuickInfoProvider QuickInfoProvider { get; set; }

        public bool EnableSyntaxHighlighting { get; set; } = true;

        public int IndentSize { get; set; } = 4;

        /// <summary>
        /// Reads a submission. Returns the submitted text, an empty string when the user
        /// cancelled with Ctrl+C, or <c>null</c> on end of input (Ctrl+D on an empty line).
        /// </summary>
        public string Read(string prompt, string continuationPrompt, Func<string, bool> isCompleteSubmission)
        {
            prompt = prompt ?? string.Empty;
            continuationPrompt = continuationPrompt ?? string.Empty;

            _buffer.Clear();
            _completion = null;
            _escapeArmed = false;
            _quickInfoArmed = false;
            _quickInfoKey = null;
            _searchActive = false;
            History.ResetNavigation();

            var previousTreatControlCAsInput = _device.TreatControlCAsInput;
            _device.TreatControlCAsInput = true;

            try
            {
                _renderer.Start();
                Render(prompt, continuationPrompt);

                while (true)
                {
                    var key = _device.ReadKey();

                    var completionWasActive = _completion != null;
                    if (key.Key != ConsoleKey.Tab)
                    {
                        _completion = null;
                    }

                    var escapeWasArmed = _escapeArmed;
                    if (key.Key != ConsoleKey.Escape)
                    {
                        _escapeArmed = false;
                    }

                    if (_searchActive)
                    {
                        HandleSearchKey(key);
                        RenderUnlessMoreInputIsPending(prompt, continuationPrompt);
                        continue;
                    }

                    var control = (key.Modifiers & ConsoleModifiers.Control) != 0;
                    var alt = (key.Modifiers & ConsoleModifiers.Alt) != 0;

                    // AltGr reports as Control|Alt on Windows, so it must insert rather than
                    // fall into the Ctrl bindings below - on many layouts it is the only way
                    // to type characters such as "ą", "@", "{" or "\\".
                    if (control && alt && !char.IsControl(key.KeyChar))
                    {
                        Modify(() => InsertCharacter(key.KeyChar));
                        RenderUnlessMoreInputIsPending(prompt, continuationPrompt);
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            if (!control && !alt && (key.Modifiers & ConsoleModifiers.Shift) == 0 &&
                                ShouldSubmit(isCompleteSubmission))
                            {
                                var submission = _buffer.Text;
                                _buffer.MoveToEnd();
                                _quickInfoArmed = false;
                                Render(prompt, continuationPrompt);
                                _renderer.Finish();
                                History.Add(submission);
                                return submission;
                            }
                            InsertNewLine();
                            break;

                        case ConsoleKey.Tab:
                            HandleTab((key.Modifiers & ConsoleModifiers.Shift) == 0);
                            break;

                        case ConsoleKey.Backspace:
                            Modify(() =>
                            {
                                if (control || alt)
                                {
                                    _buffer.KillWordLeft();
                                }
                                else
                                {
                                    _buffer.Backspace();
                                }
                            });
                            break;

                        case ConsoleKey.Delete:
                            Modify(() =>
                            {
                                if (control || alt)
                                {
                                    _buffer.KillWordRight();
                                }
                                else
                                {
                                    _buffer.Delete();
                                }
                            });
                            break;

                        case ConsoleKey.LeftArrow:
                            if (control || alt)
                            {
                                _buffer.MoveWordLeft();
                            }
                            else
                            {
                                _buffer.MoveLeft();
                            }
                            break;

                        case ConsoleKey.RightArrow:
                            if (control || alt)
                            {
                                _buffer.MoveWordRight();
                            }
                            else
                            {
                                _buffer.MoveRight();
                            }
                            break;

                        case ConsoleKey.UpArrow:
                            if (!_buffer.IsOnFirstLine)
                            {
                                _buffer.MoveUp();
                            }
                            else
                            {
                                HistoryPrevious();
                            }
                            break;

                        case ConsoleKey.DownArrow:
                            if (!_buffer.IsOnLastLine)
                            {
                                _buffer.MoveDown();
                            }
                            else
                            {
                                HistoryNext();
                            }
                            break;

                        case ConsoleKey.Home:
                            if (control)
                            {
                                _buffer.MoveToStart();
                            }
                            else
                            {
                                _buffer.MoveToLineStart();
                            }
                            break;

                        case ConsoleKey.End:
                            if (control)
                            {
                                _buffer.MoveToEnd();
                            }
                            else
                            {
                                _buffer.MoveToLineEnd();
                            }
                            break;

                        case ConsoleKey.Escape:
                            // Escape sequences that the terminal fails to map arrive as a bare Escape,
                            // so clearing the line takes a deliberate second press.
                            if (!completionWasActive)
                            {
                                if (_quickInfoArmed)
                                {
                                    _quickInfoArmed = false;
                                }
                                else if (escapeWasArmed)
                                {
                                    Modify(() => _buffer.KillAll());
                                    _escapeArmed = false;
                                }
                                else
                                {
                                    _escapeArmed = true;
                                }
                            }
                            break;

                        case ConsoleKey.G when control:
                            Modify(() => _buffer.KillAll());
                            break;

                        case ConsoleKey.A when control:
                            _buffer.MoveToLineStart();
                            break;

                        case ConsoleKey.E when control:
                            _buffer.MoveToLineEnd();
                            break;

                        case ConsoleKey.B when control || alt:
                            if (alt)
                            {
                                _buffer.MoveWordLeft();
                            }
                            else
                            {
                                _buffer.MoveLeft();
                            }
                            break;

                        case ConsoleKey.F when control || alt:
                            if (alt)
                            {
                                _buffer.MoveWordRight();
                            }
                            else
                            {
                                _buffer.MoveRight();
                            }
                            break;

                        case ConsoleKey.K when control:
                            Modify(() => _buffer.KillToLineEnd());
                            break;

                        case ConsoleKey.U when control:
                            Modify(() => _buffer.KillToLineStart());
                            break;

                        case ConsoleKey.W when control:
                            Modify(() => _buffer.KillWordLeft());
                            break;

                        case ConsoleKey.Y when control:
                            Modify(() => _buffer.Yank());
                            break;

                        case ConsoleKey.P when control:
                            HistoryPrevious();
                            break;

                        case ConsoleKey.N when control:
                            HistoryNext();
                            break;

                        case ConsoleKey.R when control:
                            BeginReverseSearch();
                            break;

                        case ConsoleKey.L when control:
                            _renderer.ClearScreen();
                            break;

                        case ConsoleKey.C when control:
                            _buffer.MoveToEnd();
                            _quickInfoArmed = false;
                            Render(prompt, continuationPrompt);
                            _renderer.Finish("^C");
                            return string.Empty;

                        case ConsoleKey.D when control:
                            if (_buffer.IsEmpty)
                            {
                                _renderer.Finish();
                                return null;
                            }
                            Modify(() => _buffer.Delete());
                            break;

                        case ConsoleKey.Spacebar when control:
                            _quickInfoArmed = true;
                            break;

                        default:
                            if (!control && !alt && !char.IsControl(key.KeyChar))
                            {
                                Modify(() => InsertCharacter(key.KeyChar));
                            }
                            break;
                    }

                    RenderUnlessMoreInputIsPending(prompt, continuationPrompt);
                }
            }
            finally
            {
                _device.TreatControlCAsInput = previousTreatControlCAsInput;
                _device.ResetColor();
            }
        }

        private bool ShouldSubmit(Func<string, bool> isCompleteSubmission)
        {
            var text = _buffer.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            // Enter inside an existing block edits the block instead of submitting it.
            if (_buffer.IsMultiLine && !_buffer.IsAtEnd)
            {
                return false;
            }

            return isCompleteSubmission == null || isCompleteSubmission(text);
        }

        private void InsertCharacter(char value)
        {
            if (value == '}' && !IsPasting && CurrentLineIsBlank())
            {
                Dedent();
            }

            if (value == '(')
            {
                _quickInfoArmed = true;
            }
            else if (value == ')')
            {
                _quickInfoArmed = false;
            }

            _buffer.Insert(value);
        }

        /// <summary>
        /// A paste arrives as a burst of buffered key strokes. Repainting after each of them
        /// makes large pastes quadratic, so the screen is only refreshed once input drains.
        /// A fast typist trips this too, which at worst costs a line its auto-indent.
        /// </summary>
        private bool IsPasting => _device.KeyAvailable;

        private void RenderUnlessMoreInputIsPending(string prompt, string continuationPrompt)
        {
            if (!IsPasting)
            {
                Render(prompt, continuationPrompt);
            }
        }

        private bool CurrentLineIsBlank()
        {
            var text = _buffer.Text;
            for (var i = _buffer.LineStart(_buffer.Caret); i < _buffer.Caret; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private void Dedent()
        {
            var lineStart = _buffer.LineStart(_buffer.Caret);
            var removable = Math.Min(IndentSize, _buffer.Caret - lineStart);
            if (removable > 0)
            {
                _buffer.Replace(_buffer.Caret - removable, removable, string.Empty);
            }
        }

        private void InsertNewLine()
        {
            Modify(() =>
            {
                // Pasted text carries its own indentation; adding to it would compound on every line.
                var indent = IsPasting ? string.Empty : new string(' ', ComputeIndentDepth() * IndentSize);
                _buffer.Insert("\n" + indent);
            });
        }

        private int ComputeIndentDepth()
        {
            var text = _buffer.Text;
            var mask = GetCodeMask(text);
            var depth = 0;

            for (var i = 0; i < _buffer.Caret && i < text.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                var c = text[i];
                if (c == '{' || c == '(' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ')' || c == ']')
                {
                    depth = Math.Max(0, depth - 1);
                }
            }

            return depth;
        }

        private void Modify(Action action)
        {
            action();
            History.ResetNavigation();
        }

        private void HistoryPrevious()
        {
            if (History.TryGetPrevious(_buffer.Text, _buffer.Caret, out var entry))
            {
                _buffer.SetText(entry);
            }
        }

        private void HistoryNext()
        {
            if (History.TryGetNext(out var entry))
            {
                _buffer.SetText(entry);
            }
        }

        private void BeginReverseSearch()
        {
            if (History.Count == 0)
            {
                return;
            }

            _searchActive = true;
            _searchTerm = string.Empty;
            _searchIndex = History.Count - 1;
            _searchSavedText = _buffer.Text;
        }

        private void HandleSearchKey(ConsoleKeyInfo key)
        {
            var control = (key.Modifiers & ConsoleModifiers.Control) != 0;

            if (key.Key == ConsoleKey.Escape)
            {
                _buffer.SetText(_searchSavedText);
                _searchActive = false;
                return;
            }

            if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow)
            {
                _searchActive = false;
                return;
            }

            if (key.Key == ConsoleKey.R && control)
            {
                UpdateSearch(_searchTerm, _searchIndex - 1);
                return;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                var term = _searchTerm.Length > 0 ? _searchTerm.Substring(0, _searchTerm.Length - 1) : string.Empty;
                UpdateSearch(term, History.Count - 1);
                return;
            }

            if (!control && !char.IsControl(key.KeyChar))
            {
                UpdateSearch(_searchTerm + key.KeyChar, History.Count - 1);
            }
        }

        private void UpdateSearch(string term, int startIndex)
        {
            _searchTerm = term;

            if (startIndex < 0)
            {
                return;
            }

            if (History.TryReverseSearch(term, startIndex, out var index, out var entry))
            {
                _searchIndex = index;
                _buffer.SetText(entry);
            }
        }

        private void HandleTab(bool forward)
        {
            if (_completion == null)
            {
                if (CompletionProvider == null || CurrentLineIsBlank())
                {
                    Modify(() => _buffer.Insert(new string(' ', IndentSize)));
                    return;
                }

                CompletionResult result;
                try
                {
                    result = CompletionProvider.GetCompletions(_buffer.Text, _buffer.Caret);
                }
                catch (Exception)
                {
                    // Completion is a convenience - a broken provider must not end the session.
                    return;
                }

                if (result == null || result.IsEmpty)
                {
                    return;
                }

                var start = Math.Max(0, Math.Min(result.Start, _buffer.Length));
                var length = Math.Max(0, Math.Min(result.Length, _buffer.Length - start));

                if (result.Items.Count == 1)
                {
                    ApplyCompletion(start, length, result.Items[0]);
                    return;
                }

                var typed = _buffer.Text.Substring(start, length);
                var commonPrefix = LongestCommonPrefix(result.Items);

                _completion = new CompletionState(start, length, result.Items);

                if (commonPrefix.Length > typed.Length)
                {
                    _completion.Length = commonPrefix.Length;
                    ApplyCompletion(start, length, commonPrefix);
                    return;
                }
            }

            var count = _completion.Items.Count;
            _completion.Index = ((_completion.Index + (forward ? 1 : -1)) % count + count) % count;
            var candidate = _completion.Items[_completion.Index];
            ApplyCompletion(_completion.Start, _completion.Length, candidate);
            _completion.Length = candidate.Length;
        }

        private void ApplyCompletion(int start, int length, string value)
        {
            _buffer.Replace(start, length, value);
            History.ResetNavigation();
        }

        private static string LongestCommonPrefix(IReadOnlyList<string> items)
        {
            var prefix = items[0];
            for (var i = 1; i < items.Count && prefix.Length > 0; i++)
            {
                var candidate = items[i];
                var length = Math.Min(prefix.Length, candidate.Length);
                var common = 0;
                while (common < length && char.ToLowerInvariant(prefix[common]) == char.ToLowerInvariant(candidate[common]))
                {
                    common++;
                }
                prefix = prefix.Substring(0, common);
            }

            return prefix;
        }

        private void Render(string prompt, string continuationPrompt)
        {
            if (_searchActive)
            {
                _renderer.Render($"(reverse-i-search)`{_searchTerm}': ", continuationPrompt, _buffer.Text, _buffer.Caret, NoSpans);
                return;
            }

            var text = _buffer.Text;
            var spans = EnableSyntaxHighlighting ? Classify(text) : NoSpans;
            var brackets = FindBracketPair(text, _buffer.Caret);
            _renderer.Render(prompt, continuationPrompt, text, _buffer.Caret, spans, brackets.Item1, brackets.Item2, GetQuickInfo(text));
        }

        private string GetQuickInfo(string text)
        {
            if (!_quickInfoArmed || QuickInfoProvider == null)
            {
                return null;
            }

            var key = _buffer.Caret + ":" + text;
            if (string.Equals(_quickInfoKey, key, StringComparison.Ordinal))
            {
                return _quickInfoText;
            }

            _quickInfoKey = key;
            try
            {
                _quickInfoText = QuickInfoProvider.GetQuickInfo(text, _buffer.Caret);
            }
            catch (Exception)
            {
                // Quick info is a convenience - a broken provider must not end the session.
                _quickInfoText = null;
            }

            return _quickInfoText;
        }

        private IReadOnlyList<ClassifiedSpan> Classify(string text)
        {
            if (!ReferenceEquals(_cachedText, text) && !string.Equals(_cachedText, text, StringComparison.Ordinal))
            {
                _cachedText = text;
                _cachedSpans = ClassifySafely(text);
                _cachedMask = null;
            }

            return _cachedSpans ?? NoSpans;
        }

        private IReadOnlyList<ClassifiedSpan> ClassifySafely(string text)
        {
            try
            {
                return Classifier?.Classify(text);
            }
            catch (Exception)
            {
                // Classification is cosmetic - never let it break the editor.
                return NoSpans;
            }
        }

        private bool[] GetCodeMask(string text)
        {
            var spans = Classify(text);
            return _cachedMask ?? (_cachedMask = SyntaxHighlighter.GetCodeMask(text, spans));
        }

        private Tuple<int, int> FindBracketPair(string text, int caret)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Tuple.Create(-1, -1);
            }

            var mask = GetCodeMask(text);

            foreach (var index in new[] { caret, caret - 1 })
            {
                if (index < 0 || index >= text.Length || !mask[index])
                {
                    continue;
                }

                var match = FindMatchingBracket(text, mask, index);
                if (match >= 0)
                {
                    return Tuple.Create(index, match);
                }
            }

            return Tuple.Create(-1, -1);
        }

        private static int FindMatchingBracket(string text, bool[] mask, int index)
        {
            var c = text[index];
            var open = "([{";
            var close = ")]}";

            var openIndex = open.IndexOf(c);
            var closeIndex = close.IndexOf(c);

            if (openIndex < 0 && closeIndex < 0)
            {
                return -1;
            }

            var forward = openIndex >= 0;
            var self = c;
            var other = forward ? close[openIndex] : open[closeIndex];
            var depth = 0;

            for (var i = index; forward ? i < text.Length : i >= 0; i += forward ? 1 : -1)
            {
                if (!mask[i])
                {
                    continue;
                }

                if (text[i] == self)
                {
                    depth++;
                }
                else if (text[i] == other)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private sealed class CompletionState
        {
            public CompletionState(int start, int length, IReadOnlyList<string> items)
            {
                Start = start;
                Length = length;
                Items = items;
                Index = -1;
            }

            public int Start { get; }

            public int Length { get; set; }

            public IReadOnlyList<string> Items { get; }

            public int Index { get; set; }
        }
    }
}
