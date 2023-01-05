using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public class ExceptionArgs : TraceEvent
    {
        public long Sequence => GetInt64At(0);

        public string Type => GetUnicodeStringAt(8);

        private Action<ExceptionArgs> action;

        internal ExceptionArgs(Action<ExceptionArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<ExceptionArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(Sequence), nameof(Type) };

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
                    return Type;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(Sequence), Sequence);
            XmlAttrib(sb, nameof(Type), Type);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
