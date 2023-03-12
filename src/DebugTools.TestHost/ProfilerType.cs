using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task Async()
        {
            await Task.Delay(100);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Thread_NameAfterCreate()
        {
            Thread.Sleep(100);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Thread_NameBeforeCreate()
        {
            Thread.Sleep(100);
        }
    }
}
