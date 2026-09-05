namespace Dotnet.Script.Core.Interactive.LineEditing
{
    public enum ClassificationKind
    {
        Default,
        Keyword,
        Identifier,
        String,
        Number,
        Comment,
        Directive,
        Punctuation,
        Type,
        Method
    }

    public readonly struct ClassifiedSpan
    {
        public ClassifiedSpan(int start, int length, ClassificationKind kind)
        {
            Start = start;
            Length = length;
            Kind = kind;
        }

        public int Start { get; }

        public int Length { get; }

        public int End => Start + Length;

        public ClassificationKind Kind { get; }
    }
}
