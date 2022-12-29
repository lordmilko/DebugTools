#include "pch.h"
#include "CCorProfilerCallback.h"
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
    LPWSTR assemblyName = nullptr;

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

        IfFailGo(GetPublicKeyToken(pbPublicKey, cbPublicKey, &pbPublicKeyToken));

        IfFailGo(GetAssemblyName(
            chName,
            asmMetaData,
            (const BYTE*)pbPublicKeyToken,
            8,
            FALSE,
            &assemblyName
        ));
    }
    else
        pAssemblyInfo = asmMatch->second;

ErrExit:
    if (SUCCEEDED(hr))
    {
        CLock moduleLock(&m_ModuleMutex, true);

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
            m_AssemblyNameMap[pAssemblyInfo->m_szShortName] = pAssemblyInfo;
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

    IClassInfo* pClassInfo;
    IfFailGo(GetClassInfo(classId, &pClassInfo));

    //Lock scope
    {
        CLock classLock(&m_ClassMutex, true);

        m_ClassInfoMap[classId] = pClassInfo;
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
    }

    return S_OK;
}

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

    m_Detailed = GetBoolEnv("DEBUGTOOLS_DETAILED");

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

CCorProfilerCallback* g_pProfiler;
HANDLE CCorProfilerCallback::g_hExitProcess;

thread_local WCHAR debugBuffer[2000];

void dprintf(LPCWSTR format, ...)
{
    va_list args;
    va_start(args, format);
    vswprintf_s(debugBuffer, format, args);
    va_end(args);
    OutputDebugString(debugBuffer);
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
    }

    //Get the type name
    IfFailGo(pMDI->GetTypeDefProps(typeDef, g_szTypeName, NAME_BUFFER_SIZE, NULL, NULL, NULL));

    //Write the event

    if (g_pProfiler->m_Detailed)
        EventWriteMethodInfoDetailedEvent(funcId, g_szMethodName, g_szTypeName, g_szModuleName, methodDef, cbSigBlob, pSigBlob);
    else
        EventWriteMethodInfoEvent(funcId, g_szMethodName, g_szTypeName, g_szModuleName);

ErrExit:
    if (typeArgs && !methodSaved)
        free(typeArgs);

    if (pMDI)
        pMDI->Release();

    *pbHookFunction = true;

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
    WCHAR* ptr = wcsrchr(g_szModuleName, '\\');

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
    DWORD flags = COR_PRF_MONITOR_ENTERLEAVE | COR_PRF_MONITOR_THREADS | COR_PRF_DISABLE_ALL_NGEN_IMAGES;

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

HRESULT CCorProfilerCallback::GetClassInfo(
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

        IfFailGo(GetClassInfo(baseClassId, &pElementType));

        pClassInfo = new CArrayInfo(pElementType, baseElemType, cRank);
    }
    else if (hr == S_FALSE)
    {
        //It's not an array

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
            pClassInfo = new CStandardTypeInfo(knownType);
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