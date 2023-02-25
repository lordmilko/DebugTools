using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public class ModuleLoadedArgs : TraceEvent
    {
        public int UniqueModuleID => GetInt32At(0);

        public string Path => GetUnicodeStringAt(4);

        private Action<ModuleLoadedArgs> action;

        internal ModuleLoadedArgs(Action<ModuleLoadedArgs> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName) :
            base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.action = action;
        }

        protected override Delegate Target
        {
            get => action;
            set => action = (Action<ModuleLoadedArgs>) value;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new[] { nameof(UniqueModuleID), nameof(Path) };

                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return UniqueModuleID;

                case 1:
                    return Path;

                default:
                    Debug.Assert(false, $"Unknown payload field '{index}'");
                    return null;
            }
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            XmlAttrib(sb, nameof(UniqueModuleID), UniqueModuleID);
            XmlAttrib(sb, nameof(Path), Path);
            sb.Append("/>");
            return sb;
        }

        protected override void Dispatch() => action(this);
    }
}
