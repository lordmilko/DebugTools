using System;
using System.Diagnostics;
using System.Text;
using ClrDebug;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public class UnmanagedTransitionArgs : TraceEvent, ICallArgs
    {
        public long FunctionID => GetInt64At(0);

        public long Sequence => GetInt64At(8);

        public COR_PRF_TRANSITION_REASON Reason => (COR_PRF_TRANSITION_REASON) GetInt32At(16);

        public HRESULT HRESULT => HRESULT.S_OK;

        private Action<UnmanagedTransitionArgs> action;

        internal UnmanagedTransitionArgs(Action<UnmanagedTransitionArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<UnmanagedTransitionArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(FunctionID), nameof(Sequence), nameof(Reason) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return FunctionID;

                case 1:
                    return Sequence;

                case 2:
                    return Reason;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(FunctionID), FunctionID);
            XmlAttrib(sb, nameof(Sequence), Sequence);
            XmlAttrib(sb, nameof(Reason), Reason);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
