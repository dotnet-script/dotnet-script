using System;
using McMaster.Extensions.CommandLineUtils;

namespace Dotnet.Script
{
    static class Extensions
    {
        public static bool ValueEquals(this CommandOption option, string value, StringComparison comparison) =>
            option.HasValue() && string.Equals(option.Value(), value, comparison);
    }
}