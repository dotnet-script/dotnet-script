using System;
using System.IO;

namespace Dotnet.Script.Core
{
    public class ScriptConsole
    {
        public static readonly ScriptConsole Default = new ScriptConsole(Console.Out, Console.Error, Console.In);

        public virtual TextWriter Error { get; }
        public virtual TextWriter Out { get; }
        public virtual TextReader In { get; }

        public virtual void Clear() => Console.Clear();

        public ScriptConsole(TextWriter output, TextWriter error, TextReader input)
        {
            Out = output;
            Error = error;
            In = input;
        }
    }
}
