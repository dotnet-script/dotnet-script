using System.Runtime.Loader;

var context = AssemblyLoadContext.CurrentContextualReflectionContext;
Console.WriteLine(context?.ToString() ?? "<null>");
