#include "pch.h"
#include "CCorProfilerCallback.h"
#include "Hooks\Hooks.h"
#include <strsafe.h>

#pragma region IUnknown

/// <summary>
/// Increments the reference count of this object.
/// </summary>
/// <returns>The incremented reference count.</returns>
ULONG CCorProfilerCallback::AddRef()
{
	return InterlockedIncrement(&m_RefCount);
}

/// <summary>
/// Decrements the reference count of this object. When the reference count reaches 0 this object will be deleted.
/// </summary>
/// <returns>The new reference count of this object.</returns>
ULONG CCorProfilerCallback::Release()
{
	ULONG refCount = InterlockedDecrement(&m_RefCount);

	if (refCount == 0)
		delete this;

	return refCount;
}

/// <summary>
/// Queries the class factory for a pointer to a specific interface it may implement.
/// </summary>
/// <param name="riid">The identifier of the interface that is being queried for.</param>
/// <param name="ppvObject">A pointer to store a pointer to the interface to, if it is supported.</param>
/// <returns>E_POINTER if ppvObject was null, S_OK if the interface is supported or E_NOINTERFACE if the interface is not supported.</returns>
HRESULT CCorProfilerCallback::QueryInterface(REFIID riid, void** ppvObject)
{
	if (ppvObject == nullptr)
		return E_POINTER;

	if (riid == IID_IUnknown)
		*ppvObject = static_cast<IUnknown*>(this);
	else if (riid == __uuidof(ICorProfilerCallback))
		*ppvObject = static_cast<ICorProfilerCallback*>(this);
	else if (riid == __uuidof(ICorProfilerCallback2))
		*ppvObject = static_cast<ICorProfilerCallback2*>(this);
	else if (riid == __uuidof(ICorProfilerCallback3))
		*ppvObject = static_cast<ICorProfilerCallback3*>(this);
	else
	{
		*ppvObject = nullptr;
		return E_NOINTERFACE;
	}

	reinterpret_cast<IUnknown*>(*ppvObject)->AddRef();

	return S_OK;
}

#pragma endregion
#pragma region ICorProfilerCallback

/// <summary>
/// Initializes the profiler, performing initial setup such as registering our event masks function mappers and function hooks.
/// </summary>
/// <param name="pICorProfilerInfoUnk">A clr!ProfToEEInterfaceImpl object that should be queried to retrieve an ICorProfilerInfo* interface.</param>
/// <returns>A HRESULT that indicates success or failure. In the event of failure the profiler and its DLL will be unloaded.</returns>
HRESULT CCorProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnk)
{
#ifdef _DEBUG
	OutputDebugStringW(L"Waiting for debugger to attach...\n");

	if (GetBoolEnv("DEBUGTOOLS_WAITFORDEBUG"))
	{
		while (!::IsDebuggerPresent())
			::Sleep(100);
	}	
#endif

	BindLifetimeToParentProcess();

	HRESULT hr = S_OK;

	IfFailGo(pICorProfilerInfoUnk->QueryInterface(&m_pInfo));

	IfFailGo(m_pInfo->SetFunctionIDMapper2(RecordFunction, nullptr));

	IfFailGo(SetEventMask());
	IfFailGo(InstallHooks());
	IfFailWin32Go(EventRegisterDebugToolsProfiler());

	g_pProfiler = this;

ErrExit:
	return hr;
}

/// <summary>
/// Notifies the profiler that the application is shutting down.
/// </summary>
/// <returns>A HRESULT that indicates success or failure.</returns>
/// <remarks>This method is not guaranteed to be called. In particular, it may not be called if the application is not purely managed (such as PowerShell, which starts unmanaged
/// and then loads the runtime). When a process such as dnSpy exits, ceemain.cpp!EEShutDown creates a new thread -> clr!EEShutDownProcForSTAThread -> EEShutDownHelper -> EEToProfInterfaceImpl::Shutdown -> ICorProfilerCallback::Shutdown.
/// PowerShell does not call Shutdown when closing out of the program normally, however when the "exit" command is executed, _wmainCRTStartup will call msvcrt!doexit, leading to clr!HandleExitProcessHelper calling EEShutDown, etc.</remarks>
HRESULT CCorProfilerCallback::Shutdown()
{
	EventWriteShutdownEvent();
	return S_OK;
}

/// <summary>
/// Notifies the profiler that a thread has been created.
/// </summary>
/// <param name="threadId">The managed ID of the thread that was created.</param>
/// <returns>A HRESULT that indicates whether the profiler encountered an error processing the event.</returns>
HRESULT CCorProfilerCallback::ThreadCreated(ThreadID threadId)
{
	HRESULT hr = S_OK;

	DWORD win32ThreadId;
	IfFailGo(m_pInfo->GetThreadInfo(threadId, &win32ThreadId));

	EventWriteThreadCreateEvent(win32ThreadId);

ErrExit:
	return hr;
}

/// <summary>
/// Notifies the profiler that a thread has been destroyed.
/// </summary>
/// <param name="threadId">The managed ID of the thread that was destroyed.</param>
/// <returns>A HRESULT that indicates whether the profiler encountered an error processing the event.</returns>
HRESULT CCorProfilerCallback::ThreadDestroyed(ThreadID threadId)
{
	HRESULT hr = S_OK;

	DWORD win32ThreadId;
	IfFailGo(m_pInfo->GetThreadInfo(threadId, &win32ThreadId));

	EventWriteThreadDestroyEvent(win32ThreadId);

ErrExit:
	return hr;
}

#pragma endregion
#pragma region ICorProfilerCallback2

HRESULT CCorProfilerCallback::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR* name)
{
	WCHAR copy[100];

	//MSDN states the name is not guaranteed to be null terminated, so we make a copy just in case
	StringCchCopyN(copy, 100, name, cchName);
	copy[cchName + 1] = '\0';

	EventWriteThreadNameEvent(copy);

	return S_OK;
}

#pragma endregion
#pragma region CCorProfilerCallback

CCorProfilerCallback* CCorProfilerCallback::g_pProfiler;
HANDLE CCorProfilerCallback::g_hExitProcess;

//Thread local buffers (to avoid reallocating with each RecordFunction invocation that is made)
#define NAME_BUFFER_SIZE 512

thread_local WCHAR methodName[NAME_BUFFER_SIZE];
thread_local WCHAR typeName[NAME_BUFFER_SIZE];
thread_local WCHAR moduleName[NAME_BUFFER_SIZE];

thread_local WCHAR debugBuffer[2000];

void dprintf(LPCWSTR format, ...)
{
	va_list args;
	va_start(args, format);
	vswprintf_s(debugBuffer, format, args);
	va_end(args);
}

/// <summary>
/// A function that is called exactly once for each function that is JITted. Allows the profiler to report on the function,
/// and decide whether the function should be hooked or not.
/// </summary>
/// <param name="funcId">The ID of the function that is being JITted.</param>
/// <param name="clientData">The client data that was passed to ICorProfilerInfo3::SetFunctionIDMapper2()</param>
/// <param name="pbHookFunction">A value that must be set by this function indicating whether the function identified by funcId should be hooked or not.</param>
/// <returns>The original funcId that was passed into this function.</returns>
UINT_PTR __stdcall CCorProfilerCallback::RecordFunction(FunctionID funcId, void* clientData, BOOL* pbHookFunction)
{
	HRESULT hr = S_OK;

	ICorProfilerInfo3* pInfo = g_pProfiler->m_pInfo;
	IMetaDataImport* pMDI = nullptr;

	mdMethodDef methodDef;
	mdTypeDef typeDef;
	ModuleID moduleId;
	PCCOR_SIGNATURE pSigBlob = nullptr;
	ULONG cbSigBlob = 0;

	BOOL detailed;

	//Get the IMetaDataImport and mdMethodDef
	IfFailGo(pInfo->GetTokenAndMetaDataFromFunction(funcId, IID_IMetaDataImport, reinterpret_cast<IUnknown**>(&pMDI), &methodDef));

	//Get the ModuleID
	IfFailGo(pInfo->GetFunctionInfo2(funcId, NULL, NULL, &moduleId, NULL, 0, NULL, NULL));

	//Get the module name
	IfFailGo(pInfo->GetModuleInfo(moduleId, NULL, NAME_BUFFER_SIZE, NULL, moduleName, NULL));

	if (!ShouldHook())
	{
		*pbHookFunction = FALSE;
		goto Exit;
	}

	detailed = GetBoolEnv("DEBUGTOOLS_DETAILED");

	if (detailed)
	{
		//Get the method name, mdTypeDef and sigblob
		IfFailGo(pMDI->GetMethodProps(
			methodDef,
			&typeDef,
			methodName,
			NAME_BUFFER_SIZE,
			NULL,
			NULL,
			&pSigBlob,
			&cbSigBlob,
			NULL,
			NULL
		));
	}
	else
	{
		//Get the method name and mdTypeDef
		IfFailGo(pMDI->GetMethodProps(
			methodDef,
			&typeDef,
			methodName,
			NAME_BUFFER_SIZE,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL
		));
	}

	//Get the type name
	IfFailGo(pMDI->GetTypeDefProps(typeDef, typeName, NAME_BUFFER_SIZE, NULL, NULL, NULL));

	//Write the event

	if (detailed)
		EventWriteMethodInfoDetailedEvent(funcId, methodName, typeName, moduleName, methodDef, cbSigBlob, pSigBlob);
	else
		EventWriteMethodInfoEvent(funcId, methodName, typeName, moduleName);

ErrExit:
	if (pMDI)
		pMDI->Release();

	*pbHookFunction = true;

Exit:
	return funcId;
}

LPCWSTR blacklist[] = {
	L"mscorlib.dll",
	L"System.dll",
	L"System.Core.dll",
	L"System.Configuration.dll",
	L"System.Xml.dll",
	L"Microsoft.VisualStudio.Telemetry.dll",
	L"Newtonsoft.Json.dll",
	L"PresentationFramework.dll",
	L"PresentationCore.dll",
	L"WindowsBase.dll"
};

BOOL CCorProfilerCallback::ShouldHook()
{
	WCHAR* ptr = wcsrchr(moduleName, '\\');

	//Path doesn't contain a slash; assume we should hook it
	if (!ptr)
		return TRUE;

	ptr++;

	for (LPCWSTR &item : blacklist)
	{
		if (!lstrcmpiW(item, ptr))
			return FALSE;
	}

	return TRUE;
}

HRESULT CCorProfilerCallback::SetEventMask()
{
	DWORD flags = COR_PRF_MONITOR_ENTERLEAVE | COR_PRF_MONITOR_THREADS;

	//WithInfo hooks won't be called unless advanced event flags are set
	if (false)
		flags |= COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO;

	return m_pInfo->SetEventMask(flags);
}

HRESULT CCorProfilerCallback::InstallHooks()
{
	return m_pInfo->SetEnterLeaveFunctionHooks3(
		(FunctionEnter3*)EnterNaked,
		(FunctionLeave3*)LeaveNaked,
		(FunctionTailcall3*)TailcallNaked
	);
}

HRESULT CCorProfilerCallback::InstallHooksWithInfo()
{
	return m_pInfo->SetEnterLeaveFunctionHooks3WithInfo(
		(FunctionEnter3WithInfo*)EnterNakedWithInfo,
		(FunctionLeave3WithInfo*)LeaveNakedWithInfo,
		(FunctionTailcall3WithInfo*)TailcallNakedWithInfo
	);
}

HRESULT CCorProfilerCallback::BindLifetimeToParentProcess()
{
#define BUFFER_SIZE 100

	HANDLE hParentProcess;

	CHAR envBuffer[BUFFER_SIZE];
	DWORD parentProcessId;

	DWORD actualSize = GetEnvironmentVariableA("DEBUGTOOLS_PARENT_PID", envBuffer, BUFFER_SIZE);

	if (actualSize == 0 || actualSize >= BUFFER_SIZE)
		goto Exit;

	parentProcessId = strtol(envBuffer, NULL, 10);

	//The only access right that is mandatory is SYNCHRONIZE; without this
	//RegisterWaitForSingleObject will throw an exception
	hParentProcess = OpenProcess(SYNCHRONIZE, FALSE, parentProcessId);

	if(!RegisterWaitForSingleObject(
		&g_hExitProcess,
		hParentProcess,
		ExitProcessCallback,
		NULL,
		INFINITE,
		WT_EXECUTEONLYONCE
	))
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

Exit:
	return S_OK;
}

void NTAPI CCorProfilerCallback::ExitProcessCallback(
	_In_ PVOID   lpParameter,
	_In_ BOOLEAN TimerOrWaitFired
)
{
#pragma warning(push)
#pragma warning(disable: 6031) //return value ignored
	UnregisterWait(g_hExitProcess);
	ExitProcess(0);
#pragma warning(pop)
}

#pragma endregion