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
            set => settings[nameof(Unique)] = value;
        }

        public string[] Include
        {
            get => GetSetting<string[]>(nameof(Include));
            set => settings[nameof(Include)] = value;
        }

        public string[] Exclude
        {
            get => GetSetting<string[]>(nameof(Exclude));
            set => settings[nameof(Exclude)] = value;
        }

        public string[] CalledFrom
        {
            get => GetSetting<string[]>(nameof(CalledFrom));
            set => settings[nameof(CalledFrom)] = value;
        }

        public bool VoidValue
        {
            get => GetSetting<bool>(nameof(VoidValue));
            set => settings[nameof(VoidValue)] = value;
        }

        public bool Unmanaged
        {
            get => GetSetting<bool>(nameof(Unmanaged));
            set => settings[nameof(Unmanaged)] = value;
        }

        #region Primitive

        public bool[] BoolValue
        {
            get => GetSetting<bool[]>(nameof(BoolValue));
            set => SetHasValue(nameof(BoolValue), value);
        }

        public char[] CharValue
        {
            get => GetSetting<char[]>(nameof(CharValue));
            set => SetHasValue(nameof(CharValue), value);
        }

        public sbyte[] SByteValue
        {
            get => GetSetting<sbyte[]>(nameof(SByteValue));
            set => SetHasValue(nameof(SByteValue), value);
        }

        public byte[] ByteValue
        {
            get => GetSetting<byte[]>(nameof(ByteValue));
            set => SetHasValue(nameof(ByteValue), value);
        }

        public short[] Int16Value
        {
            get => GetSetting<short[]>(nameof(Int16Value));
            set => SetHasValue(nameof(Int16Value), value);
        }

        public ushort[] UInt16Value
        {
            get => GetSetting<ushort[]>(nameof(UInt16Value));
            set => SetHasValue(nameof(UInt16Value), value);
        }

        public int[] Int32Value
        {
            get => GetSetting<int[]>(nameof(Int32Value));
            set => SetHasValue(nameof(Int32Value), value);
        }

        public uint[] UInt32Value
        {
            get => GetSetting<uint[]>(nameof(UInt32Value));
            set => SetHasValue(nameof(UInt32Value), value);
        }

        public long[] Int64Value
        {
            get => GetSetting<long[]>(nameof(Int64Value));
            set => SetHasValue(nameof(Int64Value), value);
        }

        public ulong[] UInt64Value
        {
            get => GetSetting<ulong[]>(nameof(UInt64Value));
            set => SetHasValue(nameof(UInt64Value), value);
        }

        public float[] FloatValue
        {
            get => GetSetting<float[]>(nameof(FloatValue));
            set => SetHasValue(nameof(FloatValue), value);
        }

        public double[] DoubleValue
        {
            get => GetSetting<double[]>(nameof(DoubleValue));
            set => SetHasValue(nameof(DoubleValue), value);
        }

        public IntPtr[] IntPtrValue
        {
            get => GetSetting<IntPtr[]>(nameof(IntPtrValue));
            set => SetHasValue(nameof(IntPtrValue), value);
        }

        public UIntPtr[] UIntPtrValue
        {
            get => GetSetting<UIntPtr[]>(nameof(UIntPtrValue));
            set => SetHasValue(nameof(UIntPtrValue), value);
        }

        #endregion

        public string[] StringValue
        {
            get => GetSetting<string[]>(nameof(StringValue));
            set => SetHasValue(nameof(StringValue), value);
        }

        public string[] ClassTypeName
        {
            get => GetSetting<string[]>(nameof(ClassTypeName));
            set => SetHasValue(nameof(ClassTypeName), value);
        }

        #region Method

        public string[] MethodModuleName
        {
            get => GetSetting<string[]>(nameof(MethodModuleName));
            set => settings[nameof(MethodModuleName)] = value;
        }

        public string[] MethodTypeName
        {
            get => GetSetting<string[]>(nameof(MethodTypeName));
            set => settings[nameof(MethodTypeName)] = value;
        }

        public string[] MethodName
        {
            get => GetSetting<string[]>(nameof(MethodName));
            set => settings[nameof(MethodName)] = value;
        }

        #endregion
        #region ParentMethod

        public string[] ParentMethodModuleName
        {
            get => GetSetting<string[]>(nameof(ParentMethodModuleName));
            set => settings[nameof(ParentMethodModuleName)] = value;
        }

        public string[] ParentMethodTypeName
        {
            get => GetSetting<string[]>(nameof(ParentMethodTypeName));
            set => settings[nameof(ParentMethodTypeName)] = value;
        }

        public string[] ParentMethodName
        {
            get => GetSetting<string[]>(nameof(ParentMethodName));
            set => settings[nameof(ParentMethodName)] = value;
        }

        #endregion

        public bool HasFilterValue { get; private set; }

        public bool IsCalledFromOnly => settings.Keys.Count == 1 && settings.ContainsKey(nameof(CalledFrom));

        private T GetSetting<T>(string name)
        {
            if (settings.TryGetValue(name, out var value))
                return (T)value;

            return default(T);
        }

        private void SetHasValue<T>(string property, T[] arr)
        {
            if (arr != null && arr.Length > 0)
                HasFilterValue = true;

            settings[property] = arr;
        }
    }
}
