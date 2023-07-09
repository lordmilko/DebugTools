using System;
using System.Runtime.Serialization;

namespace DebugTools.Dynamic
{
    /// <summary>
    /// The exception that is thrown when a non-serializable exception occurs in a remote AppDomain.
    /// </summary>
    [Serializable]
    public class RemoteException : Exception
    {
        /// <summary>
        /// Gets the type of the original remote exception.
        /// </summary>
        public Type Type { get; }

        public RemoteException(string message, Type type) : base(message)
        {
            Type = type;
        }

        protected RemoteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
