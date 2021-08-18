using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Dotnet.Script.DependencyModel.Internal
{
    /// <summary>
    /// <para>
    /// Performs operations on <see cref="System.String"/> instances that contain command line information.
    /// </para>
    /// <para>
    /// Tip: a ready-to-use package with this functionality is available at https://www.nuget.org/packages/Gapotchenko.FX.Diagnostics.CommandLine.
    /// </para>
    /// </summary>
    /// <summary>
    /// Available
    /// </summary>
    static class CommandLine
    {
        /// <summary>
        /// Escapes and optionally quotes a command line argument.
        /// </summary>
        /// <param name="value">The command line argument.</param>
        /// <returns>The escaped and optionally quoted command line argument.</returns>
        public static string EscapeArgument(string value)
        {
            if (value == null)
                return null;

            int length = value.Length;
            if (length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            Escape.AppendQuotedText(sb, value);

            if (sb.Length == length)
                return value;

            return sb.ToString();
        }

        static class Escape
        {
            public static void AppendQuotedText(StringBuilder sb, string text)
            {
                bool quotingRequired = IsQuotingRequired(text);
                if (quotingRequired)
                    sb.Append('"');

                int numberOfQuotes = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '"')
                        numberOfQuotes++;
                }

                if (numberOfQuotes > 0)
                {
                    if ((numberOfQuotes % 2) != 0)
                        throw new Exception("Command line parameter cannot contain an odd number of double quotes.");
                    text = text.Replace("\\\"", "\\\\\"").Replace("\"", "\\\"");
                }

                sb.Append(text);

                if (quotingRequired && text.EndsWith("\\"))
                    sb.Append('\\');

                if (quotingRequired)
                    sb.Append('"');
            }

            static bool IsQuotingRequired(string parameter) =>
                !AllowedUnquotedRegex.IsMatch(parameter) ||
                DefinitelyNeedQuotesRegex.IsMatch(parameter);

            static Regex m_CachedAllowedUnquotedRegex;

            static Regex AllowedUnquotedRegex =>
                m_CachedAllowedUnquotedRegex ??= new Regex(
                    @"^[a-z\\/:0-9\._\-+=]*$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            static Regex m_CachedDefinitelyNeedQuotesRegex;

            static Regex DefinitelyNeedQuotesRegex =>
                m_CachedDefinitelyNeedQuotesRegex ??= new Regex(
                    "[|><\\s,;\"]+",
                    RegexOptions.CultureInvariant);
        }
    }
}
