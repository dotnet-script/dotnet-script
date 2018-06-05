using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Threading.Tasks;
using static System.Console;

namespace dotnetPublishCode
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var globals = new CommandLineScriptGlobals(Console.Out, CSharpObjectFormatter.Instance);
                foreach (var arg in args)
                    globals.Args.Add(arg);

                var factoryMethod = typeof(scriptAssembly).GetMethod("<Factory>");
                if (factoryMethod == null) throw new Exception("couldn't find factory method to initiate script");

                var invokeTask = factoryMethod.Invoke(null, new object[] { new object[] { globals, null } }) as Task<int>;
                var invokeResult = invokeTask.Result;
                if (invokeResult != 0) WriteLine(invokeResult);
            }
            //todo: not getting the full script stack trace
            //todo: colorize errors
            catch (AggregateException ex)
            {
                foreach (var exception in ex.InnerExceptions ?? Enumerable.Empty<Exception>())
                {
                    WriteLine(exception);
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex);
            }
        }
    }
}