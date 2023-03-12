using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public sealed class ThreadNameArgs : TraceEvent
    {
        public int ThreadSequence => GetInt32At(0);

        public string ThreadName => GetUnicodeStringAt(4);

        private Action<ThreadNameArgs> action;

        internal ThreadNameArgs(Action<ThreadNameArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<ThreadNameArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(ThreadSequence), nameof(ThreadName) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ThreadSequence;

                case 1:
                    return ThreadName;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(ThreadSequence), ThreadSequence);
            XmlAttrib(sb, nameof(ThreadName), ThreadName);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
