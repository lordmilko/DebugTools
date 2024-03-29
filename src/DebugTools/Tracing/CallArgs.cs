﻿using System;
using System.Diagnostics;
using System.Text;
using ClrDebug;
using DebugTools.Profiler;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    /// <summary>
    /// Describes the CallArgs template defined in DebugToolsProfiler.man
    /// </summary>
    public sealed class CallArgs : TraceEvent, ICallArgs
    {
        public long FunctionID => GetInt64At(0);

        public long Sequence => GetInt64At(8);

        public HRESULT HRESULT => (HRESULT) GetInt32At(16);

        /// <summary>
        /// Gets the kind of frame that was unwound. This field is only valid in unwind events.
        /// </summary>
        public FrameKind UnwindFrameKind => (FrameKind) GetInt32At(16);

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
                    payloadNames = new[] { nameof(FunctionID), nameof(Sequence), nameof(HRESULT) };

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
                    return HRESULT;

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
            XmlAttrib(sb, nameof(HRESULT), HRESULT);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
