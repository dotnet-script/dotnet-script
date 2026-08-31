using System;
using System.Collections.Generic;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    public sealed class CompletionResult
    {
        public static readonly CompletionResult Empty = new CompletionResult(0, 0, new string[0]);

        public CompletionResult(int start, int length, IReadOnlyList<string> items)
        {
            Start = start;
            Length = length;
            Items = items ?? new string[0];
        }

        /// <summary>Start of the span in the input that the completion replaces.</summary>
        public int Start { get; }

        /// <summary>Length of the span in the input that the completion replaces.</summary>
        public int Length { get; }

        public IReadOnlyList<string> Items { get; }

        public bool IsEmpty => Items.Count == 0;
    }

    public interface ICompletionProvider
    {
        CompletionResult GetCompletions(string text, int caret);
    }
}
