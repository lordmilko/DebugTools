using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DebugTools
{
    [DebuggerDisplay("Base = 0x{ImageBase.ToInt64().ToString(\"X\"),nq}, Name = {Name,nq}")]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct RTL_PROCESS_MODULE_INFORMATION
    {
        public IntPtr Section;
        public IntPtr MappedBase;
        public IntPtr ImageBase;
        public uint ImageSize;
        public uint Flags;
        public ushort LoadOrderIndex;
        public ushort InitOrderIndex;
        public ushort LoadCount;
        public ushort OffsetToFileName;

        public fixed byte FullPathName[256];

        public string Name
        {
            get
            {
                fixed (byte* c = FullPathName)
                {
                    var ptr = c + OffsetToFileName;

                    return Marshal.PtrToStringAnsi((IntPtr) ptr);
                }
            }
        }

        public string FullName
        {
            get
            {
                fixed (byte* c = FullPathName)
                {
                    return Marshal.PtrToStringAnsi((IntPtr) c);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
