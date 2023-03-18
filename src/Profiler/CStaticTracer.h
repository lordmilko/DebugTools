#pragma once

#undef GetClassInfo

class CClassInfo;
class CSigField;

class CStaticTracer
{
public:
    static void Trace(LPWSTR szName);

private:
    static HRESULT ReadTraceRequest(
        _In_ LPWSTR szName,
        _Out_ LPWSTR* szType,
        _Out_ LPWSTR* szField,
        _Out_ ULONG* threadId,
        _Out_ ULONG* maxTraceDepth,
        _Out_ BOOL* exactTypeMatch);

    static HRESULT GetClassInfo(
        _In_ LPWSTR szType,
        _In_ BOOL exactTypeMatch,
        _Out_ CClassInfo** ppInfo);

    static HRESULT GetFieldToken(
        _In_ CClassInfo* pInfo,
        _In_ LPWSTR szField,
        _Out_ mdFieldDef* fieldDef,
        _Out_ CSigField** ppField);

    static HRESULT GetFieldAddress(
        _In_ CClassInfo* pInfo,
        _In_ mdFieldDef fieldDef,
        _In_ ULONG targetThreadId,
        _Out_ void** ppAddress);

    static HRESULT GetAppDomainId(
        _In_ ModuleID moduleId,
        _Out_ AppDomainID* appDomainId
    );

    static HRESULT GetThreadId(
        _In_ ULONG targetThreadId,
        _Out_ ThreadID* threadId);

    static HRESULT GetContextId(
        _In_ ULONG targetThreadId,
        _Out_ ContextID* contextId);
};