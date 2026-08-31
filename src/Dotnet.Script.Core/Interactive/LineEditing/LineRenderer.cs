using System;
using System.Collections.Generic;
using System.Text;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Paints the current input onto the console. Handles prompts, line wrapping,
    /// multi-line submissions, colorization and scrolling, repainting only what changed.
    /// Layout counts UTF-16 code units as columns, so wide glyphs (CJK, emoji) wrap early.
    /// </summary>
    public sealed class LineRenderer
    {
        private const int TabWidth = 4;

        private readonly IConsoleDevice _device;
        private readonly ReplColorScheme _colors;
        private readonly List<int> _previousRowLengths = new List<int>();

        private int _anchorTop;
        private int _renderedRows;
        private int _lastWidth;

        public LineRenderer(IConsoleDevice device, ReplColorScheme colors)
        {
            _device = device;
            _colors = colors ?? ReplColorScheme.Default;
        }

        /// <summary>Anchors the renderer at the current cursor position.</summary>
        public void Start()
        {
            if (_device.CursorLeft != 0)
            {
                _device.Write(Environment.NewLine);
            }

            _anchorTop = _device.CursorTop;
            _previousRowLengths.Clear();
            _renderedRows = 0;
            _lastWidth = UsableWidth();
        }

        public void Render(
            string prompt,
            string continuationPrompt,
            string text,
            int caret,
            IReadOnlyList<ClassifiedSpan> spans,
            int highlightA = -1,
            int highlightB = -1)
        {
            var width = UsableWidth();
            if (width != _lastWidth)
            {
                // Geometry changed; previously painted rows can no longer be reasoned about.
                _previousRowLengths.Clear();
                _lastWidth = width;
            }

            var layout = BuildLayout(prompt, continuationPrompt, text ?? string.Empty, caret, spans, highlightA, highlightB, width);

            EnsureRoomFor(layout.Rows.Count);

            _device.CursorVisible = false;

            for (var row = 0; row < layout.Rows.Count; row++)
            {
                _device.SetCursorPosition(0, _anchorTop + row);
                PaintRow(layout, row);

                var previousLength = row < _previousRowLengths.Count ? _previousRowLengths[row] : 0;
                var padding = previousLength - layout.Rows[row].Length;
                if (padding > 0)
                {
                    _device.ResetColor();
                    _device.Write(new string(' ', padding));
                }
            }

            for (var row = layout.Rows.Count; row < _previousRowLengths.Count; row++)
            {
                if (_previousRowLengths[row] <= 0)
                {
                    continue;
                }

                _device.SetCursorPosition(0, _anchorTop + row);
                _device.ResetColor();
                _device.Write(new string(' ', _previousRowLengths[row]));
            }

            _device.ResetColor();

            _previousRowLengths.Clear();
            foreach (var row in layout.Rows)
            {
                _previousRowLengths.Add(row.Length);
            }
            _renderedRows = layout.Rows.Count;

            _device.SetCursorPosition(layout.CaretColumn, _anchorTop + layout.CaretRow);
            _device.CursorVisible = true;
        }

        /// <summary>Moves past the rendered input so that subsequent output starts on a fresh line.</summary>
        public void Finish(string suffix = null)
        {
            var lastRow = Math.Max(0, _renderedRows - 1);
            var lastLength = _renderedRows > 0 ? _previousRowLengths[lastRow] : 0;
            _device.SetCursorPosition(lastLength, _anchorTop + lastRow);
            _device.ResetColor();
            if (!string.IsNullOrEmpty(suffix))
            {
                _device.Write(suffix);
            }
            _device.Write(Environment.NewLine);
            _previousRowLengths.Clear();
            _renderedRows = 0;
        }

        public void ClearScreen()
        {
            _device.Clear();
            _anchorTop = 0;
            _previousRowLengths.Clear();
            _renderedRows = 0;
        }

        // The last column is deliberately left unused: writing into it triggers
        // terminal specific auto-wrap behaviour that makes cursor math unreliable.
        private int UsableWidth() => Math.Max(1, _device.Width - 1);

        private void EnsureRoomFor(int rowCount)
        {
            var overflow = _anchorTop + rowCount - _device.Height;
            if (overflow <= 0)
            {
                return;
            }

            var before = _device.Height - 1;
            _device.SetCursorPosition(0, before);
            _device.Write(new string('\n', overflow));

            var moved = _device.CursorTop - before;
            var scrolled = overflow - moved;
            _anchorTop = Math.Max(0, _anchorTop - scrolled);
        }

        private void PaintRow(Layout layout, int rowIndex)
        {
            var row = layout.Rows[rowIndex];
            var colors = layout.Colors[rowIndex];

            var index = 0;
            while (index < row.Length)
            {
                var color = colors[index];
                var end = index + 1;
                while (end < row.Length && colors[end] == color)
                {
                    end++;
                }

                if (color.HasValue)
                {
                    _device.ForegroundColor = color.Value;
                }
                else
                {
                    _device.ResetColor();
                }

                _device.Write(row.ToString(index, end - index));
                index = end;
            }
        }

        private Layout BuildLayout(
            string prompt,
            string continuationPrompt,
            string text,
            int caret,
            IReadOnlyList<ClassifiedSpan> spans,
            int highlightA,
            int highlightB,
            int width)
        {
            var useColors = _device.SupportsColors;
            var charColors = BuildCharColors(text, spans, highlightA, highlightB, useColors);
            ConsoleColor? promptColor = useColors ? _colors.Prompt : (ConsoleColor?)null;

            var layout = new Layout(width);
            layout.AppendRow();
            layout.Append(prompt, promptColor);

            var caretPlaced = false;
            for (var i = 0; i < text.Length; i++)
            {
                if (i == caret)
                {
                    layout.PlaceCaret();
                    caretPlaced = true;
                }

                var c = text[i];
                if (c == '\n')
                {
                    layout.AppendRow();
                    layout.Append(continuationPrompt, promptColor);
                    continue;
                }

                if (c == '\t')
                {
                    layout.Append(new string(' ', TabWidth), charColors[i]);
                    continue;
                }

                layout.Append(c, charColors[i]);
            }

            if (!caretPlaced)
            {
                layout.PlaceCaret();
            }

            layout.EnsureCaretRowExists();
            return layout;
        }

        private ConsoleColor?[] BuildCharColors(
            string text,
            IReadOnlyList<ClassifiedSpan> spans,
            int highlightA,
            int highlightB,
            bool useColors)
        {
            var charColors = new ConsoleColor?[text.Length];
            if (!useColors)
            {
                return charColors;
            }

            if (spans != null)
            {
                foreach (var span in spans)
                {
                    var color = _colors.For(span.Kind);
                    if (!color.HasValue)
                    {
                        continue;
                    }

                    for (var i = span.Start; i < span.End && i < charColors.Length; i++)
                    {
                        charColors[i] = color;
                    }
                }
            }

            if (highlightA >= 0 && highlightA < charColors.Length)
            {
                charColors[highlightA] = _colors.MatchingBracket;
            }

            if (highlightB >= 0 && highlightB < charColors.Length)
            {
                charColors[highlightB] = _colors.MatchingBracket;
            }

            return charColors;
        }

        private sealed class Layout
        {
            private readonly int _width;

            public Layout(int width)
            {
                _width = width;
            }

            public List<StringBuilder> Rows { get; } = new List<StringBuilder>();

            public List<List<ConsoleColor?>> Colors { get; } = new List<List<ConsoleColor?>>();

            public int CaretRow { get; private set; }

            public int CaretColumn { get; private set; }

            public void AppendRow()
            {
                Rows.Add(new StringBuilder());
                Colors.Add(new List<ConsoleColor?>());
            }

            public void Append(string value, ConsoleColor? color)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                foreach (var c in value)
                {
                    Append(c, color);
                }
            }

            public void Append(char value, ConsoleColor? color)
            {
                var last = Rows.Count - 1;
                if (Rows[last].Length >= _width)
                {
                    AppendRow();
                    last++;
                }

                Rows[last].Append(value);
                Colors[last].Add(color);
            }

            public void PlaceCaret()
            {
                var last = Rows.Count - 1;
                if (Rows[last].Length >= _width)
                {
                    CaretRow = last + 1;
                    CaretColumn = 0;
                }
                else
                {
                    CaretRow = last;
                    CaretColumn = Rows[last].Length;
                }
            }

            public void EnsureCaretRowExists()
            {
                while (CaretRow >= Rows.Count)
                {
                    AppendRow();
                }
            }
        }
    }
}
