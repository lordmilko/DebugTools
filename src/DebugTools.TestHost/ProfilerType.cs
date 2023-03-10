using System.Runtime.CompilerServices;

namespace DebugTools.TestHost
{
    class ProfilerType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NoArgs()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SingleChild()
        {
            SingleChild1();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SingleChild1()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TwoChildren()
        {
            TwoChildren1();
            TwoChildren2();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TwoChildren1()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TwoChildren2()
        {
        }
    }
}
