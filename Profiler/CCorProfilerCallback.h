#pragma once

#include "CAssemblyInfo.h"
#include "CCommunication.h"
#include "CClassInfo.h"
#include "CModuleInfo.h"
#include "CSigMethod.h"
#include <bcrypt.h>
#include <unordered_map>
#include <unordered_set>
#include <shared_mutex>
#include "CUnknownArray.h"
#include "CExceptionManager.h"
#include "CMatchItem.h"

#undef GetClassInfo

//Thread local buffers (to avoid reallocating with each RecordFunction invocation that is made)
#define NAME_BUFFER_SIZE 512

extern thread_local WCHAR g_szMethodName[NAME_BUFFER_SIZE];
extern thread_local WCHAR g_szTypeName[NAME_BUFFER_SIZE];
extern thread_local WCHAR g_szModuleName[NAME_BUFFER_SIZE];
extern thread_local WCHAR g_szAssemblyName[NAME_BUFFER_SIZE];
extern thread_local WCHAR g_szFieldName[NAME_BUFFER_SIZE];

#if _DEBUG && DEBUG_UNKNOWN
extern std::unordered_map<CUnknown*, BYTE>* g_UnknownMap;
#endif

class CCorProfilerCallback final : public ICorProfilerCallback3
{
public:
    //The singular profiler instance of this process
    static CCorProfilerCallback* g_pProfiler;
    static HANDLE g_hExitProcess;

    CCorProfilerCallback() :
        m_pInfo(nullptr),
        m_Detailed(FALSE),
        m_hHash(nullptr),
        m_RefCount(0)
    {
    }

    ~CCorProfilerCallback();

    //Performs a one time registration function for each unique function that is JITted
    static UINT_PTR __stdcall RecordFunction(FunctionID funcId, void* clientData, BOOL* pbHookFunction);
    static BOOL ShouldHook();
    static BOOL IsWhitelistedModule(LPWSTR moduleName);
    static void NTAPI ExitProcessCallback(_In_ PVOID   lpParameter, _In_ BOOLEAN TimerOrWaitFired);

    void GetMatchItems(
        _In_ LPCWSTR envVar,
        _In_ std::vector<CMatchItem>& items);

    void GetDefaultBlacklistItems(
        _In_ std::vector<CMatchItem>& items);

    HRESULT SetEventMask();
    HRESULT InstallHooks();
    HRESULT InstallHooksWithInfo();
    HRESULT BindLifetimeToParentProcess();

    void AddClassNoLock(IClassInfo* pClassInfo);

    HRESULT CreateClassInfo(
        _In_ ClassID classId,
        _Out_ IClassInfo** ppClassInfo);

    HRESULT GetClassInfoFromClassId(ClassID classId, IClassInfo** ppClassInfo, bool lock = true);

    HRESULT GetModuleInfo(
        _In_ ModuleID moduleId,
        _Out_ CModuleInfo** ppModuleInfo);

    HRESULT GetAssemblyName(
        _In_ ULONG chName,
        _In_ ASSEMBLYMETADATA& asmMetaData,
        _In_ const BYTE* pbPublicKeyOrToken,
        _In_ ULONG cbPublicKeyOrToken,
        _In_ BOOL isPublicKey,
        _Out_ LPWSTR* szAssemblyName);

    HRESULT GetPublicKeyToken(
        _In_ const void* pbPublicKey,
        _In_ ULONG cbPublicKey,
        _Out_ const void** ppbPublicKeyToken);

    CorElementType GetElementTypeFromClassName(LPWSTR szName);

    BOOL IsObjectIdBlacklisted(ObjectID objectId)
    {
        return m_ObjectIdBlacklist.find(objectId) != m_ObjectIdBlacklist.end();
    }

    BOOL IsHookedFunction(FunctionIDOrClientID functionId);

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
    STDMETHODIMP AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus) override;
    STDMETHODIMP ModuleLoadStarted(ModuleID moduleId) override { return S_OK; }
    STDMETHODIMP ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override { return S_OK; }
    STDMETHODIMP ModuleUnloadStarted(ModuleID moduleId) override { return S_OK; }
    STDMETHODIMP ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus) override;
    STDMETHODIMP ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID assemblyId) override;
    STDMETHODIMP ClassLoadStarted(ClassID classId) override { return S_OK; }
    STDMETHODIMP ClassLoadFinished(ClassID classId, HRESULT hrStatus) override;
    STDMETHODIMP ClassUnloadStarted(ClassID classId) override { return S_OK; }
    STDMETHODIMP ClassUnloadFinished(ClassID classId, HRESULT hrStatus) override;
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
    STDMETHODIMP UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
    STDMETHODIMP ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
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
    STDMETHODIMP ExceptionThrown(ObjectID thrownObjectId) override;
    STDMETHODIMP ExceptionSearchFunctionEnter(FunctionID functionId) override { return S_OK; }
    STDMETHODIMP ExceptionSearchFunctionLeave() override { return S_OK; }
    STDMETHODIMP ExceptionSearchFilterEnter(FunctionID functionId) override;
    STDMETHODIMP ExceptionSearchFilterLeave() override;
    STDMETHODIMP ExceptionSearchCatcherFound(FunctionID functionId) override { return S_OK; }
    STDMETHODIMP ExceptionOSHandlerEnter(FunctionID functionId) override { return S_OK; }
    STDMETHODIMP ExceptionOSHandlerLeave(FunctionID functionId) override { return S_OK; }
    STDMETHODIMP ExceptionUnwindFunctionEnter(FunctionID functionId) override;
    STDMETHODIMP ExceptionUnwindFunctionLeave() override;
    STDMETHODIMP ExceptionUnwindFinallyEnter(FunctionID functionId) override;
    STDMETHODIMP ExceptionUnwindFinallyLeave() override;
    STDMETHODIMP ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override;
    STDMETHODIMP ExceptionCatcherLeave() override;
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
    BOOL m_Detailed;

    std::unordered_map<AssemblyID, CAssemblyInfo*> m_AssemblyInfoMap;
    std::unordered_map<std::wstring_view, CAssemblyInfo*> m_AssemblyNameMap;
    std::unordered_map<std::wstring_view, CAssemblyInfo*> m_AssemblyShortNameMap;
    std::shared_mutex m_AssemblyMutex;

    std::unordered_map<ModuleID, CModuleInfo*> m_ModuleInfoMap;
    std::shared_mutex m_ModuleMutex;

    std::unordered_map<ClassID, IClassInfo*> m_ClassInfoMap;
    std::unordered_set<ClassID> m_CanonTypes;
    std::unordered_set<CClassInfo*> m_CanonicalGenericTypes;
    std::unordered_map<CorElementType, CStandardTypeInfo*> m_StandardTypeMap;
    std::unordered_map<ClassID, CUnknownArray<CArrayInfo>*> m_ArrayTypeMap;
    std::shared_mutex m_ClassMutex;

    std::unordered_map<FunctionID, CSigMethodDef*> m_MethodInfoMap;
    std::unordered_map<FunctionID, BYTE> m_HookedMethodMap;
    std::shared_mutex m_MethodMutex;

    std::unordered_map<ObjectID, BYTE> m_ObjectIdBlacklist;
    std::shared_mutex m_ObjectIdBlacklistMutex;

    std::vector<CMatchItem> m_ModuleBlacklist;
    std::vector<CMatchItem> m_ModuleWhitelist;

private:
    CCommunication m_Communication;
    BCRYPT_HASH_HANDLE m_hHash;

    CExceptionManager m_ExceptionManager;

    long m_RefCount;
};

#define g_pProfiler CCorProfilerCallback::g_pProfiler