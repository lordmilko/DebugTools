using System;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class FilterStackFrameCmdlet : ProfilerSessionCmdlet
    {
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

        [Alias("StructTypeName")]
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] ClassTypeName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] MethodModuleName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] MethodTypeName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] MethodName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] ParentMethodModuleName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] ParentMethodTypeName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] ParentMethodName { get; set; }

        protected FilterStackFrameCmdlet(bool mayCreateGlobalSession = false) : base(mayCreateGlobalSession)
        {
        }

        protected FrameFilterOptions GetFrameFilterOptions(bool unique, string[] include, string[] exclude, string[] calledFrom)
        {
            var options = new FrameFilterOptions
            {
                Unique = unique,
                Include = include,
                Exclude = exclude,
                CalledFrom = calledFrom,
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
                ClassTypeName = ClassTypeName,

                MethodModuleName = MethodModuleName,
                MethodTypeName = MethodTypeName,
                MethodName = MethodName,
                ParentMethodModuleName = ParentMethodModuleName,
                ParentMethodTypeName = ParentMethodTypeName,
                ParentMethodName = ParentMethodName
            };

            return options;
        }
    }
}
