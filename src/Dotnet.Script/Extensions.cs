using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis.Text;

namespace Dotnet.Script
{
    static class Extensions
    {
        public static bool ValueEquals(this CommandOption option, string value, StringComparison comparison)
        {
            if (option == null) throw new ArgumentNullException(nameof(option));
            return option.HasValue() && string.Equals(option.Value(), value, comparison);
        }
    }
}