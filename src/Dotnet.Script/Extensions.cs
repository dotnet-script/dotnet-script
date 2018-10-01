using System;
using McMaster.Extensions.CommandLineUtils;

namespace Dotnet.Script
{
    using System.Collections.Generic;

    static class Extensions
    {
        public static bool ValueEquals(this CommandOption option, Func<string, bool> predicate)
        {
            if (option == null) throw new ArgumentNullException(nameof(option));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return option.HasValue() && predicate(option.Value());
        }

        public static Func<T, bool> PredicateEquals<T>(this IEqualityComparer<T> comparer, T first)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            return second => comparer.Equals(first);
        }
    }
}