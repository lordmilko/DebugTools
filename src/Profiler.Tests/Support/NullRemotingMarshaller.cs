using System;
using DebugTools.Host;

namespace Profiler.Tests
{
    class NullRemotingMarshaller : IRemotingMarshaller
    {
        public static readonly NullRemotingMarshaller Instance = new NullRemotingMarshaller();

        public T Marshal<T>(T value) where T : MarshalByRefObject
        {
            return value;
        }
    }
}
