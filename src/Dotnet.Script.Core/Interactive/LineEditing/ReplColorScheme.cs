using System;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Maps classifications to console colors. Only the 16 <see cref="ConsoleColor"/> values are used
    /// so that the REPL renders correctly on legacy terminals without ANSI support.
    /// </summary>
    public class ReplColorScheme
    {
        public static ReplColorScheme Default { get; } = new ReplColorScheme();

        public ConsoleColor Prompt { get; set; } = ConsoleColor.DarkGray;

        public ConsoleColor Keyword { get; set; } = ConsoleColor.Cyan;

        public ConsoleColor String { get; set; } = ConsoleColor.DarkYellow;

        public ConsoleColor Number { get; set; } = ConsoleColor.Green;

        public ConsoleColor Comment { get; set; } = ConsoleColor.DarkGreen;

        public ConsoleColor Directive { get; set; } = ConsoleColor.Magenta;

        public ConsoleColor Punctuation { get; set; } = ConsoleColor.DarkGray;

        public ConsoleColor Type { get; set; } = ConsoleColor.DarkCyan;

        public ConsoleColor Method { get; set; } = ConsoleColor.Yellow;

        public ConsoleColor MatchingBracket { get; set; } = ConsoleColor.White;

        public ConsoleColor QuickInfo { get; set; } = ConsoleColor.DarkGray;

        public virtual ConsoleColor? For(ClassificationKind kind)
        {
            switch (kind)
            {
                case ClassificationKind.Keyword: return Keyword;
                case ClassificationKind.String: return String;
                case ClassificationKind.Number: return Number;
                case ClassificationKind.Comment: return Comment;
                case ClassificationKind.Directive: return Directive;
                case ClassificationKind.Punctuation: return Punctuation;
                case ClassificationKind.Type: return Type;
                case ClassificationKind.Method: return Method;
                default: return null;
            }
        }
    }
}
