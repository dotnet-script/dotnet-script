using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Dotnet.Script.Extras.Demos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var code = File.ReadAllText("test.csx");

            var resolver = new PaketScriptMetadataResolver(code);
            var opts = resolver.CreateScriptOptions(ScriptOptions.Default);

            CSharpScript.RunAsync(code, opts).GetAwaiter().GetResult();
        }
    }
}
