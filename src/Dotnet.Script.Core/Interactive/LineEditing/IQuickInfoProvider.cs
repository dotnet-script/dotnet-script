using System.Collections.Generic;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    public interface IQuickInfoProvider
    {
        /// <summary>
        /// Describes the symbol at the caret, one entry per overload, or an empty list when there is
        /// nothing to show.
        /// </summary>
        IReadOnlyList<string> GetQuickInfo(string text, int caret);
    }
}
