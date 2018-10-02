using System;
using System.IO;

namespace Dotnet.Script
{
    static class Extensions
    {
        public static bool CreateIfMissing(this DirectoryInfo directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (directory.Exists)
                return false;
            directory.Create();
            return true;
        }
    }
}