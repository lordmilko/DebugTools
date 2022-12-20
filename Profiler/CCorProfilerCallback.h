#pragma once

#include "CCommunication.h"

class CCorProfilerCallback final : public ICorProfilerCallback3
{
public:
	//The singular profiler instance of this process
	static CCorProfilerCallback* g_pProfiler;
	static HANDLE g_hExitProcess;

	CCorProfilerCallback() :
		m_pInfo(nullptr),
		m_RefCount(0)
	{
	}

	~CCorProfilerCallback()
	{
		if (m_pInfo)
			m_pInfo->Release();
	}

	//Performs a one time registration function for each unique function that is JITted
	static UINT_PTR __stdcall RecordFunction(FunctionID funcId, void* clientData, BOOL* pbHookFunction);
	static BOOL ShouldHook();
	static void NTAPI ExitProcessCallback(_In_ PVOID   lpParameter, _In_ BOOLEAN TimerOrWaitFired);

	HRESULT SetEventMask();
	HRESULT InstallHooks();
	HRESULT InstallHooksWithInfo();
	HRESULT BindLifetimeToParentProcess();

#pragma region IUnknown
	STDMETHODIMP_(ULONG) AddRef() override;
	STDMETHODIMP_(ULONG) Release() override;
	STDMETHODIMP QueryInterface(REFIID riid, void** ppvObject) override;
#pragma endregion
#pragma region ICorProfilerCallback
	/***********************************************************************************
	 * By default, all callbacks return S_OK. Callbacks that we're actually interested *
	 * in properly overriding are overridden in CCorProfilerCallback.cpp instead       *
	 ***********************************************************************************/

	STDMETHODIMP AppDomainCreationStarted(AppDomainID appDomainId) override { return S_OK; }
	STDMETHODIMP AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP AppDomainShutdownStarted(AppDomainID appDomainId) override { return S_OK; }
	STDMETHODIMP AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP AssemblyLoadStarted(AssemblyID assemblyId) override { return S_OK; }
	STDMETHODIMP AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP AssemblyUnloadStarted(AssemblyID assemblyId) override { return S_OK; }
	STDMETHODIMP AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP ModuleLoadStarted(ModuleID moduleId) override { return S_OK; }
	STDMETHODIMP ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP ModuleUnloadStarted(ModuleID moduleId) override { return S_OK; }
	STDMETHODIMP ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID assemblyId) override { return S_OK; }
	STDMETHODIMP ClassLoadStarted(ClassID classId) override { return S_OK; }
	STDMETHODIMP ClassLoadFinished(ClassID classId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP ClassUnloadStarted(ClassID classId) override { return S_OK; }
	STDMETHODIMP ClassUnloadFinished(ClassID classId, HRESULT hrStatus) override { return S_OK; }
	STDMETHODIMP FunctionUnloadStarted(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP Initialize(IUnknown* pICorProfilerInfoUnk) override;
	STDMETHODIMP JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override { return S_OK; }
	STDMETHODIMP JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override { return S_OK; }
	STDMETHODIMP JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction) override { return S_OK; }
	STDMETHODIMP JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result) override { return S_OK; }
	STDMETHODIMP JITFunctionPitched(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline) override { return S_OK; }
	STDMETHODIMP ThreadCreated(ThreadID threadId) override;
	STDMETHODIMP ThreadDestroyed(ThreadID threadId) override;
	STDMETHODIMP ThreadAssignedToOSThread(ThreadID managedThreadId, ULONG osThreadId) override { return S_OK; }
	STDMETHODIMP RemotingClientInvocationStarted() override { return S_OK; }
	STDMETHODIMP RemotingClientSendingMessage(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	STDMETHODIMP RemotingClientReceivingReply(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	STDMETHODIMP RemotingClientInvocationFinished() override { return S_OK; }
	STDMETHODIMP RemotingServerReceivingMessage(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	STDMETHODIMP RemotingServerInvocationStarted() override { return S_OK; }
	STDMETHODIMP RemotingServerInvocationReturned() override { return S_OK; }
	STDMETHODIMP RemotingServerSendingReply(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	STDMETHODIMP UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override { return S_OK; }
	STDMETHODIMP ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override { return S_OK; }
	STDMETHODIMP RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason) override { return S_OK; }
	STDMETHODIMP RuntimeSuspendFinished() override { return S_OK; }
	STDMETHODIMP RuntimeSuspendAborted() override { return S_OK; }
	STDMETHODIMP RuntimeResumeStarted() override { return S_OK; }
	STDMETHODIMP RuntimeResumeFinished() override { return S_OK; }
	STDMETHODIMP RuntimeThreadSuspended(ThreadID threadId) override { return S_OK; }
	STDMETHODIMP RuntimeThreadResumed(ThreadID threadId) override { return S_OK; }
	STDMETHODIMP MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[]) override { return S_OK; }
	STDMETHODIMP ObjectAllocated(ObjectID objectId, ClassID classId) override { return S_OK; }
	STDMETHODIMP ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[]) override { return S_OK; }
	STDMETHODIMP ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]) override { return S_OK; }
	STDMETHODIMP RootReferences(ULONG cRootRefs, ObjectID rootRefIds[]) override { return S_OK; }
	STDMETHODIMP ExceptionThrown(ObjectID thrownObjectId) override { return S_OK; }
	STDMETHODIMP ExceptionSearchFunctionEnter(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionSearchFunctionLeave() override { return S_OK; }
	STDMETHODIMP ExceptionSearchFilterEnter(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionSearchFilterLeave() override { return S_OK; }
	STDMETHODIMP ExceptionSearchCatcherFound(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionOSHandlerEnter(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionOSHandlerLeave(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionUnwindFunctionEnter(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionUnwindFunctionLeave() override { return S_OK; }
	STDMETHODIMP ExceptionUnwindFinallyEnter(FunctionID functionId) override { return S_OK; }
	STDMETHODIMP ExceptionUnwindFinallyLeave() override { return S_OK; }
	STDMETHODIMP ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override { return S_OK; }
	STDMETHODIMP ExceptionCatcherLeave() override { return S_OK; }
	STDMETHODIMP COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable, ULONG cSlots) override { return S_OK; }
	STDMETHODIMP COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable) override { return S_OK; }
	STDMETHODIMP ExceptionCLRCatcherFound(void) override { return S_OK; }
	STDMETHODIMP ExceptionCLRCatcherExecute(void) override { return S_OK; }
	STDMETHODIMP Shutdown() override;
#pragma endregion
#pragma region ICorProfilerCallback2
	STDMETHODIMP ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR* name) override;
	STDMETHODIMP GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override { return S_OK; }
	STDMETHODIMP SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[]) override { return S_OK; }

	STDMETHODIMP GarbageCollectionFinished(void) override { return S_OK; }
	STDMETHODIMP FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID) override { return S_OK; }
	STDMETHODIMP RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[]) override { return S_OK; }
	STDMETHODIMP HandleCreated(GCHandleID handleId, ObjectID initialObjectId) override { return S_OK; }
	STDMETHODIMP HandleDestroyed(GCHandleID handleId) override { return S_OK; }
#pragma endregion
#pragma region ICorProfilerCallback3
	HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProfilerAttachComplete(void) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded(void) override { return S_OK; }
#pragma endregion

	ICorProfilerInfo3* m_pInfo;

private:
	CCommunication m_Communication;

	long m_RefCount;
};