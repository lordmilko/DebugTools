using System;
using System.Diagnostics;
using System.Text;
using ClrDebug;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public sealed class MethodInfoDetailedArgs : TraceEvent
    {
        public long FunctionID => GetInt64At(0);

        public string MethodName => GetUnicodeStringAt(8);

        public string TypeName => GetUnicodeStringAt(SkipUnicodeString(8, 1));

        public string ModuleName => GetUnicodeStringAt(SkipUnicodeString(8, 2));

        public mdMethodDef Token => GetInt32At(SkipUnicodeString(8, 3));

        public int SigBlobLength => GetInt32At(SkipUnicodeString(8, 3) + sizeof(int));

        public byte[] SigBlob => GetByteArrayAt(SkipUnicodeString(8, 3) + sizeof(int) + sizeof(int), SigBlobLength);

        private Action<MethodInfoDetailedArgs> action;

        internal MethodInfoDetailedArgs(Action<MethodInfoDetailedArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<MethodInfoDetailedArgs>)value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(FunctionID), nameof(MethodName), nameof(TypeName), nameof(ModuleName), nameof(Token), nameof(SigBlobLength), nameof(SigBlob) };

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

                case 4:
                    return Token;

                case 5:
                    return SigBlobLength;

                case 6:
                    return SigBlob;

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
            XmlAttrib(sb, nameof(Token), Token);
            XmlAttrib(sb, nameof(SigBlobLength), SigBlobLength);
            XmlAttrib(sb, nameof(SigBlob), SigBlob);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}