using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug;
using DebugTools.SOS;
using static ClrDebug.HRESULT;

namespace DebugTools.Host
{
    public partial class HostApp
    {
        [Obsolete]
        private Dictionary<SOSProcessHandle, SOSProcess> sosProcesses = new Dictionary<SOSProcessHandle, SOSProcess>();

        public SOSProcessHandle CreateSOSProcess(int processId)
        {
            var process = Process.GetProcessById(processId);

            var sosProcess = new SOSProcess(process);

            var handle = new SOSProcessHandle(processId);

#pragma warning disable 612
            sosProcesses[handle] = sosProcess;
#pragma warning restore 612

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
                return process;
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
    }
}
