using System.Collections.Generic;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    public interface ISyntaxClassifier
    {
        /// <summary>
        /// Classifies the input. The returned spans must be ordered and must not overlap.
        /// </summary>
        IReadOnlyList<ClassifiedSpan> Classify(string text);
    }
}
