using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    /// <summary>
    /// Describes the CallArgs template defined in DebugToolsProfiler.man
    /// </summary>
    public sealed class CallArgs : TraceEvent
    {
        public long FunctionID => GetInt64At(0);

        private Action<CallArgs> action;

        internal CallArgs(Action<CallArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<CallArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(FunctionID) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return FunctionID;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(FunctionID), FunctionID);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}