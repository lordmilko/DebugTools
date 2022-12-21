using System;

namespace DebugTools.TestHost
{
    class SigBlobType
    {
        public void NoArgs_ReturnVoid()
        {
        }

        public void OneArg_ReturnVoid(int a)
        {
        }

        public void BoolArg(bool a)
        {
        }

        public void CharArg(char a)
        {
        }

        public void ByteArg(byte a)
        {
        }

        public void SByteArg(sbyte a)
        {
        }

        public void Int16Arg(short a)
        {
        }

        public void UInt16Arg(ushort a)
        {
        }

        public void Int32Arg(int a)
        {
        }

        public void UInt32Arg(uint a)
        {
        }

        public void Int64Arg(long a)
        {
        }

        public void UInt64Arg(ulong a)
        {
        }

        public void FloatArg(float a)
        {
        }

        public void DoubleArg(double a)
        {
        }

        public void IntPtrArg(IntPtr a)
        {
        }

        public void UIntPtrArg(UIntPtr a)
        {
        }

        public void VarArg1(string a, __arglist)
        {
            VarArg2("first", __arglist(2, true, "three"));
        }

        public void VarArg2(string a, __arglist)
        {
        }
    }
}
