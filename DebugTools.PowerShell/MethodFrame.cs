using System.Collections.Generic;

namespace DebugTools.PowerShell
{
    class MethodFrame : IFrame
    {
        public IFrame Parent { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public List<IFrame> Children { get; set; } = new List<IFrame>();

        public int HashCode => GetHashCode();

        public RootFrame GetRoot()
        {
            var parent = Parent;

            while (true)
            {
                if (parent is RootFrame)
                    return (RootFrame)parent;

                parent = parent.Parent;
            }
        }

        public override string ToString()
        {
            return $"{MethodInfo.TypeName}.{MethodInfo.MethodName}";
        }
    }
}