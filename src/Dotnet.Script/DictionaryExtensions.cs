using System;
using System.Collections;
using System.Collections.Generic;

namespace Dotnet.Script
{
    static class DictionaryExtensions
    {
        public static IEnumerable<DictionaryEntry> GetEntries(this IDictionary dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            return GetEntriesIterator(dictionary);
        }

        static IEnumerable<DictionaryEntry> GetEntriesIterator(IDictionary dictionary)
        {
            var e = dictionary.GetEnumerator();
            while (e.MoveNext())
                yield return e.Entry;
        }
    }
}