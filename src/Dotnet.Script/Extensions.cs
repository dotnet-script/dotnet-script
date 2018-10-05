using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace Dotnet.Script
{
    static class Extensions
    {
        public static bool ValueEquals(this CommandOption option, string value, StringComparison comparison)
        {
            if (option == null) throw new ArgumentNullException(nameof(option));
            return option.HasValue() && string.Equals(option.Value(), value, comparison);
        }

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