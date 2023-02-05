using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Show, "DbgProfilerStackTrace")]
    public class ShowDbgProfilerStackTrace : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public int Depth { get; set; } = 10;

        [Parameter(Mandatory = false)]
        public SwitchParameter Unlimited { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Filter)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false)]
        public string[] HighlightMethod { get; set; }

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

        private WildcardPattern[] highlightMethodNameWildcards;

        private List<IFrame> frames = new List<IFrame>();

        private FrameFilterer filter;

        private IOutputSource output = new ConsoleOutputSource();

        protected override void BeginProcessing()
        {
            if (HighlightMethod != null)
                highlightMethodNameWildcards = HighlightMethod.Select(h => new WildcardPattern(h, WildcardOptions.IgnoreCase)).ToArray();

            if (ParameterSetName == ParameterSet.Filter)
            {
                filter = new FrameFilterer(
                    new FrameFilterOptions
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
                    }
                );
            }
        }

        protected override void DoProcessRecordEx()
        {
            if (filter != null)
                filter.ProcessFrame(Frame);
            else
                frames.Add(Frame);
        }

        protected override void EndProcessing()
        {
            List<IFrame> outputFrames;

            if (filter != null)
                outputFrames = filter.GetSortedFilteredFrames();
            else
                outputFrames = frames;

            var methodFrameFormatter = new MethodFrameFormatter(ExcludeNamespace);
            var methodFrameWriter = new MethodFrameColorWriter(methodFrameFormatter, output)
            {
                HighlightValues = filter?.MatchedValues,
                HighlightMethodNames = highlightMethodNameWildcards,
                HighlightFrames = filter?.HighlightFrames
            };

            var stackWriter = new StackFrameWriter(
                methodFrameWriter,
                GetDepth(),
                CancellationToken
            );

            stackWriter.Execute(outputFrames);

            base.EndProcessing();
        }

        private int? GetDepth()
        {
            if (Unlimited || ParameterSetName == ParameterSet.Filter)
                return null;

            return Depth;
        }

        public void Dispose()
        {
            filter?.Dispose();
        }
    }
}
