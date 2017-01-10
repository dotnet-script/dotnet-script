using System;
using System.Collections.Generic;
using System.Linq;

namespace Dotnet.Script.Core
{
    static class EnumerableExtensions
    {
        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            var comparer = Comparer<T>.Create(compare);
            return source.OrderBy(t => t, comparer);
        }
    }
}