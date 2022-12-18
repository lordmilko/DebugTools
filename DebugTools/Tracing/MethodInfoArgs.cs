using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    /// <summary>
    /// Describes the MethodInfoArgs template defined in DebugToolsProfiler.man
    /// </summary>
    public sealed class MethodInfoArgs : TraceEvent
    {
        public long FunctionID => GetInt64At(0);

        public string MethodName => GetUnicodeStringAt(8);

        public string TypeName => GetUnicodeStringAt(SkipUnicodeString(8, 1));

        public string ModuleName => GetUnicodeStringAt(SkipUnicodeString(8, 2));

        private Action<MethodInfoArgs> action;

        internal MethodInfoArgs(Action<MethodInfoArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<MethodInfoArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(FunctionID), nameof(MethodName), nameof(TypeName), nameof(ModuleName) };

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
                    return MethodName;

                case 2:
                    return TypeName;

                case 3:
                    return ModuleName;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(FunctionID), FunctionID);
            XmlAttrib(sb, nameof(MethodName), MethodName);
            XmlAttrib(sb, nameof(TypeName), TypeName);
            XmlAttrib(sb, nameof(ModuleName), ModuleName);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}