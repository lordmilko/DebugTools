using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell
{
    internal static class PSEnumerableEx
    {
        /// <summary>
        /// Filters an enumeration of items by an array of wildcard expressions.
        /// </summary>
        /// <typeparam name="TSource">The type of element to filter against.</typeparam>
        /// <param name="source">The enumeration of elements to filter.</param>
        /// <param name="selector">A lambda that retrieves the value to filter against for each <typeparamref name="TSource"/>.</param>
        /// <param name="arr">The list of wildcard values to filter <paramref name="source"/> by.</param>
        /// <returns>A new enumeration containing only the elements that matched the specified filter.</returns>
        internal static IEnumerable<TSource> FilterBy<TSource>(this IEnumerable<TSource> source, Func<TSource, string> selector, params string[] arr) =>
            source.Filter((f, i) => f.IsMatch(selector(i)), arr);

        /// <summary>
        /// Filters an enumeration of items by a custom predicate that inspects the <typeparamref name="TSource"/> one or more times to determine whether the wildcard pattern is a match.
        /// </summary>
        /// <typeparam name="TSource">The type of element to filter against.</typeparam>
        /// <param name="source">The enumeration of elements to filter.</param>
        /// <param name="predicate">The predicate to use to filter each element.</param>
        /// <param name="arr">The list of wildcard values to filter <paramref name="source"/> by.</param>
        /// <returns>A new enumeration containing only the elements that matched the specified filter.</returns>
        internal static IEnumerable<TSource> Filter<TSource>(this IEnumerable<TSource> source, Func<WildcardPattern, TSource, bool> predicate, params string[] arr)
        {
            var matches = source.Where(
                item => arr
                    .Select(a => new WildcardPattern(CleanPattern(a), WildcardOptions.IgnoreCase))
                    .Any(filter => predicate(filter, item))
            );

            return matches;
        }

        internal static string CleanPattern(this string pattern)
        {
            return pattern.Replace("[", "`[").Replace("]", "`]").Replace("?", "`?");
        }
    }
}
