using System.Collections.Generic;

namespace DebugTools.PowerShell
{
    public class FrameEqualityComparer : IEqualityComparer<IFrame>
    {
        public static readonly FrameEqualityComparer Instance = new FrameEqualityComparer();

        public bool Equals(IFrame x, IFrame y)
        {
            if (x is MethodFrame m1 && y is MethodFrame m2)
                return m1.MethodInfo.Equals(m2.MethodInfo);

            return Equals(x, y);
        }

        public int GetHashCode(IFrame obj)
        {
            if (obj is MethodFrame m)
                return m.MethodInfo.GetHashCode();

            return obj.GetHashCode();
        }
    }
}