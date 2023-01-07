using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public interface IMethodFrameDetailed : IMethodFrame
    {
        List<object> GetEnterParameters();

        object GetExitResult();
    }
}
