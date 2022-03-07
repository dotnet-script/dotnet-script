using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script.Core
{
    public static class Extensions
    {
        public static string GetRootedPath(this string path) => Path.IsPathRooted(path) ? path : Path.Combine(Directory.GetCurrentDirectory(), path);

        public static SourceText ToSourceText(this string absoluteFilePath)
        {
            using var filestream = File.OpenRead(absoluteFilePath);
            return SourceText.From(filestream);
        }
    }
}