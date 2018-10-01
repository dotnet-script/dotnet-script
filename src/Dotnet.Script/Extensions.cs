using System;

namespace Dotnet.Script
{
    static class Extensions
    {
        public static string ToHexadecimalString(this byte[] bytes) =>
            BitConverter.ToString(bytes)
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();
    }
}