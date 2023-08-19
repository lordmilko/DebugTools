using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugTools.Ui
{
    struct MMAReader
    {
        private int position;
        private MemoryMappedViewAccessor mma;

        public MMAReader(MemoryMappedViewAccessor mma)
        {
            position = 0;
            this.mma = mma;
        }

        public uint ReadUInt32()
        {
            var result = mma.ReadUInt32(position);
            position += 4;
            return result;
        }

        public ulong ReadPointer()
        {
            var result = IntPtr.Size == 4 ? mma.ReadUInt32(position) : mma.ReadUInt64(position);
            position += IntPtr.Size;
            return result;
        }

        public T ReadStruct<T>() where T : struct
        {
            mma.Read<T>(position, out var val);
            position += Marshal.SizeOf<T>();
            return val;
        }

        public T ReadEnum<T>() where T : struct
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException($"Cannot read value for type '{typeof(T).Name}': type is not an enum");

            var underlying = Enum.GetUnderlyingType(typeof(T));

            if (underlying == typeof(int) || underlying == typeof(uint))
                return (T) (object) (int) ReadUInt32();

            throw new NotImplementedException($"Don't know how to deserialize underlying type '{underlying.Name}' for enum '{typeof(T).Name}'");
        }

        public byte[] ReadBytes(int length)
        {
            var array = new byte[length];
            mma.ReadArray(position, array, 0, length);
            position += length;
            return array;
        }

        public string ReadString()
        {
            var length = ReadUInt32();
            var bytes = ReadBytes((int) length);

            var str = Encoding.ASCII.GetString(bytes);

            return str;
        }
    }
}
