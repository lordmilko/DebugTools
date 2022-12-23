using System;

namespace DebugTools.Tracing
{
    interface ICallArgs
    {
        DateTime TimeStamp { get; }

        int ThreadID { get; }

        long FunctionID { get; }
    }
}