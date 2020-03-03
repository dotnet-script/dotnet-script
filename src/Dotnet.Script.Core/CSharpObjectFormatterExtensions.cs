using System;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;

namespace Dotnet.Script.Core
{
    internal static class CSharpObjectFormatterExtensions 
    {
        internal static string ToDisplayString(this CSharpObjectFormatter csharpObjectFormatter, Exception ex) 
        {
            var builder = new StringBuilder();
            builder.Append(ex.GetType());
            builder.Append(": ");
            builder.Append(csharpObjectFormatter.FormatException(ex));
            return builder.ToString();
        }
    }
}