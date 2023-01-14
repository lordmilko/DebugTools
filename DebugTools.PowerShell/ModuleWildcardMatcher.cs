using System;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    class ModuleWildcardMatcher : WildcardMatcher
    {
        protected override (MatchKind kind, string value) TransformStartsWith(string str)
        {
            throw new InvalidOperationException($"Cannot use StartsWith wildcard expression '{str}': module expressions must either be a filename literal \"Foo.dll\" or a path contains \"*some\\path*\".");
        }

        protected override (MatchKind kind, string value) TransformEndsWith(string str)
        {
            throw new InvalidOperationException($"Cannot use EndsWith wildcard expression '{str}': module expressions must either be a filename literal \"Foo.dll\" or a path contains \"*some\\path*\".");
        }

        protected override (MatchKind kind, string value) TransformLiteral(string str)
        {
            if (str.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || str.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return (MatchKind.ModuleName, str);

            if (str.Contains("\\") || str.Contains("//"))
                throw new InvalidOperationException($"Cannot process module literal '{str}': to filter on a file path, use a contains wildcard expression (\"*{str}*\")");

            return (MatchKind.ModuleName, str + ".dll");
        }
    }
}
