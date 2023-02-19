using System;
using System.Runtime.Serialization;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSException : Exception
    {
        public SOSException()
        {
        }

        public SOSException(string message) : base(message)
        {
        }

        public SOSException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SOSException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
