using System.IO;
using System.Reflection;

namespace Dotnet.Script.Core.Templates
{
    public static class TemplateLoader
    {
        public static string ReadTemplate(string name)
        {
            var resourceStream = typeof(TemplateLoader).GetTypeInfo().Assembly.GetManifestResourceStream($"Dotnet.Script.Core.Templates.{name}");
            using (var streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}