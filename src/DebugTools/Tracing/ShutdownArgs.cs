using System;
using System.Diagnostics;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public sealed class ShutdownArgs : TraceEvent
    {
        private Action<ShutdownArgs> action;

        internal ShutdownArgs(Action<ShutdownArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<ShutdownArgs>)value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[0];

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        protected override void Dispatch() => action(this);
    }
}