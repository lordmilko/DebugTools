using System;
using System.Collections.Generic;

namespace DebugTools
{
    static class EnumerableEx
    {
        internal static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var set = new HashSet<TKey>(comparer);

            foreach (var item in source)
            {
                if (set.Add(keySelector(item)))
                    yield return item;
            }
        }
    }
}
