using System;
using System.IO;
using Dotnet.Script.Core.Interactive.LineEditing;
using Microsoft.CodeAnalysis;

namespace Dotnet.Script.Core
{
    public class ScriptConsole
    {
        // Lazy to avoid touching anything during type initialization
        private static readonly Lazy<ScriptConsole> s_default =
            new Lazy<ScriptConsole>(() => new ScriptConsole(Console.Out, Console.In, Console.Error));

        private readonly LineEditor _lineEditor;

        public static ScriptConsole Default => s_default.Value;

        public virtual TextWriter Error { get; }
        public virtual TextWriter Out { get; }
        public virtual TextReader In { get; }

        public virtual void Clear() => Console.Clear();

        public virtual void WriteError(string value)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Error.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
            Console.ResetColor();
        }

        public virtual void WriteSuccess(string value)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
            Console.ResetColor();
        }

        public virtual void WriteHighlighted(string value)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
            Console.ResetColor();
        }

        public virtual void WriteWarning(string value)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Error.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
            Console.ResetColor();
        }

        public virtual void WriteNormal(string value)
        {
            Out.WriteLine(value.TrimEnd(Environment.NewLine.ToCharArray()));
        }

        public virtual void WriteDiagnostics(Diagnostic[] warningDiagnostics, Diagnostic[] errorDiagnostics)
        {
            if (warningDiagnostics != null)
            {
                foreach (var warning in warningDiagnostics)
                {
                    WriteWarning(warning.ToString());
                }
            }

            if (errorDiagnostics != null)
            {
                foreach (var error in errorDiagnostics)
                {
                    WriteError(error.ToString());
                }
            }
        }

        public virtual string ReadLine() => In?.ReadLine();

        /// <summary>
        /// The interactive line editor, or <c>null</c> when this console is not attached to a terminal.
        /// </summary>
        public virtual LineEditor LineEditor => _lineEditor;

        public ScriptConsole(TextWriter output, TextReader input, TextWriter error)
        {
            Out = output;
            Error = error;
            In = input;
            _lineEditor = TryCreateLineEditor(input);
        }

        /// <summary>
        /// The line editor is only used when reading from a real, non redirected terminal.
        /// Piped input and in-memory readers keep using plain line reads.
        /// </summary>
        private static LineEditor TryCreateLineEditor(TextReader input)
        {
            try
            {
                if (input == null || !ReferenceEquals(input, Console.In))
                {
                    return null;
                }

                if (Console.IsInputRedirected || Console.IsOutputRedirected)
                {
                    return null;
                }

                return new LineEditor();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
