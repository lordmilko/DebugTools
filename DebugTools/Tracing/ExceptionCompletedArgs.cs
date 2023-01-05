using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public class ExceptionCompletedArgs : TraceEvent
    {
        public long Sequence => GetInt64At(0);

        public ExceptionStatus Reason => (ExceptionStatus) GetInt32At(8);

        private Action<ExceptionCompletedArgs> action;

        internal ExceptionCompletedArgs(Action<ExceptionCompletedArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<ExceptionCompletedArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(Sequence), nameof(Reason) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Sequence;

                case 1:
                    return Reason;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(Sequence), Sequence);
            XmlAttrib(sb, nameof(Reason), Reason);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
