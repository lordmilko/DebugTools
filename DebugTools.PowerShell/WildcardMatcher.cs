using System;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    class WildcardMatcher
    {
        public MatchCollection Execute(string[] strs)
        {
            MatchCollection collection = new MatchCollection();

            foreach (var str in strs)
            {
                var trimmedStr = str.Trim();

                var sanitized = trimmedStr.Trim('*');

                if (sanitized.Contains("*"))
                    throw new InvalidOperationException($"Cannot process wildcard '{str}': cannot use a wildcard that has a * in the middle of a string.");

                if (sanitized == string.Empty)
                {
                    collection.Add(TransformAll(sanitized));
                    continue;
                }

                if (trimmedStr.StartsWith("*"))
                {
                    if (trimmedStr.EndsWith("*"))
                    {
                        //contains
                        collection.Add(TransformContains(sanitized));
                    }
                    else
                    {
                        //ends with
                        collection.Add(TransformEndsWith(sanitized));
                    }
                }
                else
                {
                    if (trimmedStr.EndsWith("*"))
                    {
                        //starts with
                        collection.Add(TransformStartsWith(sanitized));
                    }
                    else
                    {
                        //literal
                        collection.Add(TransformLiteral(sanitized));
                    }
                }
            }

            return collection;
        }

        protected virtual (MatchKind kind, string value) TransformAll(string str) => (MatchKind.All, str);

        protected virtual (MatchKind kind, string value) TransformContains(string str) => (MatchKind.Contains, str);

        protected virtual (MatchKind kind, string value) TransformStartsWith(string str) => (MatchKind.StartsWith, str);

        protected virtual (MatchKind kind, string value) TransformEndsWith(string str) => (MatchKind.EndsWith, str);

        protected virtual (MatchKind kind, string value) TransformLiteral(string str) => (MatchKind.Literal, str);
    }
}
