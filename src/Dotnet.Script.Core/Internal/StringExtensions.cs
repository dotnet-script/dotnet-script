using System;

public static class StringExtensions
{
    public static bool Contains(this string source, string value, StringComparison comparison)
    {
        return source?.IndexOf(value, comparison) >= 0;
    }
}