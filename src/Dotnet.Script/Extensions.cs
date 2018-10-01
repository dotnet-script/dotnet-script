namespace Dotnet.Script
{
    using System;
    using McMaster.Extensions.CommandLineUtils;

    static class Extensions
    {
        public static bool ValueEquals(this CommandOption option, string value, StringComparison comparison) =>
            option.HasValue() && string.Equals(option.Value(), value, comparison);
    }
}