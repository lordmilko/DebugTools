using System;
using ClrDebug;

namespace DebugTools.Tracing
{
    interface ICallArgs
    {
        DateTime TimeStamp { get; }

        int ThreadID { get; }

        long FunctionID { get; }

        long Sequence { get; }

        HRESULT HRESULT { get; }
    }
}
