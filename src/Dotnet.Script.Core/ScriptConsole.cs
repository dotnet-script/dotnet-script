using System;
using System.IO;

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

        public ScriptConsole(TextWriter output, TextReader input, TextWriter error)
        {
            Out = output;
            Error = error;
            In = input;
        }
    }
}
