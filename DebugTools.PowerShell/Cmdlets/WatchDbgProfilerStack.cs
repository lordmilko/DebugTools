using System;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Watch, "DbgProfilerStack")]
    public class WatchDbgProfilerStack : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Filter)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public SwitchParameter VoidValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public SwitchParameter Unmanaged { get; set; }

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

        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeNamespace { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DisableProgress { get; set; }

        protected override void ProcessRecordEx()
        {
            using (CtrlCHandler())
            {
                ProgressRecord record = null;

                if (!DisableProgress)
                {
                    record = new ProgressRecord(1, "Watch-DbgProfilerStack", "Watching... (Ctrl+C to end)");
                    WriteProgress(record);
                }

                var filter = new FrameFilterer(new FrameFilterOptions
                {
                    Include = Include,
                    Exclude = Exclude,
                    Unique = Unique,
                    VoidValue = VoidValue,
                    Unmanaged = Unmanaged,
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
                    TypeName = TypeName,
                });

                var methodFrameFormatter = new MethodFrameFormatter(ExcludeNamespace);
                var writer = new MethodFrameStringWriter(methodFrameFormatter);

                try
                {
                    foreach (var item in Session.Watch(TokenSource, f => filter.CheckFrameAndClear(f)))
                    {
                        var str = writer.ToString(item);

                        Host.UI.WriteLine(str);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    if (!DisableProgress)
                    {
                        record.RecordType = ProgressRecordType.Completed;
                        WriteProgress(record);
                    }
                }
            }
        }
    }
}
