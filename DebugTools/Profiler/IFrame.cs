using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public interface IFrame
    {
        IFrame Parent { get; set; }

        List<MethodFrame> Children { get; set; }

        RootFrame GetRoot();
    }
}