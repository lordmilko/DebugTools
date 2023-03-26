using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClrDebug;
using DebugTools.SOS;
using static ClrDebug.HRESULT;

namespace DebugTools.Host
{
    public partial class HostApp
    {
        [Obsolete]
        private Dictionary<SOSProcessHandle, Lazy<SOSProcess>> sosProcesses = new Dictionary<SOSProcessHandle, Lazy<SOSProcess>>();

        public SOSProcessHandle CreateSOSProcess(int processId, bool lazy)
        {
            var process = Process.GetProcessById(processId);

            Lazy<SOSProcess> value;

            //The .NET Unmanaged API does not appreciate it when you create unmanaged interfaces on one thread and consume them on another.
            //We get told our QueryInterface attempts fail. As such, for our unit tests we allow specifying that the SOSProcess should be
            //lazily created, so that all the interfaces are created on the PowerShell thread where they are consumed
            if (lazy)
                value = new Lazy<SOSProcess>(() => new SOSProcess(process));
            else
            {
                var initial = new SOSProcess(process);
                value = new Lazy<SOSProcess>(() => initial);
            }

            var handle = new SOSProcessHandle(processId);

#pragma warning disable CS0612 // Type or member is obsolete
            sosProcesses[handle] = value;
#pragma warning restore CS0612 // Type or member is obsolete

            return handle;
        }

        public void RemoveSOSProcess(SOSProcessHandle handle)
        {
#pragma warning disable 612
            sosProcesses.Remove(handle);
#pragma warning restore 612
        }

        private SOSProcess GetSOSProcess(SOSProcessHandle handle)
        {
#pragma warning disable 612
            if (sosProcesses.TryGetValue(handle, out var process))
                return process.Value;
#pragma warning restore 612

            throw new ArgumentException($"Cannot find SOSProcess with ProcessID {handle.ProcessId}.");
        }

        private SOSDacInterface GetSOS(SOSProcessHandle handle) => GetSOSProcess(handle).SOS;

        #region GetSOSAppDomain

        public SOSAppDomain GetSOSAppDomain(SOSProcessHandle handle, CLRDATA_ADDRESS address) =>
            SOSAppDomain.GetAppDomain(address, GetSOS(handle));

        public SOSAppDomain[] GetSOSAppDomains(SOSProcessHandle handle) =>
            SOSAppDomain.GetAppDomains(GetSOS(handle));

        #endregion
        #region GetSOSAssembly

        public SOSAssembly GetSOSAssembly(SOSProcessHandle handle, CLRDATA_ADDRESS address) =>
            SOSAssembly.GetAssembly(address, GetSOS(handle));

        public SOSAssembly[] GetSOSAssemblies(SOSProcessHandle handle, SOSAppDomain appDomain) =>
            SOSAssembly.GetAssemblies(appDomain, GetSOS(handle));

        #endregion
        #region GetSOSFieldDesc

        public SOSFieldDesc GetSOSFieldDesc(SOSProcessHandle handle, CLRDATA_ADDRESS address) =>
            SOSFieldDesc.GetFieldDesc(address, GetSOS(handle));

        public SOSFieldDesc[] GetSOSFieldDescs(SOSProcessHandle handle, SOSMethodTable methodTable) =>
            SOSFieldDesc.GetFieldDescs(methodTable, GetSOS(handle));

        #endregion
        #region GetSOSMethodDesc

        public SOSMethodDesc GetSOSMethodDesc(SOSProcessHandle handle, CLRDATA_ADDRESS address) =>
            SOSMethodDesc.GetMethodDesc(address, GetSOS(handle));

        public SOSMethodDesc[] GetSOSMethodDescs(SOSProcessHandle handle, SOSMethodTable methodTable) =>
            SOSMethodDesc.GetMethodDescs(methodTable, GetSOS(handle));

        #endregion
        #region GetSOSMethodTable

        public SOSMethodTable GetSOSMethodTable(SOSProcessHandle handle, CLRDATA_ADDRESS address) =>
            SOSMethodTable.GetMethodTable(address, GetSOS(handle));

        public SOSMethodTable[] GetSOSMethodTables(SOSProcessHandle handle, SOSModule module) =>
            SOSMethodTable.GetMethodTables(module, GetSOS(handle));

        #endregion
        #region GetSOSModule

        public SOSModule GetSOSModule(SOSProcessHandle handle, CLRDATA_ADDRESS address) =>
            SOSModule.GetModule(address, GetSOS(handle));

        public SOSModule[] GetSOSModules(SOSProcessHandle handle, SOSAssembly assembly) =>
            SOSModule.GetModules(assembly, GetSOS(handle));

        #endregion
        #region GetSOSThreads

        public SOSThreadInfo[] GetSOSThreads(SOSProcessHandle handle)
        {
            var sos = GetSOS(handle);

            var threadStore = sos.ThreadStoreData;

            var currentThread = threadStore.firstThread;

            var threads = new List<SOSThreadInfo>();

            while (currentThread != 0)
            {
                var threadData = sos.GetThreadData(currentThread);

                threads.Add(new SOSThreadInfo(threadData, sos));

                currentThread = threadData.nextThread;
            }

            return threads.ToArray();
        }

        #endregion
        #region Flush

        public void Flush(SOSProcessHandle handle)
        {
            var sosProcess = GetSOSProcess(handle);

            sosProcess.DataTarget.Flush(GetSOS(handle));
        }

        #endregion
        #region GetSOSStackTrace

        public object GetSOSStackTrace(SOSProcessHandle handle, int threadId)
        {
            var sosProcess = GetSOSProcess(handle);
            var sos = sosProcess.SOS;

            var xclrDataProcess = sos.As<XCLRDataProcess>();

            var hr = xclrDataProcess.TryGetTaskByOSThreadID(threadId, out var task);

            if (hr != S_OK)
            {
                return $"Failed to get {nameof(XCLRDataTask)} for thread {threadId}: {hr}";
            }

            hr = task.TryCreateStackWalk(
                CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_UNRECOGNIZED |
                CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_MANAGED_METHOD |
                CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                out var stackWalk
            );

            if (hr != S_OK)
            {
                return $"Failed to get {nameof(XCLRDataStackWalk)} for thread {threadId}: {hr}";
            }

            var sosFrames = new List<SOSStackFrame>();

            do
            {
                CLRDATA_ADDRESS ip;
                CLRDATA_ADDRESS sp;
                hr = GetFrameLocation(stackWalk, out ip, out sp);

                if (hr == S_OK)
                {
                    DacpFrameData frameData = new DacpFrameData();
                    var frameDataResult = frameData.Request(stackWalk.Raw);

                    if (frameDataResult == S_OK && frameData.frameAddr != 0)
                        sosFrames.Add(new SOSHelperStackFrame(ip, frameData, stackWalk, sos, sosProcess.DataTarget));
                    else
                        sosFrames.Add(new SOSManagedStackFrame(ip, sp, stackWalk, sos));
                }
                else
                {
                    return $"Failed to get frame location for thread {threadId}: {hr}{(hr == S_FALSE ? ". Frame may be a native frame" : string.Empty)}";
                }

                hr = stackWalk.TryNext();
            } while (hr == S_OK);

            return sosFrames.ToArray();
        }

        private HRESULT GetFrameLocation(XCLRDataStackWalk stackWalk, out CLRDATA_ADDRESS ip, out CLRDATA_ADDRESS sp)
        {
            HRESULT hr;

            if (IntPtr.Size == 4)
            {
                hr = stackWalk.TryGetContext<X86_CONTEXT>(ContextFlags.X86ContextAll, out var ctx);

                if (hr == S_OK)
                {
                    ip = ctx.Eip;
                    sp = ctx.Esp;
                }
                else
                {
                    ip = 0;
                    sp = 0;
                }
            }
            else
            {
                hr = stackWalk.TryGetContext<AMD64_CONTEXT>(ContextFlags.AMD64ContextAll, out var ctx);

                if (hr == S_OK)
                {
                    ip = ctx.Rip;
                    sp = ctx.Rsp;
                }
                else
                {
                    ip = 0;
                    sp = 0;
                }
            }

            return hr;
        }

        #endregion
        #region

        public MethodTable GetRawMethodTable(SOSProcessHandle handle, CLRDATA_ADDRESS address)
        {
            var dataTarget = GetSOSProcess(handle).DataTarget;

            var result = dataTarget.ReadVirtual<MethodTable>(address);

            return result;
        }

        public unsafe MethodDesc GetRawMethodDesc(SOSProcessHandle handle, CLRDATA_ADDRESS address)
        {
            var dataTarget = GetSOSProcess(handle).DataTarget;

            var methodDescBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<MethodDesc>());
            NativeMethods.ZeroMemory(methodDescBuffer, Marshal.SizeOf<MethodDesc>());

            try
            {
                dataTarget.ReadVirtual(address, methodDescBuffer, MethodDesc.MethodDescSize, out var bytesRead).ThrowOnNotOK();

                if (bytesRead != MethodDesc.MethodDescSize)
                    throw new InvalidOperationException($"Expected to read {MethodDesc.MethodDescSize} bytes however only {bytesRead} were read.");

                MethodDesc* methodDescPtr = (MethodDesc*) methodDescBuffer;

                //We've got a MethodDesc, but maybe it's a specific subtype of MethodDesc? If so there'll be additional fields in that subtype
                if (methodDescPtr->Classification == MethodClassification.mcInstantiated)
                {
                    //It's an InstantiatedMethodDesc. Read just the fields that are unique to InstantiatedMethodDesc

                    var toRead = Marshal.SizeOf<InstantiatedMethodDesc_Fragment>();

                    dataTarget.ReadVirtual(
                        address + MethodDesc.MethodDescSize,
                        methodDescBuffer + MethodDesc.MethodDescSize,
                        toRead,
                        out bytesRead
                    ).ThrowOnNotOK();

                    if (bytesRead != toRead)
                        throw new InvalidOperationException($"Expected to read {toRead} bytes however only {bytesRead} were read.");
                }

                var methodDesc = Marshal.PtrToStructure<MethodDesc>(methodDescBuffer);

                return methodDesc;
            }
            finally
            {
                Marshal.FreeHGlobal(methodDescBuffer);
            }
        }

        #endregion
    }
}
