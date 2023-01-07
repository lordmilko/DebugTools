using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public interface IFrame
    {
        IFrame Parent { get; set; }

        List<IMethodFrame> Children { get; set; }

        long Sequence { get; }

        IRootFrame GetRoot();
    }
}
