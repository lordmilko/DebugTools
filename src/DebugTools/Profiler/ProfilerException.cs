using System.Runtime.InteropServices;

namespace DebugTools.Profiler
{
    class ProfilerException : COMException
    {
        public new PROFILER_HRESULT HResult => (PROFILER_HRESULT) base.HResult;

        public ProfilerException(PROFILER_HRESULT hr) : base($"Error HRESULT {hr} has been returned from a call to a COM component.", (int) hr)
        {
        }

        public ProfilerException(string message, PROFILER_HRESULT hr) : base(message, (int) hr)
        {
        }
    }
}
