using System;
using System.IO;

namespace Dotnet.Script.Core
{
    public class ScriptConsole
    {
        public static readonly ScriptConsole Default = new ScriptConsole(Console.Out, Console.Error, Console.In);

        public TextWriter Error { get; }
        public TextWriter Out { get; }
        public TextReader In { get; }

        public ScriptConsole(TextWriter output, TextWriter error, TextReader input)
        {
            Out = output;
            Error = error;
            In = input;
        }
    }
}
