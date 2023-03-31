using System.Collections.Generic;

namespace DebugTools.Profiler
{
    class MethodInfoComparer : IEqualityComparer<IMethodInfo>
    {
        public static readonly MethodInfoComparer Instance = new MethodInfoComparer();

        public bool Equals(IMethodInfo x, IMethodInfo y)
        {
            return x.FunctionID == y.FunctionID;
        }

        public int GetHashCode(IMethodInfo obj)
        {
            return obj.FunctionID.GetHashCode();
        }
    }
}