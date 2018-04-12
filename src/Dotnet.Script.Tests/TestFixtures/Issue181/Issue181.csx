#r "nuget: Microsoft.CodeAnalysis.Scripting, 2.4.0"

List<string> list = new List<string>(new[] { "42" });
Write(list.FirstOrDefault());

