#load "other.csx"
#r "nuget: AutoMapper, 6.1.0"
#r "nuget: Microsoft.CodeAnalysis.Scripting, 2.8.2"

using AutoMapper;

// throw new Exception("something bad here...");

foreach (var arg in Args)
{
    Console.WriteLine(arg);
}

System.Console.WriteLine(typeof(Mapper));
System.Console.WriteLine("hello world");

new OtherClass().OtherMethod();
