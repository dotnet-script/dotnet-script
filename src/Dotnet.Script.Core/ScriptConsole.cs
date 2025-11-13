using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using RL = System.ReadLine;

namespace Dotnet.Script.Core
{
    public class ScriptConsole
    {
        // Lazy to avoid touching anything during type initialization
        private static readonly Lazy<ScriptConsole> s_default =
            new Lazy<ScriptConsole>(() => new ScriptConsole(Console.Out, Console.In, Console.Error));

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

        public virtual string ReadLine()
        {
            if (In != null)
                return In.ReadLine();

            return ReadLineInteractive();
        }

        public ScriptConsole(TextWriter output, TextReader input, TextWriter error)
        {
            if (input == null)
            {
                TryEnableReadLineHistory();
            }

            Out = output;
            Error = error;
            In = input;
        }

        // Isolate the ReadLine reference so JIT does not resolve it unless called.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ReadLineInteractive()
        {
            try
            {
                return RL.Read();
            }
            catch (System.IO.FileLoadException)
            {
                // ReadLine is not strongly named or not resolvable on netfx test hosts; fallback to Console.ReadLine.
                return Console.ReadLine();
            }
            catch (TypeInitializationException tie) when (tie.InnerException is System.IO.FileLoadException)
            {
                return Console.ReadLine();
            }
        }

        // Isolate the ReadLine reference so JIT does not resolve it unless called.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TryEnableReadLineHistory()
        {
            try
            {
                RL.HistoryEnabled = true;
            }
            catch (System.IO.FileLoadException)
            {
                // netfx may require a strong-named dependency chain; ignore for tests.
            }
            catch (TypeInitializationException tie) when (tie.InnerException is System.IO.FileLoadException)
            {
                // Same case wrapped by a type initializer; ignore.
            }
        }
    }
}
