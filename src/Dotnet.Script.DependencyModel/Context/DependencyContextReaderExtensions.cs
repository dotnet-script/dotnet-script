using System.IO;
using Microsoft.Extensions.DependencyModel;

namespace Dotnet.Script.DependencyModel.Context
{
    public static class DependencyContextReaderExtensions
    {
        public static DependencyContext Read(this DependencyContextJsonReader reader, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // https://github.com/dotnet/core-setup/blob/master/src/managed/Microsoft.Extensions.DependencyModel/DependencyContextJsonReader.cs
                using (var contextReader = new DependencyContextJsonReader())
                {
                    return contextReader.Read(fs);
                }
            }
        }
    }
}