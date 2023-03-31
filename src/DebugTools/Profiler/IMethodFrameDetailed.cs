using System.Collections.Generic;

namespace DebugTools.Profiler
{
    internal interface IMethodFrameDetailedInternal : IMethodFrameDetailed
    {
        byte[] EnterValue { get; set; }

        byte[] ExitValue { get; set; }
    }

    public interface IMethodFrameDetailed : IMethodFrame
    {
        List<object> GetEnterParameters();

        object GetExitResult();
    }
}
