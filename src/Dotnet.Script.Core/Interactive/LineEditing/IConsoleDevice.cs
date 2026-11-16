using System;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// The subset of console capabilities required by <see cref="LineEditor"/>.
    /// Exists so that the editor can be driven by a virtual console in tests.
    /// </summary>
    public interface IConsoleDevice
    {
        int Width { get; }

        int Height { get; }

        int CursorLeft { get; }

        int CursorTop { get; }

        bool CursorVisible { set; }

        ConsoleColor ForegroundColor { get; set; }

        bool TreatControlCAsInput { get; set; }

        bool SupportsColors { get; }

        /// <summary>True when input is already buffered, which is how a paste is detected.</summary>
        bool KeyAvailable { get; }

        ConsoleKeyInfo ReadKey();

        void Write(string value);

        void SetCursorPosition(int left, int top);

        void ResetColor();

        void Clear();
    }
}
