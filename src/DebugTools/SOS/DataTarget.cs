using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClrDebug;

namespace DebugTools.SOS
{
    /// <summary>
    /// Represents a simple <see cref="ICLRDataTarget"/> for reading memory from a target process.
    /// </summary>
    public class DataTarget : ICLRDataTarget
    {
        private Process process;
        private bool isWow64;

        //IXCLRDataProcess::Flush does not use DAC_ENTER, which can lead to heap corruption if the process is flushed while it is running. To work around this,
        //ReadVirtual is tricked to call IXCLRDataProcess::Flush upon attempting to execute another method (which DOES call DAC_ENTER), thereby giving us a safe flush.
        //Taken from RuntimeBuilder.FlushDac() in ClrMD.
        private Action doFlush;
        private volatile int flushContext;
        private ulong MagicFlushAddress = 0x43; //A random (invalid) address constant to signal we're doing a flush, not reading an address

        public DataTarget(Process process)
        {
            this.process = process;

            if (!NativeMethods.IsWow64Process(process.Handle, out isWow64))
                throw new SOSException($"Failed to query {nameof(NativeMethods.IsWow64Process)}: {(HRESULT)Marshal.GetHRForLastWin32Error()}");

            if (isWow64 && IntPtr.Size == 8)
                throw new InvalidOperationException("Cannot attach to a 32-bit target from a 64-bit process.");
        }

        public void SetFlushCallback(Action action)
        {
            doFlush = action;
        }

        public void Flush(SOSDacInterface sos)
        {
            Interlocked.Increment(ref flushContext);

            try
            {
                sos.TryGetWorkRequestData(MagicFlushAddress, out _);
            }
            finally
            {
                Interlocked.Decrement(ref flushContext);
            }
        }

        public HRESULT GetMachineType(out IMAGE_FILE_MACHINE machineType)
        {
            //This sample assumes Windows
            machineType = isWow64 ? IMAGE_FILE_MACHINE.I386 : IMAGE_FILE_MACHINE.AMD64;
            return HRESULT.S_OK;
        }

        public HRESULT GetPointerSize(out int pointerSize)
        {
            pointerSize = isWow64 ? 4 : 8;
            return HRESULT.S_OK;
        }

        //This method is called to get the base address of certain loaded modules in the target process, principally clr.dll
        public HRESULT GetImageBase(string imagePath, out CLRDATA_ADDRESS baseAddress)
        {
            //In .NET Core it's possible to dbgshim!RegisterForRuntimeStartup (with the catch-22 you already need to know
            //where your dbgshim.dll is). In .NET Framework however there is no such function, so our fallback strategy is to repeatedly poll to see when it's loaded

            ProcessModule module = null;

            for (var i = 0; i < 100; i++)
            {
                //Once Process.Modules have been retrieved, they'll be cached. As such we re-retrieve the Process each time to get the latest available set of modules
                module = Process.GetProcessById(process.Id).Modules.Cast<ProcessModule>().FirstOrDefault(m => StringComparer.OrdinalIgnoreCase.Equals(m.ModuleName, imagePath));

                if (module != null)
                    break;

                Thread.Sleep(50);
            }

            if (module == null)
            {
                baseAddress = 0;
                return HRESULT.E_FAIL;
            }

            baseAddress = module.BaseAddress.ToInt64();
            return HRESULT.S_OK;
        }

        public unsafe HRESULT ReadVirtual(CLRDATA_ADDRESS address, IntPtr buffer, int bytesRequested, out int bytesRead)
        {
            if (address == MagicFlushAddress && flushContext > 0)
            {
                doFlush?.Invoke();
                bytesRead = 0;
                return HRESULT.E_FAIL;
            }

            //ReadProcessMemory will fail if any part of the region to read does not have read access, which can commonly occur
            //when attempting to read across a page boundary. As such, memory requests must be "chunked" to be within each page,
            //which we do by gradually shifting the pointer of our buffer along until we've read everything we're after

            var pageSize = 0x1000;

            var totalRead = 0;

            HRESULT hr = HRESULT.S_OK;

            //For some reason, when trying to read the optHeaderMagic in GetMachineAndResourceSectionRVA() when trying to establish an SOS
            //against PowerShell 7, when using the buffer provided by mscordaccore, ReadProcessMemory would say it read 2 bytes, but no memory would actually change.
            //Manually copying into mscordaccore's buffer after we safely read into our own buffer appears to resolve this
            var innerBuffer = Marshal.AllocHGlobal(pageSize);

            try
            {
                while (bytesRequested > 0)
                {
                    //This bit of magic ensures we're not reading more than 1 page worth of data. I don't understand how this works
                    //however Microsoft use it all the time so you know it's right
                    var readSize = pageSize - (int)(address & (pageSize - 1));
                    readSize = Math.Min(bytesRequested, readSize);

                    var result = NativeMethods.ReadProcessMemory(
                        process.Handle,
                        address,
                        innerBuffer,
                        readSize,
                        out bytesRead
                    );

                    Buffer.MemoryCopy(innerBuffer.ToPointer(), buffer.ToPointer(), bytesRead, bytesRead);

                    if (!result)
                    {
                        //Some methodtables' parents appear to point to an invalid memory address. When we read these invalid memory addresses,
                        //pass them back to the DAC and then are asked to read some actual data from these invalid locations, this will naturally fail with ERROR_PARTIAL_COPY,
                        //but really its a total failure
                        if (totalRead > 0)
                            hr = HRESULT.S_OK;
                        else
                            hr = (HRESULT)Marshal.GetHRForLastWin32Error();

                        break;
                    }

                    totalRead += bytesRead;
                    address += bytesRead;
                    buffer = new IntPtr(buffer.ToInt64() + bytesRead);
                    bytesRequested -= bytesRead;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(innerBuffer);
            }

            if (hr == HRESULT.S_OK)
                bytesRead = totalRead;
            else
                bytesRead = 0;

            return hr;
        }

        #region Not Implemented

        public HRESULT WriteVirtual(CLRDATA_ADDRESS address, IntPtr buffer, int bytesRequested, out int bytesWritten)
        {
            throw new NotImplementedException();
        }

        public HRESULT GetTLSValue(int threadID, int index, out CLRDATA_ADDRESS value)
        {
            throw new NotImplementedException();
        }

        public HRESULT SetTLSValue(int threadID, int index, CLRDATA_ADDRESS value)
        {
            throw new NotImplementedException();
        }

        public HRESULT GetCurrentThreadID(out int threadID)
        {
            throw new NotImplementedException();
        }

        public unsafe HRESULT GetThreadContext(int threadID, ContextFlags contextFlags, int contextSize, IntPtr context)
        {
            NativeMethods.ZeroMemory(context, contextSize);

            if (IntPtr.Size == 4)
                ((X86_CONTEXT*) context)->ContextFlags = contextFlags;
            else
                ((AMD64_CONTEXT*) context)->ContextFlags = contextFlags;

            var hThread = NativeMethods.OpenThread(ThreadAccess.GET_CONTEXT, false, threadID);

            if (hThread == IntPtr.Zero)
                return (HRESULT) Marshal.GetHRForLastWin32Error();

            if (!NativeMethods.GetThreadContext(hThread, context))
                return (HRESULT)Marshal.GetHRForLastWin32Error();

            return HRESULT.S_OK;
        }

        public HRESULT SetThreadContext(int threadID, int contextSize, IntPtr context)
        {
            throw new NotImplementedException();
        }

        public HRESULT Request(uint reqCode, int inBufferSize, IntPtr inBuffer, int outBufferSize, IntPtr outBuffer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
