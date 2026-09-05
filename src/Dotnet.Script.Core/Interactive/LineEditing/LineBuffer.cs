using System;
using System.Text;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Mutable, multi-line text buffer with a caret. Pure model - it knows nothing
    /// about the console, which makes all editing operations directly testable.
    /// Lines are separated by a single '\n'.
    /// </summary>
    public sealed class LineBuffer
    {
        private readonly StringBuilder _text = new StringBuilder();
        private string _cachedText = string.Empty;
        private int _caret;

        // Reading the text is on the hot path of every keystroke, so materialize it only after a change.
        public string Text => _cachedText ?? (_cachedText = _text.ToString());

        public int Length => _text.Length;

        public int Caret => _caret;

        public bool IsEmpty => _text.Length == 0;

        public bool IsAtEnd => _caret == _text.Length;

        /// <summary>The most recently killed text, used by the yank command.</summary>
        public string Killed { get; private set; } = string.Empty;

        public void Clear()
        {
            _text.Clear();
            _cachedText = string.Empty;
            _caret = 0;
        }

        public void SetText(string text)
        {
            _text.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                _text.Append(text);
            }
            _cachedText = null;
            _caret = _text.Length;
        }

        public void SetCaret(int index) => _caret = Clamp(index);

        public void Insert(char value)
        {
            _text.Insert(_caret, value);
            _cachedText = null;
            _caret++;
        }

        public void Insert(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _text.Insert(_caret, value);
            _cachedText = null;
            _caret += value.Length;
        }

        public void Replace(int start, int length, string value)
        {
            start = Clamp(start);
            length = Math.Max(0, Math.Min(length, _text.Length - start));
            _text.Remove(start, length);
            _text.Insert(start, value ?? string.Empty);
            _cachedText = null;
            _caret = start + (value?.Length ?? 0);
        }

        public bool MoveLeft()
        {
            if (_caret == 0)
            {
                return false;
            }

            _caret -= CharWidthBefore(_caret);
            return true;
        }

        public bool MoveRight()
        {
            if (IsAtEnd)
            {
                return false;
            }

            _caret += CharWidthAt(_caret);
            return true;
        }

        public bool MoveWordLeft()
        {
            if (_caret == 0)
            {
                return false;
            }

            var index = _caret;
            while (index > 0 && !IsWordChar(_text[index - 1]))
            {
                index--;
            }
            while (index > 0 && IsWordChar(_text[index - 1]))
            {
                index--;
            }

            _caret = index;
            return true;
        }

        public bool MoveWordRight()
        {
            if (IsAtEnd)
            {
                return false;
            }

            var index = _caret;
            while (index < _text.Length && !IsWordChar(_text[index]))
            {
                index++;
            }
            while (index < _text.Length && IsWordChar(_text[index]))
            {
                index++;
            }

            _caret = index;
            return true;
        }

        public void MoveToLineStart() => _caret = LineStart(_caret);

        public void MoveToLineEnd() => _caret = LineEnd(_caret);

        public void MoveToStart() => _caret = 0;

        public void MoveToEnd() => _caret = _text.Length;

        public bool MoveUp()
        {
            var lineStart = LineStart(_caret);
            if (lineStart == 0)
            {
                return false;
            }

            var column = _caret - lineStart;
            var previousLineStart = LineStart(lineStart - 1);
            var previousLineLength = lineStart - 1 - previousLineStart;
            _caret = previousLineStart + Math.Min(column, previousLineLength);
            return true;
        }

        public bool MoveDown()
        {
            var lineEnd = LineEnd(_caret);
            if (lineEnd == _text.Length)
            {
                return false;
            }

            var column = _caret - LineStart(_caret);
            var nextLineStart = lineEnd + 1;
            var nextLineLength = LineEnd(nextLineStart) - nextLineStart;
            _caret = nextLineStart + Math.Min(column, nextLineLength);
            return true;
        }

        public bool Backspace()
        {
            if (_caret == 0)
            {
                return false;
            }

            var width = CharWidthBefore(_caret);
            _text.Remove(_caret - width, width);
            _cachedText = null;
            _caret -= width;
            return true;
        }

        public bool Delete()
        {
            if (IsAtEnd)
            {
                return false;
            }

            _text.Remove(_caret, CharWidthAt(_caret));
            _cachedText = null;
            return true;
        }

        public bool KillWordLeft()
        {
            var end = _caret;
            if (!MoveWordLeft())
            {
                return false;
            }

            return Kill(_caret, end - _caret);
        }

        public bool KillWordRight()
        {
            var start = _caret;
            var caret = _caret;
            if (!MoveWordRight())
            {
                return false;
            }

            var end = _caret;
            _caret = caret;
            return Kill(start, end - start);
        }

        public bool KillToLineStart()
        {
            var start = LineStart(_caret);
            return Kill(start, _caret - start);
        }

        public bool KillToLineEnd()
        {
            var end = LineEnd(_caret);
            if (end == _caret && !IsAtEnd)
            {
                // On an empty line, swallow the line break itself.
                return Kill(_caret, 1);
            }

            return Kill(_caret, end - _caret);
        }

        /// <summary>Removes everything, keeping it in the kill ring so that it can be yanked back.</summary>
        public bool KillAll() => Kill(0, _text.Length);

        public void Yank() => Insert(Killed);

        public int LineStart(int index)
        {
            index = Clamp(index);
            while (index > 0 && _text[index - 1] != '\n')
            {
                index--;
            }
            return index;
        }

        public int LineEnd(int index)
        {
            index = Clamp(index);
            while (index < _text.Length && _text[index] != '\n')
            {
                index++;
            }
            return index;
        }

        public bool IsOnFirstLine => LineStart(_caret) == 0;

        public bool IsOnLastLine => LineEnd(_caret) == _text.Length;

        public bool IsMultiLine
        {
            get
            {
                for (var i = 0; i < _text.Length; i++)
                {
                    if (_text[i] == '\n')
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private bool Kill(int start, int length)
        {
            if (length <= 0)
            {
                return false;
            }

            Killed = _text.ToString(start, length);
            _text.Remove(start, length);
            _cachedText = null;
            _caret = start;
            return true;
        }

        private int CharWidthAt(int index) =>
            index + 1 < _text.Length && char.IsHighSurrogate(_text[index]) && char.IsLowSurrogate(_text[index + 1]) ? 2 : 1;

        private int CharWidthBefore(int index) =>
            index >= 2 && char.IsLowSurrogate(_text[index - 1]) && char.IsHighSurrogate(_text[index - 2]) ? 2 : 1;

        private int Clamp(int index) => Math.Max(0, Math.Min(index, _text.Length));

        internal static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
