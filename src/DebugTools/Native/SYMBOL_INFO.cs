using System.Runtime.InteropServices;
using ClrDebug.DbgEng;

namespace DebugTools
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SYMBOL_INFO
    {
        public int SizeOfStruct;
        public int TypeIndex;
        public fixed ulong Reserved[2];
        public int Index;
        public int Size;
        public ulong ModBase;
        public SymFlag Flags;
        public ulong Value;
        public ulong Address;
        public uint Register;
        public uint Scope;
        public SymTag Tag;
        public int NameLen;
        public int MaxNameLen;
        public fixed char Name[1];
    }
}
