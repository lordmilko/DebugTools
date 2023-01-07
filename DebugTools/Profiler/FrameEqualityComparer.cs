using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class FrameEqualityComparer : IEqualityComparer<IFrame>
    {
        public static readonly FrameEqualityComparer Instance = new FrameEqualityComparer();

        public bool Equals(IFrame x, IFrame y)
        {
            if (x is IMethodFrame m1 && y is IMethodFrame m2)
                return m1.MethodInfo.Equals(m2.MethodInfo);

            return ReferenceEquals(x, y);
        }

        public int GetHashCode(IFrame obj)
        {
            if (obj is IMethodFrame m)
                return m.MethodInfo.GetHashCode();

            return obj.GetHashCode();
        }
    }
}
