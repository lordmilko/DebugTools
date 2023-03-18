using System;
using System.Diagnostics;
using System.Text;
using ClrDebug;
using DebugTools.Profiler;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public sealed class StaticFieldValueArgs : TraceEvent
    {
        public HRESULT HRESULT => (HRESULT)GetInt32At(0);

        internal PROFILER_HRESULT? PROFILER_HRESULT
        {
            get
            {
                var hr = HRESULT;

                if (hr.IsProfilerHRESULT())
                    return (PROFILER_HRESULT)hr;

                return null;
            }
        }

        public int ValueLength => GetInt32At(4);

        public byte[] Value => GetByteArrayAt(8, ValueLength);

        private Action<StaticFieldValueArgs> action;

        internal StaticFieldValueArgs(Action<StaticFieldValueArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<StaticFieldValueArgs>)value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(HRESULT), nameof(ValueLength), nameof(Value) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
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
            XmlAttrib(sb, nameof(HRESULT), HRESULT);
            XmlAttrib(sb, nameof(ValueLength), ValueLength);
            XmlAttrib(sb, nameof(Value), Value);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
