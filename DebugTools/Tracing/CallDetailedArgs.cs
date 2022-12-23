using System;
using System.Diagnostics;
using System.Text;
using ClrDebug;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    /// <summary>
    /// Describes the CallDetailedArgs template defined in DebugToolsProfiler.man
    /// </summary>
    public sealed class CallDetailedArgs : TraceEvent, ICallArgs
    {
        public long FunctionID => GetInt64At(0);

        public HRESULT HRESULT => (HRESULT)GetInt32At(8);

        public int ValueLength => GetInt32At(12);

        public byte[] Value => GetByteArrayAt(16, ValueLength);

        private Action<CallDetailedArgs> action;

        internal CallDetailedArgs(Action<CallDetailedArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<CallDetailedArgs>)value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(FunctionID), nameof(HRESULT), nameof(ValueLength), nameof(Value) };

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
                    return HRESULT;

                case 2:
                    return ValueLength;

                case 3:
                    return Value;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(FunctionID), FunctionID);
            XmlAttrib(sb, nameof(HRESULT), HRESULT);
            XmlAttrib(sb, nameof(ValueLength), ValueLength);
            XmlAttrib(sb, nameof(Value), Value);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
