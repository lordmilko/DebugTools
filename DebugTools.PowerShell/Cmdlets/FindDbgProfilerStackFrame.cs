using System;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Find, "DbgProfilerStackFrame")]
    public class FindDbgProfilerStackFrame : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public SwitchParameter VoidValue { get; set; }

        #region Primitive

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public bool[] BoolValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public char[] CharValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public sbyte[] SByteValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public byte[] ByteValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public short[] Int16Value { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public ushort[] UInt16Value { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public int[] Int32Value { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public uint[] UInt32Value { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public long[] Int64Value { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public ulong[] UInt64Value { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public float[] FloatValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public double[] DoubleValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public IntPtr[] IntPtrValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public UIntPtr[] UIntPtrValue { get; set; }

        #endregion

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] StringValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] TypeName { get; set; }

        private FrameFilterer filter;

        protected override void BeginProcessing()
        {
            var options = new FrameFilterOptions
            {
                Unique = Unique,
                Include = Include,
                Exclude = Exclude,
                VoidValue = VoidValue,
                BoolValue = BoolValue,
                CharValue = CharValue,
                SByteValue = SByteValue,
                ByteValue = ByteValue,
                Int16Value = Int16Value,
                UInt16Value = UInt16Value,
                Int32Value = Int32Value,
                UInt32Value = UInt32Value,
                Int64Value = Int64Value,
                UInt64Value = UInt64Value,
                FloatValue = FloatValue,
                DoubleValue = DoubleValue,
                IntPtrValue = IntPtrValue,
                UIntPtrValue = UIntPtrValue,
                StringValue = StringValue,
                TypeName = TypeName
            };

            filter = new FrameFilterer(options);
        }

        protected override void DoProcessRecordEx()
        {
            filter.ProcessFrame(Frame);
        }

        protected override void EndProcessing()
        {
            foreach (var item in filter.SortedFrames)
                WriteObject(item);
        }

        public void Dispose()
        {
            filter?.Dispose();
        }
    }
}
