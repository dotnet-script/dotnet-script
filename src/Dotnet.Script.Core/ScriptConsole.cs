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

        public virtual void WritePrettyError(string value)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Error.WriteLine(value.TrimEnd(System.Environment.NewLine.ToCharArray()));
            Console.ResetColor();
        }

        public ScriptConsole(TextWriter output, TextReader input, TextWriter error)
        {
            Out = output;
            Error = error;
            In = input;
        }
    }
}
