﻿using System;

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

        #region Primitive

        public bool[] BoolValue { get; set; }

        public char[] CharValue { get; set; }

        public sbyte[] SByteValue { get; set; }

        public byte[] ByteValue { get; set; }

        public short[] Int16Value { get; set; }

        public ushort[] UInt16Value { get; set; }

        public int[] Int32Value { get; set; }

        public uint[] UInt32Value { get; set; }

        public long[] Int64Value { get; set; }

        public ulong[] UInt64Value { get; set; }

        public float[] FloatValue { get; set; }

        public double[] DoubleValue { get; set; }

        public IntPtr[] IntPtrValue { get; set; }

        public UIntPtr[] UIntPtrValue { get; set; }

        #endregion

        public string[] StringValue { get; set; }

        public string[] TypeName { get; set; }

        public bool HasFilterValue { get; set; }
    }
}