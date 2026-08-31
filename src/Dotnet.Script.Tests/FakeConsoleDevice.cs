using System;
using System.Collections.Generic;
using System.Linq;
using Dotnet.Script.Core.Interactive.LineEditing;

namespace Dotnet.Script.Tests
{
    /// <summary>
    /// In-memory console used to drive <see cref="LineEditor"/> from tests.
    /// Keeps a virtual screen buffer so rendering can be asserted on.
    /// </summary>
    public sealed class FakeConsoleDevice : IConsoleDevice
    {
        private readonly List<char[]> _screen = new List<char[]>();
        private readonly Queue<PendingKey> _keys = new Queue<PendingKey>();
        private readonly int _width;
        private readonly int _height;

        public FakeConsoleDevice(int width = 40, int height = 10)
        {
            _width = width;
            _height = height;
            for (var i = 0; i < height; i++)
            {
                _screen.Add(CreateRow());
            }
        }

        public int Width => _width;

        public int Height => _height;

        public int CursorLeft { get; private set; }

        public int CursorTop { get; private set; }

        public bool CursorVisible { set { } }

        public ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;

        public bool TreatControlCAsInput { get; set; }

        public bool SupportsColors { get; set; }

        // Only pasted keys are already buffered; typed keys arrive one at a time.
        public bool KeyAvailable => _keys.Count > 0 && _keys.Peek().IsPasted;

        /// <summary>Counts writes to the screen, used to assert that a paste does not repaint per key.</summary>
        public int WriteCount { get; private set; }

        public void Enqueue(params ConsoleKeyInfo[] keys) => Enqueue((IEnumerable<ConsoleKeyInfo>)keys);

        public void Enqueue(IEnumerable<ConsoleKeyInfo> keys)
        {
            foreach (var key in keys)
            {
                _keys.Enqueue(new PendingKey(key, isPasted: false));
            }
        }

        /// <summary>Enqueues text as a single burst, the way a terminal delivers a paste.</summary>
        public void EnqueuePaste(string text)
        {
            foreach (var c in text)
            {
                _keys.Enqueue(new PendingKey(c == '\n' ? Key.Enter : Key.Char(c), isPasted: true));
            }
        }

        /// <summary>
        /// Stops the read loop while leaving the last rendered frame on screen, so that rendering which
        /// only happens mid-edit can be asserted on.
        /// </summary>
        public void EnqueueStop() => _keys.Enqueue(new PendingKey(default, isPasted: false, isStop: true));

        public ConsoleKeyInfo ReadKey()
        {
            if (_keys.Count == 0)
            {
                throw new InvalidOperationException("The test did not provide enough key strokes.");
            }

            var pending = _keys.Dequeue();

            return pending.IsStop ? throw new StopReadingException() : pending.Key;
        }

        /// <summary>Thrown by <see cref="ReadKey"/> in response to <see cref="EnqueueStop"/>.</summary>
        public sealed class StopReadingException : Exception
        {
        }

        public void Write(string value)
        {
            WriteCount++;

            foreach (var c in value ?? string.Empty)
            {
                if (c == '\r')
                {
                    CursorLeft = 0;
                    continue;
                }

                if (c == '\n')
                {
                    CursorLeft = 0;
                    MoveDown();
                    continue;
                }

                if (CursorLeft >= _width)
                {
                    CursorLeft = 0;
                    MoveDown();
                }

                _screen[CursorTop][CursorLeft] = c;
                CursorLeft++;
            }
        }

        public void SetCursorPosition(int left, int top)
        {
            CursorLeft = Math.Max(0, Math.Min(left, _width - 1));
            CursorTop = Math.Max(0, Math.Min(top, _height - 1));
        }

        public void ResetColor() => ForegroundColor = ConsoleColor.Gray;

        public void Clear()
        {
            for (var i = 0; i < _screen.Count; i++)
            {
                _screen[i] = CreateRow();
            }

            CursorLeft = 0;
            CursorTop = 0;
        }

        public string GetRow(int row) => new string(_screen[row]).TrimEnd();

        public string[] GetRows() => _screen.Select(row => new string(row).TrimEnd()).ToArray();

        public string GetScreen() => string.Join(Environment.NewLine, GetRows()).TrimEnd();

        private void MoveDown()
        {
            if (CursorTop < _height - 1)
            {
                CursorTop++;
                return;
            }

            _screen.RemoveAt(0);
            _screen.Add(CreateRow());
        }

        private char[] CreateRow() => Enumerable.Repeat(' ', _width).ToArray();

        private readonly struct PendingKey
        {
            public PendingKey(ConsoleKeyInfo key, bool isPasted, bool isStop = false)
            {
                Key = key;
                IsPasted = isPasted;
                IsStop = isStop;
            }

            public ConsoleKeyInfo Key { get; }

            public bool IsPasted { get; }

            public bool IsStop { get; }
        }

        public static class Key
        {
            public static ConsoleKeyInfo Char(char c)
            {
                var key = char.IsLetter(c)
                    ? (ConsoleKey)char.ToUpperInvariant(c)
                    : char.IsDigit(c) ? (ConsoleKey)c : default;

                return new ConsoleKeyInfo(c, key, shift: char.IsUpper(c), alt: false, control: false);
            }

            public static IEnumerable<ConsoleKeyInfo> Type(string text) => text.Select(Char);

            public static ConsoleKeyInfo Press(ConsoleKey key, ConsoleModifiers modifiers = 0) =>
                new ConsoleKeyInfo(
                    '\0',
                    key,
                    (modifiers & ConsoleModifiers.Shift) != 0,
                    (modifiers & ConsoleModifiers.Alt) != 0,
                    (modifiers & ConsoleModifiers.Control) != 0);

            public static ConsoleKeyInfo Ctrl(ConsoleKey key) => Press(key, ConsoleModifiers.Control);

            public static ConsoleKeyInfo Enter => Press(ConsoleKey.Enter);

            public static ConsoleKeyInfo Tab => Press(ConsoleKey.Tab);
        }
    }

    internal sealed class StaticCompletionProvider : ICompletionProvider
    {
        private readonly string[] _items;

        public StaticCompletionProvider(params string[] items) => _items = items;

        public CompletionResult GetCompletions(string text, int caret)
        {
            var start = caret;
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_'))
            {
                start--;
            }

            var prefix = text.Substring(start, caret - start);
            var matches = _items.Where(i => i.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            return new CompletionResult(start, prefix.Length, matches);
        }
    }

    internal sealed class ThrowingCompletionProvider : ICompletionProvider
    {
        public CompletionResult GetCompletions(string text, int caret) => throw new InvalidOperationException("boom");
    }

    internal sealed class OutOfRangeCompletionProvider : ICompletionProvider
    {
        public CompletionResult GetCompletions(string text, int caret) =>
            new CompletionResult(999, 999, new[] { "item" });
    }

    internal static class BraceCompleteness
    {
        /// <summary>Minimal stand-in for Roslyn's submission completeness check.</summary>
        public static bool IsComplete(string text)
        {
            var depth = 0;
            foreach (var c in text)
            {
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                }
            }

            return depth <= 0;
        }
    }
}
