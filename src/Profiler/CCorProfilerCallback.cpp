#include "pch.h"
#include "CCorProfilerCallback.h"
#include "CExceptionInfo.h"
#include "CSigReader.h"
#include "Hooks\Hooks.h"
#include <bcrypt.h>
#include <strsafe.h>

//Thread local buffers (to avoid reallocating with each RecordFunction invocation that is made)

thread_local WCHAR g_szMethodName[NAME_BUFFER_SIZE];
thread_local WCHAR g_szTypeName[NAME_BUFFER_SIZE];
thread_local WCHAR g_szModuleName[NAME_BUFFER_SIZE];
thread_local WCHAR g_szAssemblyName[NAME_BUFFER_SIZE];
thread_local WCHAR g_szFieldName[NAME_BUFFER_SIZE];

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

//We don't utilize AssemblyLoadFinished() or ModuleLoadFinished(); if the module was successfully loaded, we'll process it and its assembly in ModuleAttachedToAssembly()

#pragma region Transition

HRESULT CCorProfilerCallback::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    HRESULT hr = S_OK;

    EnsureTransitionMethodRecorded(functionId);

    if (!g_ExceptionQueue.empty())
    {
        ULONG oldSequence = g_Sequence;

        m_ExceptionManager.UnmanagedToManagedTransition(functionId, reason);

        //If LEAVE_FUNCTION was called, an ExceptionFrameUnwindEvent was executed, and both the profiler and the controller know
        //that the frame has been left. As such, there's nothing more we need to do here
        if (oldSequence != g_Sequence)
            goto ErrExit;
    }

    if (reason == COR_PRF_TRANSITION_CALL)
    {
        ENTER_FUNCTION(functionId, FrameKind::U2M);
        LogCall(L"U2M Call", functionId);
    }
    else
    {
        LEAVE_FUNCTION(functionId);
        LogCall(L"U2M Return", functionId);
    }

    if (!g_TracingEnabled)
        return hr;

    ValidateETW(EventWriteUnmanagedToManagedEvent(functionId, g_Sequence, reason));

ErrExit:
    return hr;
}

HRESULT CCorProfilerCallback::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    HRESULT hr = S_OK;

    EnsureTransitionMethodRecorded(functionId);

    if (!g_ExceptionQueue.empty())
    {
        ULONG oldSequence = g_Sequence;

        m_ExceptionManager.ManagedToUnmanagedTransition(functionId, reason);

        //If LEAVE_FUNCTION was called, an ExceptionFrameUnwindEvent was executed, and both the profiler and the controller know
        //that the frame has been left. As such, there's nothing more we need to do here
        if (oldSequence != g_Sequence)
            goto ErrExit;
    }

    if (reason == COR_PRF_TRANSITION_CALL)
    {
        ENTER_FUNCTION(functionId, FrameKind::M2U);
        LogCall(L"M2U Call", functionId);
    }
    else
    {
        LEAVE_FUNCTION(functionId);
        LogCall(L"M2U Return", functionId);
    }

    if (!g_TracingEnabled)
        return hr;

    ValidateETW(EventWriteManagedToUnmanagedEvent(functionId, g_Sequence, reason));

ErrExit:
    return hr;
}

#pragma endregion
#pragma region Load Events

HRESULT CCorProfilerCallback::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    //This method only executes in detailed profiling mode

    if (hrStatus != S_OK)
        return S_OK;

    CLock assemblyLock(&m_AssemblyMutex, true);

    auto match = m_AssemblyInfoMap.find(assemblyId);

    if (match != m_AssemblyInfoMap.end())
    {
        CAssemblyInfo* info = match->second;

        m_AssemblyInfoMap.erase(assemblyId);
        m_AssemblyShortNameMap.erase(info->m_szShortName);
        m_AssemblyNameMap.erase(info->m_szName);

        info->Release();
    }

    return S_OK;
}

HRESULT CCorProfilerCallback::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    //This method only executes in detailed profiling mode

    if (hrStatus != S_OK)
        return S_OK;

    CLock moduleLock(&m_ModuleMutex, true);
    CLock assemblyLock(&m_AssemblyMutex, true);

    auto moduleMatch = m_ModuleInfoMap.find(moduleId);

    if (moduleMatch != m_ModuleInfoMap.end())
    {
        CModuleInfo* info = moduleMatch->second;

        auto asmMatch = m_AssemblyInfoMap.find(info->m_AssemblyID);

        if (asmMatch != m_AssemblyInfoMap.end())
            asmMatch->second->RemoveModule(info);

        m_ModuleInfoMap.erase(moduleId);

        info->Release();
    }

    return S_OK;
}

HRESULT CCorProfilerCallback::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID assemblyId)
{
    //This method only executes in detailed profiling mode and the hrStatus passed to ModuleLoadFinished SUCCEEDED()

    HRESULT hr = S_OK;
    LPWSTR shortAsmName = nullptr;
    CAssemblyInfo* pAssemblyInfo = nullptr;
    IMetaDataImport2* pMDI = nullptr;
    IMetaDataAssemblyImport* pMDAI = nullptr;

    mdAssembly mdAssembly;
    const void* pbPublicKey = nullptr;
    ULONG cbPublicKey;
    ULONG chName;
    ASSEMBLYMETADATA asmMetaData;
    ZeroMemory(&asmMetaData, sizeof(ASSEMBLYMETADATA));
    const void* pbPublicKeyToken = nullptr;
    ULONG cbPublicKeyToken = 0;
    LPWSTR assemblyName = nullptr;

    /* CTypeRefResolver::Resolve will take a (shared) lock on m_ModuleMutex as long as it is executing.During
     * its execution, it may also take a lock on m_AssemblyMutex. If we were to lock m_ModuleMutex in the SUCCEEDED() block below,
     * the following sequence of events could transpire:
     * 
     * 1. CTypeRefResolver::Resolve locks m_ModuleMutex in shared mode
     * 2. ModuleAttachedToAssembly locks m_AssemblyMutex in exclusive mode
     * 3. CTypeRefResolver::ResolveAssemblyRef attempts to lock m_AssemblyMutex in shared mode, is blocked by exclusive lock in ModuleAttachedToAssembly
     * 4. ModuleAttachedToAssembly attempts to lock m_ModuleMutex, is blocked by shared lock in CTypeRefResolver::Resolve!
     * 
     * We workaround this by acquiring m_ModuleMutex BEFORE m_AssemblyMutex here in ModuleAttachedToAssembly, thus blocking us until CTypeRefResolver::Resolve
     * finishes executing
     */
    CLock moduleLock(&m_ModuleMutex, true);
    CLock assemblyLock(&m_AssemblyMutex, true);

    auto asmMatch = m_AssemblyInfoMap.find(assemblyId);

    IfFailGo(m_pInfo->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport2, (IUnknown**)&pMDI));

    if (asmMatch == m_AssemblyInfoMap.end())
    {
        pAssemblyInfo = nullptr;

        IfFailGo(pMDI->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&pMDAI));

        IfFailGo(pMDAI->GetAssemblyFromScope(&mdAssembly));

        IfFailGo(pMDAI->GetAssemblyProps(
            mdAssembly,
            &pbPublicKey,
            &cbPublicKey,
            NULL,
            g_szAssemblyName,
            NAME_BUFFER_SIZE,
            &chName,
            &asmMetaData,
            NULL
        ));

        shortAsmName = _wcsdup(g_szAssemblyName);

        if (cbPublicKey)
        {
            IfFailGo(GetPublicKeyToken(pbPublicKey, cbPublicKey, &pbPublicKeyToken));
            cbPublicKeyToken = 8;
        }

        IfFailGo(GetAssemblyName(
            chName,
            asmMetaData,
            (const BYTE*)pbPublicKeyToken,
            cbPublicKeyToken,
            FALSE,
            &assemblyName
        ));
    }
    else
        pAssemblyInfo = asmMatch->second;

ErrExit:
    if (SUCCEEDED(hr))
    {
        CModuleInfo* pModuleInfo = new CModuleInfo(assemblyId, moduleId, pMDI);

        m_ModuleInfoMap[moduleId] = pModuleInfo;

        if (!pAssemblyInfo)
        {
            pAssemblyInfo = new CAssemblyInfo(
                shortAsmName,
                g_szAssemblyName,
                (const BYTE*)pbPublicKey,
                cbPublicKey,
                (const BYTE*)pbPublicKeyToken,
                pMDAI
            );

            m_AssemblyInfoMap[assemblyId] = pAssemblyInfo;
            m_AssemblyShortNameMap[pAssemblyInfo->m_szShortName] = pAssemblyInfo;
            m_AssemblyNameMap[pAssemblyInfo->m_szName] = pAssemblyInfo;
        }

        pAssemblyInfo->AddModule(pModuleInfo);
    }
    else
    {
        if (assemblyName)
            free(assemblyName);

        if (pbPublicKeyToken)
            free((void*)pbPublicKeyToken);

        //If we couldn't retrieve our required metadata, remove the module from the cache as it won't do us any good
        dprintf(L"ModuleAttachedToAssembly failed with %d\n", hr);
    }

    if (shortAsmName)
        free(shortAsmName);

    if (pMDI)
        pMDI->Release();

    if (pMDAI)
        pMDAI->Release();

    return hr;
}

HRESULT CCorProfilerCallback::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    //This method only executes in detailed profiling mode

    if (hrStatus != S_OK)
        return S_OK;

    HRESULT hr = S_OK;

    //We may have force loaded this type prior to ClassLoadFinished being called; in this case, there's nothing to do

    {
        CLock classLock(&m_ClassMutex);

        if (m_ClassInfoMap.find(classId) != m_ClassInfoMap.end())
            goto ErrExit;
    }

    IClassInfo* pClassInfo;
    IfFailGo(CreateClassInfo(classId, &pClassInfo));

    //Lock scope. We need this scope because IfFailGo above will skip initialization of the classLock which we want to
    //declare after we've got the class info
    {
        CLock classLock(&m_ClassMutex, true);

        if (pClassInfo->m_InfoType == ClassInfoType::Class)
        {
            CClassInfo* info = (CClassInfo*)pClassInfo;

            if (wcscmp(L"System.__Canon", info->m_szName) == 0)
                m_CanonTypes.insert(classId);

            if (info->m_NumGenericTypeArgs > 0)
            {
                BOOL all = TRUE;

                for (ULONG i = 0; i < info->m_NumGenericTypeArgs; i++)
                {
                    if (m_CanonTypes.find(info->m_GenericTypeArgs[i]) == m_CanonTypes.end())
                    {
                        all = FALSE;
                        break;
                    }
                }

                if (all)
                {
                    info->AddRef();
                    {
                        info->m_IsCanonical = true;
                        m_CanonicalGenericTypes.insert(info);
                    }
                }
            }
        }

        AddClassNoLock(pClassInfo);
    }

ErrExit:
    return hr;
}

HRESULT CCorProfilerCallback::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    //This method only executes in detailed profiling mode

    if (hrStatus != S_OK)
        return S_OK;

    CLock classLock(&m_ClassMutex, true);

    auto match = m_ClassInfoMap.find(classId);

    if (match != m_ClassInfoMap.end())
    {
        m_ClassInfoMap.erase(classId);

        match->second->Release();

        if (match->second->m_InfoType == ClassInfoType::StandardType)
        {
            CStandardTypeInfo* std = (CStandardTypeInfo*)match->second;

            m_StandardTypeMap.erase(std->m_ElementType);
            std->Release();
        }

        if (m_CanonTypes.find(classId) != m_CanonTypes.end())
            m_CanonTypes.erase(classId);

        if (m_CanonicalGenericTypes.find((CClassInfo*) match->second) != m_CanonicalGenericTypes.end())
        {
            match->second->Release();
            m_CanonicalGenericTypes.erase((CClassInfo*)match->second);
        }
    }

    return S_OK;
}

#pragma endregion
#pragma region Exception Events

HRESULT CCorProfilerCallback::ExceptionThrown(ObjectID thrownObjectId)                        { return m_ExceptionManager.ExceptionThrown(thrownObjectId);    }

//SearchFilter
HRESULT CCorProfilerCallback::ExceptionSearchFilterEnter(FunctionID functionId)               { return m_ExceptionManager.SearchFilterEnter(functionId);      }
HRESULT CCorProfilerCallback::ExceptionSearchFilterLeave()                                    { return m_ExceptionManager.SearchFilterLeave();                }

//UnwindFunction
HRESULT CCorProfilerCallback::ExceptionUnwindFunctionEnter(FunctionID functionId)             { return m_ExceptionManager.UnwindFunctionEnter(functionId);    }
HRESULT CCorProfilerCallback::ExceptionUnwindFunctionLeave()                                  { return m_ExceptionManager.UnwindFunctionLeave();              }

//CatcherEnter
HRESULT CCorProfilerCallback::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) { return m_ExceptionManager.CatcherEnter(functionId, objectId); }
HRESULT CCorProfilerCallback::ExceptionCatcherLeave()                                         { return m_ExceptionManager.CatcherLeave();                     }

//UnwindFinally
HRESULT CCorProfilerCallback::ExceptionUnwindFinallyEnter(FunctionID functionId)              { return m_ExceptionManager.UnwindFinallyEnter(functionId);     }
HRESULT CCorProfilerCallback::ExceptionUnwindFinallyLeave()                                   { return m_ExceptionManager.UnwindFinallyLeave();               }

#pragma endregion

/// <summary>
/// Initializes the profiler, performing initial setup such as registering our event masks function mappers and function hooks.
/// </summary>
/// <param name="pICorProfilerInfoUnk">A clr!ProfToEEInterfaceImpl object that should be queried to retrieve an ICorProfilerInfo* interface.</param>
/// <returns>A HRESULT that indicates success or failure. In the event of failure the profiler and its DLL will be unloaded.</returns>
HRESULT CCorProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    if (GetBoolEnv("DEBUGTOOLS_WAITFORDEBUG"))
    {
        OutputDebugStringW(L"Waiting for debugger to attach...\n");

        while (!::IsDebuggerPresent())
            ::Sleep(100);
    }    

    m_Detailed = GetBoolEnv("DEBUGTOOLS_DETAILED");
    g_TracingEnabled = GetBoolEnv("DEBUGTOOLS_TRACESTART");

    GetMatchItems(L"DEBUGTOOLS_MODULEBLACKLIST", m_ModuleBlacklist);
    GetMatchItems(L"DEBUGTOOLS_MODULEWHITELIST", m_ModuleWhitelist);

    GetDefaultBlacklistItems(m_ModuleBlacklist);

    HRESULT hr = S_OK;

    BindLifetimeToParentProcess();
    IfFailGo(m_Communication.Initialize());

    IfFailGo(pICorProfilerInfoUnk->QueryInterface(&m_pInfo));

    IfFailGo(m_pInfo->SetFunctionIDMapper2(RecordFunction, nullptr));

    IfFailGo(SetEventMask());
    
    if (m_Detailed)
    {
        IfFailGo(CValueTracer::Initialize(m_pInfo));

        IfFailGo(HRESULT_FROM_NT(BCryptCreateHash(BCRYPT_SHA1_ALG_HANDLE, &m_hHash, NULL, 0, NULL, 0, NULL)));
        IfFailGo(InstallHooksWithInfo());
    }
    else
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
    HRESULT hr = S_OK;
    ValidateETW(EventWriteShutdownEvent());
    return hr;
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

    ValidateETW(EventWriteThreadCreateEvent(win32ThreadId));

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

    ValidateETW(EventWriteThreadDestroyEvent(win32ThreadId));

ErrExit:
    return hr;
}

#pragma endregion
#pragma region ICorProfilerCallback2

HRESULT CCorProfilerCallback::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR* name)
{
    HRESULT hr = S_OK;

    WCHAR copy[100];

    //MSDN states the name is not guaranteed to be null terminated, so we make a copy just in case
    StringCchCopyN(copy, 100, name, cchName);
    copy[cchName + 1] = '\0';

    ValidateETW(EventWriteThreadNameEvent(copy));

    return hr;
}

#pragma endregion
#pragma region CCorProfilerCallback

CCorProfilerCallback* g_pProfiler;
HANDLE CCorProfilerCallback::g_hExitProcess;

CCorProfilerCallback::~CCorProfilerCallback()
{
    for (auto const& kv : m_MethodInfoMap)
        kv.second->Release();

    for (auto const& kv : m_ClassInfoMap)
        kv.second->Release();

    for (auto const& kv : m_ModuleInfoMap)
        kv.second->Release();

    for (auto const& kv : m_AssemblyInfoMap)
        kv.second->Release();

    for (auto const& kv : m_StandardTypeMap)
        kv.second->Release();

    for (auto const& item : m_CanonicalGenericTypes)
        item->Release();

    for (auto const& kv : m_ArrayTypeMap)
        delete kv.second;

    if (m_pInfo)
        m_pInfo->Release();

    if (m_hHash)
        BCryptDestroyHash(m_hHash);

#if _DEBUG
    _ASSERTE(g_ExceptionQueue.empty());
#endif

    for (auto const& kv : g_ExceptionQueue)
        delete kv;

#if _DEBUG && DEBUG_UNKNOWN
    _ASSERTE(g_UnknownMap->size() == 1); //+1 for CSigType Sentinel which is a static member
#endif
}

void CCorProfilerCallback::GetMatchItems(
    _In_ LPCWSTR envVar,
    _In_ std::vector<CMatchItem>& items)
{

#define MATCH_BUFFER_SIZE 4000

#define READBUFFER do { ptr++; \
    if ((ptr - szBuffer) > length) \
        return; } while(0)

    WCHAR szBuffer[MATCH_BUFFER_SIZE];
    int length = GetEnvironmentVariable(envVar, szBuffer, MATCH_BUFFER_SIZE);

    if (length == 0 || length >= MATCH_BUFFER_SIZE)
        return;

    WCHAR* ptr = szBuffer;

    while (true)
    {
        MatchKind matchKind = (MatchKind)*(WCHAR*)ptr;

        READBUFFER;

        WCHAR* strStart = ptr;

        while (*ptr != '\t')
            READBUFFER;

        *ptr = '\0';

        LPWSTR str = _wcsdup(strStart);

        items.emplace_back(CMatchItem());
        CMatchItem& item = items[items.size() - 1];
        item.m_MatchKind = matchKind;
        item.m_szValue = str;

        READBUFFER;

        //Two \t's in a row. It's the last one!
        if (*ptr == '\t')
            break;
    }
}

LPCWSTR blacklistModules[] = {
    //.NET Framework
    L"mscorlib.dll",
    L"System.dll",
    L"System.Core.dll",
    L"System.Configuration.dll",
    L"System.Xml.dll",
    L"Microsoft.VisualStudio.Telemetry.dll",
    L"Newtonsoft.Json.dll",
    L"PresentationFramework.dll",
    L"PresentationCore.dll",
    L"WindowsBase.dll",

    //.NET Core
    L"System.Private.CoreLib.dll"
};

LPCWSTR blacklistPaths[] = {
    L"dotnet\\shared\\Microsoft.NETCore.App",
    L"dotnet\\sdk\\",

#ifdef _DEBUG
    L"coreclr\\windows.x64.Debug"
#endif
};

void CCorProfilerCallback::GetDefaultBlacklistItems(
    _In_ std::vector<CMatchItem>& items)
{
    BOOL value = GetBoolEnv("DEBUGTOOLS_IGNORE_DEFAULT_BLACKLIST");

    if (value)
        return;

    for (LPCWSTR& str : blacklistPaths)
    {
        items.emplace_back(CMatchItem());

        CMatchItem& item = items[items.size() - 1];
        item.m_MatchKind = MatchKind::Contains;
        item.m_szValue = _wcsdup(str);
    }

    for (LPCWSTR& str : blacklistModules)
    {
        items.emplace_back(CMatchItem());

        CMatchItem& item = items[items.size() - 1];
        item.m_MatchKind = MatchKind::ModuleName;
        item.m_szValue = _wcsdup(str);
    }
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
    IMetaDataImport2* pMDI = nullptr;

    mdMethodDef methodDef;
    mdTypeDef typeDef;
    ModuleID moduleId;
    ClassID* typeArgs = 0;
    PCCOR_SIGNATURE pSigBlob = nullptr;
    ULONG cbSigBlob = 0;

    CSigMethodDef* method = nullptr;
    BOOL methodSaved = FALSE;

    *pbHookFunction = FALSE;

    //Get the IMetaDataImport and mdMethodDef
    IfFailGo(pInfo->GetTokenAndMetaDataFromFunction(funcId, IID_IMetaDataImport, reinterpret_cast<IUnknown**>(&pMDI), &methodDef));

    /* Get the ModuleIDand any type arguments.Due to the fact reference types will tend to share a single generic type definition,
     * in order to get proper typeArg information, we need to have a COR_PRF_FRAME_INFO, which we'll only have during EnterWithInfo().
     * While we don't query for generic info here, we'll know if a method is generic thanks to m_NumGenericTypeArgNames. For more info on generics,
     * see https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/docs/design/coreclr/profiling/davbr-blog-archive/Generics%20and%20Your%20Profiler.md */
    IfFailGo(pInfo->GetFunctionInfo2(
        funcId,     //[in] funcId
        NULL,       //[in] frameInfo
        NULL,       //[out] pClassId
        &moduleId,  //[out] pModuleId
        NULL,       //[out] pToken
        0,          //[in] cTypeArgs
        NULL, //[out] pcTypeArgs
        NULL        //[out] typeArgs
    ));

    //Get the module name
    IfFailGo(pInfo->GetModuleInfo(moduleId, NULL, NAME_BUFFER_SIZE, NULL, g_szModuleName, NULL));

    if (!ShouldHook())
    {
        LogShouldHook(L"Not tracing " FORMAT_PTR "\n", funcId);
        *pbHookFunction = FALSE;
        goto ErrExit;
    }

    if (g_pProfiler->m_Detailed)
    {
        //Get the method name, mdTypeDef and sigblob
        IfFailGo(pMDI->GetMethodProps(
            methodDef,
            &typeDef,
            g_szMethodName,
            NAME_BUFFER_SIZE,
            NULL,
            NULL,
            &pSigBlob,
            &cbSigBlob,
            NULL,
            NULL
        ));

        CSigReader reader(methodDef, pMDI, pSigBlob);

        if (reader.ParseMethod(g_szMethodName, TRUE, (CSigMethod**)&method) == S_OK)
        {
            method->m_ModuleID = moduleId;

            CLock methodMutex(&g_pProfiler->m_MethodMutex, true);

            g_pProfiler->m_MethodInfoMap[funcId] = method;
            g_pProfiler->m_HookedMethodMap.insert(funcId);

            methodSaved = TRUE;
        }
    }
    else
    {
        //Get the method name and mdTypeDef
        IfFailGo(pMDI->GetMethodProps(
            methodDef,
            &typeDef,
            g_szMethodName,
            NAME_BUFFER_SIZE,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL
        ));

        CLock methodMutex(&g_pProfiler->m_MethodMutex, true);
        g_pProfiler->m_HookedMethodMap.insert(funcId);
    }

    //Get the type name
    IfFailGo(pMDI->GetTypeDefProps(typeDef, g_szTypeName, NAME_BUFFER_SIZE, NULL, NULL, NULL));

    //Write the event

    LogShouldHook(L"Tracing %s " FORMAT_PTR "\n", g_szMethodName, funcId);
    *pbHookFunction = true;

    if (g_pProfiler->m_Detailed)
        ValidateETW(EventWriteMethodInfoDetailedEvent(funcId, g_szMethodName, g_szTypeName, g_szModuleName, methodDef, cbSigBlob, pSigBlob));
    else
        ValidateETW(EventWriteMethodInfoEvent(funcId, g_szMethodName, g_szTypeName, g_szModuleName));

ErrExit:
    if (FAILED(hr) && !*pbHookFunction)
    {
        LogShouldHook(L"Not tracing " FORMAT_PTR " due to HRESULT 0x%X\n", funcId, hr);
    }

    if (typeArgs && !methodSaved)
        free(typeArgs);

    if (pMDI)
        pMDI->Release();

    return funcId;
}

BOOL CCorProfilerCallback::ShouldHook()
{
    WCHAR* ptr = wcsrchr(g_szModuleName, '\\');

    //Path doesn't contain a slash; assume we should hook it
    if (!ptr)
        return TRUE;

    ptr++;

    for(size_t i = 0; i < g_pProfiler->m_ModuleBlacklist.size(); i++)
    {
        CMatchItem& item = g_pProfiler->m_ModuleBlacklist[i];

        if (item.IsMatch(item.m_MatchKind == MatchKind::ModuleName ? ptr : g_szModuleName))
        {
            if (IsWhitelistedModule(ptr))
                return TRUE;

            return FALSE;
        }
    }

    return TRUE;
}

BOOL CCorProfilerCallback::IsWhitelistedModule(LPWSTR moduleName)
{
    for (size_t i = 0; i < g_pProfiler->m_ModuleWhitelist.size(); i++)
    {
        CMatchItem& item = g_pProfiler->m_ModuleWhitelist[i];

        if (item.IsMatch(item.m_MatchKind == MatchKind::ModuleName ? moduleName : g_szModuleName))
            return TRUE;
    }

    return FALSE;
}

HRESULT CCorProfilerCallback::SetEventMask()
{
    DWORD flags =
        COR_PRF_MONITOR_ENTERLEAVE       | //Inject Enter/Leave/Tailcall hooks during JIT
        COR_PRF_MONITOR_EXCEPTIONS       | //Leave won't be called when an exception occurs, so we must unwind ourselves
        COR_PRF_MONITOR_THREADS          | //Record basic thread information
        COR_PRF_MONITOR_CODE_TRANSITIONS | //Track code transitions (but only when an exception is active) to detect when an exception is caught in unmanaged code
        COR_PRF_DISABLE_ALL_NGEN_IMAGES;   //Don't use NGEN images (we need a fresh JIT to be able to inject our Enter/Leave/Tailcall hooks)

    //WithInfo hooks won't be called unless advanced event flags are set
    if (m_Detailed)
    {
        flags |= COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO; //Detailed frame info

        flags |= COR_PRF_MONITOR_ASSEMBLY_LOADS; //Record assemblies for resolving mdTypeRef -> mdAssemblyRef -> CAssemblyInfo -> CModuleInfo -> ModuleID + mdtypeDef
        flags |= COR_PRF_MONITOR_MODULE_LOADS;   //Record modules for resolving mdTypeRefs
        flags |= COR_PRF_MONITOR_CLASS_LOADS;    //Record known classes for looking up their structure when getting their field's values
    }

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

void CCorProfilerCallback::AddClassNoLock(IClassInfo* pClassInfo)
{
    m_ClassInfoMap[pClassInfo->m_ClassID] = pClassInfo;

    if (pClassInfo->m_InfoType == ClassInfoType::Class)
    {
        CClassInfo* info = (CClassInfo*)pClassInfo;

        if (m_StandardTypeMap.find(ELEMENT_TYPE_OBJECT) == m_StandardTypeMap.end() && _wcsnicmp(info->m_szName, L"System.Object", sizeof(L"System.Object") / sizeof(WCHAR)) == 0)
        {
            m_StandardTypeMap[ELEMENT_TYPE_OBJECT] = new CStandardTypeInfo(info->m_ClassID, ELEMENT_TYPE_OBJECT);
        }
    }
    else if (pClassInfo->m_InfoType == ClassInfoType::StandardType)
    {
        CStandardTypeInfo* std = (CStandardTypeInfo*)pClassInfo;
        std->AddRef();

        m_StandardTypeMap[std->m_ElementType] = std;
    }
    else if (pClassInfo->m_InfoType == ClassInfoType::Array)
    {
        CArrayInfo* arr = (CArrayInfo*)pClassInfo;

        ClassID elmClassID = arr->m_pElementType->m_ClassID;

        auto match = m_ArrayTypeMap.find(elmClassID);

        if (match == m_ArrayTypeMap.end())
        {
            CUnknownArray<CArrayInfo>* elmArray = new CUnknownArray<CArrayInfo>();

            elmArray->Add(arr);

            m_ArrayTypeMap[elmClassID] = elmArray;
        }
        else
        {
            match->second->Add(arr);
        }
    }
}

HRESULT CCorProfilerCallback::CreateClassInfo(
    _In_ ClassID classId,
    _Out_ IClassInfo** ppClassInfo)
{
    HRESULT hr = S_OK;

    IMetaDataImport2* pMDI = nullptr;

    ModuleID moduleId;
    mdTypeDef typeDef;
    ULONG32 cNumTypeArgs = 0;
    ClassID* typeArgs = nullptr;

    CorElementType baseElemType;
    ClassID baseClassId;
    ULONG cRank;

    ULONG cFieldOffset = 0;
    COR_FIELD_OFFSET* rFieldOffset = nullptr;

    PCCOR_SIGNATURE pSigBlob;
    ULONG cbSigBlob;

    CSigField** fields = nullptr;

    ULONG i = 0;

    IClassInfo* pClassInfo = nullptr;
    IClassInfo* pElementType = nullptr;

    hr = m_pInfo->IsArrayClass(classId, &baseElemType, &baseClassId, &cRank);

    if (FAILED(hr))
        goto ErrExit;

    if (hr == S_OK)
    {
        //It's an array

        IfFailGo(CreateClassInfo(baseClassId, &pElementType));

        pClassInfo = new CArrayInfo(classId, pElementType, baseElemType, cRank);
    }
    else if (hr == S_FALSE)
    {
        //It's not an array

        /* A ClassID is a TypeHandle, which can point to either
         * A MethodTable or a TypeDesc.
         * At the present time a TypeHandle can point at two possible things
         * 
         *     1) A MethodTable    (Arrays, Intrinsics, Classes, Value Types and their instantiations)
         *     2) A TypeDesc       (all other cases: byrefs, pointer types, function pointers, generic type variables)
         * 
         * GetClassIDInfo2 will return CORPROF_E_CLASSID_IS_COMPOSITE
         * if the ClassID points to a TypeDesc, and GetClassIDInfo will simply
         * do _nothing_ if given the ClassID of a TypeDesc. An example type that
         * will generate CORPROF_E_CLASSID_IS_COMPOSITE is a pointer array (i.e. int*[])
         */
        IfFailGo(m_pInfo->GetClassIDInfo2(
            classId,
            &moduleId,
            &typeDef,
            NULL,
            0,
            &cNumTypeArgs,
            NULL
        ));

        if (cNumTypeArgs)
        {
            typeArgs = new ClassID[cNumTypeArgs];

            IfFailGo(m_pInfo->GetClassIDInfo2(
                classId,
                &moduleId,
                &typeDef,
                NULL,
                cNumTypeArgs,
                &cNumTypeArgs,
                typeArgs
            ));
        }

        IfFailGo(m_pInfo->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport2, (IUnknown**)&pMDI));

        IfFailGo(pMDI->GetTypeDefProps(typeDef, g_szTypeName, NAME_BUFFER_SIZE, NULL, NULL, NULL));

        CorElementType knownType = GetElementTypeFromClassName(g_szTypeName);

        if (knownType != ELEMENT_TYPE_END)
        {
            //When we have an array of strings, baseElemType reports the type as ELEMENT_TYPE_CLASS. GetClassLayout will return E_INVALIDARG
            //if you attempt to query a classId of a string. For other types, such as Int32, either these types have been boxed, or we're looking at the class ID of some generic type args; either way we'll record their boxed type so we can unbox them later
            pClassInfo = new CStandardTypeInfo(classId, knownType);
            goto ErrExit;
        }

        IfFailGo(m_pInfo->GetClassLayout(classId, NULL, 0, &cFieldOffset, NULL));

        if (cFieldOffset)
        {
            rFieldOffset = new COR_FIELD_OFFSET[cFieldOffset];

            IfFailGo(m_pInfo->GetClassLayout(classId, rFieldOffset, cFieldOffset, &cFieldOffset, NULL));

            fields = new CSigField*[cFieldOffset];

            for (; i < cFieldOffset; i++)
            {
                mdFieldDef fieldDef = rFieldOffset[i].ridOfField;

                IfFailGo(pMDI->GetFieldProps(
                    fieldDef,
                    NULL,
                    g_szFieldName,
                    NAME_BUFFER_SIZE,
                    NULL,
                    NULL,
                    &pSigBlob,
                    &cbSigBlob,
                    NULL,
                    NULL,
                    NULL
                ));

                CSigReader reader(fieldDef, pMDI, pSigBlob);

                CSigField* sigField;

                IfFailGo(reader.ParseField(g_szFieldName, &sigField));

                fields[i] = sigField;
            }
        }

        pClassInfo = new CClassInfo(
            g_szTypeName,
            classId,
            moduleId,
            typeDef,
            cFieldOffset,
            fields,
            rFieldOffset,
            cNumTypeArgs,
            typeArgs
        );
        typeArgs = nullptr; //Clear this out in case there's an error after this so we don't double free it (CClassInfo will free it in its destructor)
    }

ErrExit:
    if (SUCCEEDED(hr))
    {
        *ppClassInfo = pClassInfo;
    }
    else
    {
        if (fields)
        {
            for (ULONG j = 0; j < i; j++)
            {
                fields[i]->Release();
            }

            delete fields;
        }

        if (rFieldOffset)
            delete rFieldOffset;

        if (pElementType)
            pElementType->Release();

        if (pClassInfo)
            pClassInfo->Release();
    }

    if (pMDI)
        pMDI->Release();

    return hr;
}

HRESULT CCorProfilerCallback::GetClassInfoFromClassId(ClassID classId, IClassInfo** ppClassInfo, bool lock)
{
    HRESULT hr = S_OK;
    IClassInfo* pClassInfo = nullptr;

    CLock* classLock = nullptr;

    if (lock)
        classLock = new CLock(&m_ClassMutex, true);

    auto match = m_ClassInfoMap.find(classId);

    if (match == m_ClassInfoMap.end())
    {
        //Array types don't seem to hit ClassLoadFinished, so if we got an unknown type it's probably because it's an array

        IfFailGo(CreateClassInfo(classId, &pClassInfo));

        //We already hold the class lock (see above)
        AddClassNoLock(pClassInfo);
    }
    else
        pClassInfo = match->second;

ErrExit:
    if (classLock)
        delete classLock;

    if (SUCCEEDED(hr))
        *ppClassInfo = pClassInfo;

    return hr;
}

HRESULT CCorProfilerCallback::GetModuleInfo(_In_ ModuleID moduleId, _Out_ CModuleInfo** ppModuleInfo)
{
    HRESULT hr = S_OK;

    auto match = g_pProfiler->m_ModuleInfoMap.find(moduleId);

    if (match == g_pProfiler->m_ModuleInfoMap.end())
    {
        hr = PROFILER_E_MISSING_MODULE;
        goto ErrExit;
    }

    *ppModuleInfo = match->second;

ErrExit:
    return hr;
}

HRESULT CCorProfilerCallback::GetAssemblyName(
    _In_ ULONG chName,
    _In_ ASSEMBLYMETADATA& asmMetaData,
    _In_ const BYTE* pbPublicKeyOrToken,
    _In_ ULONG cbPublicKeyOrToken,
    _In_ BOOL isPublicKey,
    _Out_ LPWSTR* szAssemblyName)
{
    HRESULT hr = S_OK;
    const BYTE* pbTempPublicKeyToken = nullptr;

    chName--; //Ignore the null terminator

    chName += swprintf_s(
        g_szAssemblyName + chName,
        NAME_BUFFER_SIZE - chName,
        L", Version=%d.%d.%d.%d, Culture=",
        asmMetaData.usMajorVersion,
        asmMetaData.usMinorVersion,
        asmMetaData.usBuildNumber,
        asmMetaData.usRevisionNumber
    );

    chName += swprintf_s(
        g_szAssemblyName + chName,
        NAME_BUFFER_SIZE - chName,
        L"%s",
        asmMetaData.szLocale == nullptr ? L"neutral" : asmMetaData.szLocale
    );

    if (isPublicKey)
    {
        IfFailGo(GetPublicKeyToken(pbPublicKeyOrToken, cbPublicKeyOrToken, (const void**)&pbTempPublicKeyToken));

        pbPublicKeyOrToken = pbTempPublicKeyToken;
        cbPublicKeyOrToken = 8;
    }

    chName += swprintf_s(
        g_szAssemblyName + chName,
        NAME_BUFFER_SIZE - chName,
        L", PublicKeyToken="
    );

    if (cbPublicKeyOrToken)
    {
        for (ULONG i = 0; i < cbPublicKeyOrToken; i++)
        {
            chName += swprintf_s(
                g_szAssemblyName + chName,
                NAME_BUFFER_SIZE - chName,
                L"%2.2x",
                pbPublicKeyOrToken[i]
            );
        }
    }
    else
    {
        chName += swprintf_s(
            g_szAssemblyName + chName,
            NAME_BUFFER_SIZE - chName,
            L"null"
        );
    }

    *szAssemblyName = _wcsdup(g_szAssemblyName);

ErrExit:
    if (pbTempPublicKeyToken)
        free((void*)pbTempPublicKeyToken);

    return hr;
}

HRESULT CCorProfilerCallback::GetPublicKeyToken(
    _In_ const void* pbPublicKey,
    _In_ ULONG cbPublicKey,
    _Out_ const void** ppbPublicKeyToken)
{
    NTSTATUS ntStatus = 0;
    BYTE* pbPublicKeyToken;

    //A SHA-1 hash is 20 bytes. This saves us from having to call BCryptGetProperty with BCRYPT_HASH_LENGTH
    BYTE hashBuffer[20];
    ULONG bufferLength = sizeof(hashBuffer);

    ntStatus = BCryptHashData(m_hHash, (BYTE*)pbPublicKey, cbPublicKey, 0);

    if (!BCRYPT_SUCCESS(ntStatus))
        goto ErrExit;

    ntStatus = BCryptFinishHash(m_hHash, hashBuffer, bufferLength, 0);

    pbPublicKeyToken = (BYTE*)malloc(8);

    for(ULONG i = 0; i < 8; i++)
    {
        pbPublicKeyToken[i] = hashBuffer[bufferLength - 1 - i];
    }

    *ppbPublicKeyToken = pbPublicKeyToken;

ErrExit:
    return HRESULT_FROM_NT(ntStatus);
}

#define CHECK_ELEMENT_TYPE(name, elementType) \
    if (_wcsnicmp(szName, name, sizeof(name) / sizeof(WCHAR)) == 0) \
        return elementType

CorElementType CCorProfilerCallback::GetElementTypeFromClassName(LPWSTR szName)
{
    CHECK_ELEMENT_TYPE(L"System.Boolean", ELEMENT_TYPE_BOOLEAN);
    CHECK_ELEMENT_TYPE(L"System.Char", ELEMENT_TYPE_CHAR);
    CHECK_ELEMENT_TYPE(L"System.SByte", ELEMENT_TYPE_I1);
    CHECK_ELEMENT_TYPE(L"System.Byte", ELEMENT_TYPE_U1);
    CHECK_ELEMENT_TYPE(L"System.Int16", ELEMENT_TYPE_I2);
    CHECK_ELEMENT_TYPE(L"System.UInt16", ELEMENT_TYPE_U2);
    CHECK_ELEMENT_TYPE(L"System.Int32", ELEMENT_TYPE_I4);
    CHECK_ELEMENT_TYPE(L"System.UInt32", ELEMENT_TYPE_U4);
    CHECK_ELEMENT_TYPE(L"System.Int64", ELEMENT_TYPE_I8);
    CHECK_ELEMENT_TYPE(L"System.UInt64", ELEMENT_TYPE_U8);
    CHECK_ELEMENT_TYPE(L"System.Single", ELEMENT_TYPE_R4);
    CHECK_ELEMENT_TYPE(L"System.Double", ELEMENT_TYPE_R8);
    CHECK_ELEMENT_TYPE(L"System.IntPtr", ELEMENT_TYPE_I);
    CHECK_ELEMENT_TYPE(L"System.UIntPtr", ELEMENT_TYPE_U);
    CHECK_ELEMENT_TYPE(L"System.String", ELEMENT_TYPE_STRING);

    //ELEMENT_TYPE_OBJECT is intentionally not listed here; ELEMENT_TYPE_OBJECT goes to TraceClass() anyway, so flagging object as a "known type" will result in an infinite loop
    //as TraceClass() repeatedly tries to dispatch the element type to itself

    return ELEMENT_TYPE_END;
}

BOOL CCorProfilerCallback::IsHookedFunction(FunctionID functionId)
{
    CLock methodLock(&m_MethodMutex);

    return m_HookedMethodMap.find(functionId) != m_HookedMethodMap.end();
}

void CCorProfilerCallback::EnsureTransitionMethodRecorded(FunctionID functionId)
{
    /* Certain transition stubs(such as COMToCLR and CLRToCOM) are meaningless, however when the COM method(or the P / Invoke definition is actually invoked)
     * there is a second helper frame that is called (I think it may be the one that gets inlined). These methods DO exist in our metadata (i.e. the COM interface method or the P/Invoke definition),
     * however the function mapper won't be called for these methods (which makes sense, since they're special frames). As such, we need to record them ourselves. */

    CLock transitionLock(&m_TransitionMutex, true);

    if (m_TransitionMap.find(functionId) == m_TransitionMap.end())
    {
        BOOL isHooked = IsHookedFunction(functionId);

        if (!isHooked)
        {
            BOOL hook;
            RecordFunction(functionId, nullptr, &hook);
        }

        m_TransitionMap.insert(functionId);
    }
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