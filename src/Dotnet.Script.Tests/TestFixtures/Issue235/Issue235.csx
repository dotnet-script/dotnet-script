#! "netcoreapp2.0"
#r "nuget: AgileObjects.AgileMapper, 0.23.1"
#r "TestClass.dll"

using System.Reflection;
using System.Runtime.CompilerServices;
using AgileObjects.AgileMapper;
static string GetScriptPath([CallerFilePath] string path = null) => path;
IMapper mapper = Mapper.CreateNew();
//TODO: Temporary workaround until I figure out how to change TestClass Submission#0 class name
var testClassAssembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(GetScriptPath()),"TestClass.dll"));
var testClass = testClassAssembly.GetType("Submission#0").GetNestedType("TestClass");
var instance = Activator.CreateInstance(testClass);
Console.WriteLine("Hello World!");