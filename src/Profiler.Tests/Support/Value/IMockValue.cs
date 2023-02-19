using System.IO;
using ClrDebug;

namespace Profiler.Tests
{
    interface IMockValue<out T> : IMockValue
    {
        new T RawValue { get; }
    }

    interface IMockValue
    {
        CorElementType ElementType { get; }

        Stream Stream { get; }

        object OuterValue { get; }

        object RawValue { get; }
    }
}
