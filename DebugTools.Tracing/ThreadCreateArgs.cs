using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    /// <summary>
    /// Describes the ThreadArgs template defined in DebugToolsProfiler.man
    /// </summary>
    public sealed class ThreadArgs : TraceEvent
    {
        public long ThreadId => GetInt64At(0);

        private Action<ThreadArgs> action;

        internal ThreadArgs(Action<ThreadArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<ThreadArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(ThreadID) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ThreadID;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(ThreadID), ThreadID);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}