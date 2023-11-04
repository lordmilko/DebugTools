using System;

namespace DebugTools.Host
{
    public interface IRemotingMarshaller
    {
        T Marshal<T>(T value) where T : MarshalByRefObject;
    }
}
