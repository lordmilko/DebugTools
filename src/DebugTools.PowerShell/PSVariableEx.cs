using System;
using System.Management.Automation;

namespace DebugTools.PowerShell
{
    class PSVariableEx : PSVariable
    {
        private readonly Func<object> getValue;
        private readonly Action<object> setValue;

        public PSVariableEx(string name, object initial, Func<object> getValue, Action<object> setValue) : base(name, initial)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }

        public override object Value
        {
            get
            {
                var value = getValue();

                /* Prior to emitting any new object that might be an IDynamicMetaObjectProvider to the pipeline,
                 * ensure we've previously wrapped it as a PSObject with PSVariableProperty properties for each of its
                 * GetDynamicMemberNames(). PowerShell will separately cache the members of ourobject in the
                 * PSObject._instanceMembersResurrectionTable, thus allowing us to restore our PSVariableProperty members
                 * when we enter a PowerShell context */
                PSObjectDynamicContainer.EnsurePSObject(value);

                return value;
            }
            set => setValue(value);
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
