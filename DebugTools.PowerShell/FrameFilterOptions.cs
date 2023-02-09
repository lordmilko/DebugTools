using System;

namespace DebugTools.PowerShell
{
    //Filtering by pointers and function pointers is not currently supported. A function pointer just returns an address, and in the case of pointers,
    //we either record a void* or read the raw value (which can then be filtered using the other filter options)

    public class FrameFilterOptions
    {
        public bool Unique { get; set; }

        public string[] Include { get; set; }

        public string[] Exclude { get; set; }

        public bool VoidValue { get; set; }

        public bool Unmanaged { get; set; }

        #region Primitive

        private bool[] boolValue;
        public bool[] BoolValue
        {
            get => boolValue;
            set => boolValue = SetHasValue(value);
        }

        private char[] charValue;
        public char[] CharValue
        {
            get => charValue;
            set => charValue = SetHasValue(value);
        }

        private sbyte[] sbyteValue;
        public sbyte[] SByteValue
        {
            get => sbyteValue;
            set => sbyteValue = SetHasValue(value);
        }

        private byte[] byteValue;
        public byte[] ByteValue
        {
            get => byteValue;
            set => byteValue = SetHasValue(value);
        }

        private short[] int16Value;
        public short[] Int16Value
        {
            get => int16Value;
            set => int16Value = SetHasValue(value);
        }

        private ushort[] uint16Value;
        public ushort[] UInt16Value
        {
            get => uint16Value;
            set => uint16Value = SetHasValue(value);
        }

        private int[] int32Value;
        public int[] Int32Value
        {
            get => int32Value;
            set => int32Value = SetHasValue(value);
        }

        private uint[] uint32Value;
        public uint[] UInt32Value
        {
            get => uint32Value;
            set => uint32Value = SetHasValue(value);
        }

        private long[] int64Value;
        public long[] Int64Value
        {
            get => int64Value;
            set => int64Value = SetHasValue(value);
        }

        private ulong[] uint64Value;
        public ulong[] UInt64Value
        {
            get => uint64Value;
            set => uint64Value = SetHasValue(value);
        }

        private float[] floatValue;
        public float[] FloatValue
        {
            get => floatValue;
            set => floatValue = SetHasValue(value);
        }

        private double[] doubleValue;
        public double[] DoubleValue
        {
            get => doubleValue;
            set => doubleValue = SetHasValue(value);
        }

        private IntPtr[] intPtrValue;
        public IntPtr[] IntPtrValue
        {
            get => intPtrValue;
            set => intPtrValue = SetHasValue(value);
        }

        private UIntPtr[] uintPtrValue;
        public UIntPtr[] UIntPtrValue
        {
            get => uintPtrValue;
            set => uintPtrValue = SetHasValue(value);
        }

        #endregion

        private string[] stringValue;
        public string[] StringValue
        {
            get => stringValue;
            set => stringValue = SetHasValue(value);
        }

        private string[] classTypeName;
        public string[] ClassTypeName
        {
            get => classTypeName;
            set => classTypeName = SetHasValue(value);
        }

        #region Method

        public string[] MethodModuleName { get; set; }

        public string[] MethodTypeName { get; set; }

        public string[] MethodName { get; set; }

        #endregion
        #region ParentMethod

        public string[] ParentMethodModuleName { get; set; }

        public string[] ParentMethodTypeName { get; set; }

        public string[] ParentMethodName { get; set; }

        #endregion

        public bool HasFilterValue { get; private set; }

        private T[] SetHasValue<T>(T[] arr)
        {
            if (arr != null && arr.Length > 0)
                HasFilterValue = true;

            return arr;
        }
    }
}
