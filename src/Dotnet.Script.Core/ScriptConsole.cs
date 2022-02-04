using System;
using System.IO;
using Microsoft.CodeAnalysis;
using RL = System.ReadLine;

namespace Dotnet.Script.Core
{
    public class ScriptConsole
    {
        public static readonly ScriptConsole Default = new ScriptConsole(Console.Out, Console.In, Console.Error);

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
                    WriteHighlighted(warning.ToString());
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
            return In == null ? RL.Read() : In.ReadLine();
        }

        public ScriptConsole(TextWriter output, TextReader input, TextWriter error)
        {
            if (input == null)
            {
                RL.HistoryEnabled = true;
            }

            Out = output;
            Error = error;
            In = input;
        }
    }
}
