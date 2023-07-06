using System.Linq;
using System.Management.Automation;
using DebugTools.Ui;
using FlaUI.Core.Definitions;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "UiElement", DefaultParameterSetName = ParameterSet.Default)]
    public class GetUiElement : UiCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public IUiElement Parent { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        [Parameter(Mandatory = false)]
        public ControlType[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public ControlType[] ParentType { get; set; }

        protected override void ProcessRecordEx()
        {
            if (Parent == null)
                Parent = Session.Root;

            var descs = Parent.DescendantNodesAndSelf();

            if (Name != null)
                descs = GetDbgProfiler.FilterByWildcardArray(Name, descs, r => r.Name);

            if (Type != null)
                descs = descs.Where(d => Type.Contains(d.ControlType));

            if (ParentType != null)
                descs = descs.Where(d => d.Parent != null && ParentType.Contains(d.Parent.ControlType));

            //Enumerate all items before emitting to the pipeline in case a downstream cmdlet causes the element to be closed
            foreach (var item in descs.ToArray())
                WriteObject(item);
        }
    }
}
