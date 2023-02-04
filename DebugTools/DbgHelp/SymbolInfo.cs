using System;
using System.Runtime.InteropServices;
using ClrDebug.DbgEng;

namespace DebugTools
{
    [Serializable]
    public unsafe struct SymbolInfo
    {
        public string Name { get; }

        public ulong Address { get; }

        public SymFlag Flags { get; }

        public SymTag Tag { get; }

        public int Size { get; }

        public SymbolInfo(SYMBOL_INFO* symbolInfo)
        {
            Address = symbolInfo->Address;
            Flags = symbolInfo->Flags;
            Tag = symbolInfo->Tag;
            Size = symbolInfo->Size;

            Name = Marshal.PtrToStringAnsi((IntPtr) symbolInfo->Name, symbolInfo->NameLen);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
