using System;
using System.Collections.Generic;

namespace DebugTools.PowerShell
{
    //Filtering by pointers and function pointers is not currently supported. A function pointer just returns an address, and in the case of pointers,
    //we either record a void* or read the raw value (which can then be filtered using the other filter options)

    public class FrameFilterOptions
    {
        private Dictionary<string, object> settings = new Dictionary<string, object>();

        public bool Unique
        {
            get => GetSetting<bool>(nameof(Unique));
            set => SetSetting(nameof(Unique), value);
        }

        public string[] Include
        {
            get => GetSetting<string[]>(nameof(Include));
            set => SetSetting(nameof(Include), value);
        }

        public string[] Exclude
        {
            get => GetSetting<string[]>(nameof(Exclude));
            set => SetSetting(nameof(Exclude), value);
        }

        public string[] CalledFrom
        {
            get => GetSetting<string[]>(nameof(CalledFrom));
            set => SetSetting(nameof(CalledFrom), value);
        }

        public bool VoidValue
        {
            get => GetSetting<bool>(nameof(VoidValue));
            set => SetSetting(nameof(VoidValue), value);
        }

        public bool Unmanaged
        {
            get => GetSetting<bool>(nameof(Unmanaged));
            set => SetSetting(nameof(Unmanaged), value);
        }

        #region Primitive

        public bool[] BoolValue
        {
            get => GetSetting<bool[]>(nameof(BoolValue));
            set => SetValueFilter(nameof(BoolValue), value);
        }

        public char[] CharValue
        {
            get => GetSetting<char[]>(nameof(CharValue));
            set => SetValueFilter(nameof(CharValue), value);
        }

        public sbyte[] SByteValue
        {
            get => GetSetting<sbyte[]>(nameof(SByteValue));
            set => SetValueFilter(nameof(SByteValue), value);
        }

        public byte[] ByteValue
        {
            get => GetSetting<byte[]>(nameof(ByteValue));
            set => SetValueFilter(nameof(ByteValue), value);
        }

        public short[] Int16Value
        {
            get => GetSetting<short[]>(nameof(Int16Value));
            set => SetValueFilter(nameof(Int16Value), value);
        }

        public ushort[] UInt16Value
        {
            get => GetSetting<ushort[]>(nameof(UInt16Value));
            set => SetValueFilter(nameof(UInt16Value), value);
        }

        public int[] Int32Value
        {
            get => GetSetting<int[]>(nameof(Int32Value));
            set => SetValueFilter(nameof(Int32Value), value);
        }

        public uint[] UInt32Value
        {
            get => GetSetting<uint[]>(nameof(UInt32Value));
            set => SetValueFilter(nameof(UInt32Value), value);
        }

        public long[] Int64Value
        {
            get => GetSetting<long[]>(nameof(Int64Value));
            set => SetValueFilter(nameof(Int64Value), value);
        }

        public ulong[] UInt64Value
        {
            get => GetSetting<ulong[]>(nameof(UInt64Value));
            set => SetValueFilter(nameof(UInt64Value), value);
        }

        public float[] FloatValue
        {
            get => GetSetting<float[]>(nameof(FloatValue));
            set => SetValueFilter(nameof(FloatValue), value);
        }

        public double[] DoubleValue
        {
            get => GetSetting<double[]>(nameof(DoubleValue));
            set => SetValueFilter(nameof(DoubleValue), value);
        }

        public IntPtr[] IntPtrValue
        {
            get => GetSetting<IntPtr[]>(nameof(IntPtrValue));
            set => SetValueFilter(nameof(IntPtrValue), value);
        }

        public UIntPtr[] UIntPtrValue
        {
            get => GetSetting<UIntPtr[]>(nameof(UIntPtrValue));
            set => SetValueFilter(nameof(UIntPtrValue), value);
        }

        #endregion

        public string[] StringValue
        {
            get => GetSetting<string[]>(nameof(StringValue));
            set => SetValueFilter(nameof(StringValue), value);
        }

        public string[] ClassTypeName
        {
            get => GetSetting<string[]>(nameof(ClassTypeName));
            set => SetValueFilter(nameof(ClassTypeName), value);
        }

        #region Method

        public string[] MethodModuleName
        {
            get => GetSetting<string[]>(nameof(MethodModuleName));
            set => SetSetting(nameof(MethodModuleName), value);
        }

        public string[] MethodTypeName
        {
            get => GetSetting<string[]>(nameof(MethodTypeName));
            set => SetSetting(nameof(MethodTypeName), value);
        }

        public string[] MethodName
        {
            get => GetSetting<string[]>(nameof(MethodName));
            set => SetSetting(nameof(MethodName), value);
        }

        #endregion
        #region ParentMethod

        public string[] ParentMethodModuleName
        {
            get => GetSetting<string[]>(nameof(ParentMethodModuleName));
            set => SetSetting(nameof(ParentMethodModuleName), value);
        }

        public string[] ParentMethodTypeName
        {
            get => GetSetting<string[]>(nameof(ParentMethodTypeName));
            set => SetSetting(nameof(ParentMethodTypeName), value);
        }

        public string[] ParentMethodName
        {
            get => GetSetting<string[]>(nameof(ParentMethodName));
            set => SetSetting(nameof(ParentMethodName), value);
        }

        #endregion

        public bool HasValueFilter { get; private set; }

        public bool IsCalledFromOnly => settings.Keys.Count == 1 && settings.ContainsKey(nameof(CalledFrom));

        public bool IsUniqueOnly => settings.Keys.Count == 1 && settings.ContainsKey(nameof(Unique));

        private T GetSetting<T>(string name)
        {
            if (settings.TryGetValue(name, out var value))
                return (T)value;

            return default(T);
        }

        private void SetSetting<T>(string property, T value)
        {
            if (!Equals(value, default(T)))
                settings[property] = value;
        }

        private void SetValueFilter<T>(string property, T[] arr)
        {
            if (arr != null && arr.Length > 0)
            {
                HasValueFilter = true;

                settings[property] = arr;
            }
        }
    }
}
