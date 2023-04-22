#include "pch.h"
#include "CStaticTracer.h"
#include "CCorProfilerCallback.h"
#include "CSigReader.h"
#include "CValueTracer.h"
#include "Events.h"
#include <vector>

void CStaticTracer::Trace(LPWSTR szName)
{
    HRESULT hr = S_OK;

    CClassInfo* pInfo = nullptr;
    mdFieldDef fieldDef = 0;
    CSigField* pField;
    CValueTracer tracer;
    void* pAddress;

    LPWSTR szType;
    LPWSTR szField;
    ULONG threadId;
    ULONG maxTraceDepth;
    BOOL exactTypeMatch;

    if (!g_pProfiler->m_Detailed)
    {
        hr = PROFILER_E_STATICFIELD_DETAILED_REQUIRED;
        goto ErrExit;
    }

    IfFailGo(ReadTraceRequest(szName, &szType, &szField, &threadId, &maxTraceDepth, &exactTypeMatch));

    if (maxTraceDepth != 0)
        tracer.m_MaxTraceDepth = maxTraceDepth;

    IfFailGo(GetClassInfo(szType, exactTypeMatch, &pInfo));
    IfFailGo(GetFieldToken(pInfo, szField, &fieldDef, &pField));

    IfFailGo(GetFieldAddress(pInfo, fieldDef, threadId, &pAddress));

    IfFailGo(tracer.GetFieldValue(pAddress, pInfo, pField));

ErrExit:
    ValidateETW(EventWriteStaticFieldValueEvent(hr, g_ValueBufferPosition, g_ValueBuffer));
}

HRESULT CStaticTracer::ReadTraceRequest(
    _In_ LPWSTR szName,
    _Out_ LPWSTR* szType,
    _Out_ LPWSTR* szField,
    _Out_ ULONG* threadId,
    _Out_ ULONG* maxTraceDepth,
    _Out_ BOOL* exactTypeMatch)
{
    HRESULT hr = S_OK;

    LPWSTR lastDot = wcsrchr(szName, L'.');
    LPWSTR firstPipe = nullptr;
    LPWSTR secondPipe = nullptr;

    if (lastDot)
        firstPipe = wcschr(lastDot + 1, '|');

    if (firstPipe)
        secondPipe = wcschr(firstPipe + 1, '|');

    if (!lastDot || !firstPipe || !secondPipe)
    {
        hr = PROFILER_E_STATICFIELD_INVALID_REQUEST;
        goto ErrExit;
    }

    *lastDot = '\0';
    *firstPipe = '\0';
    *secondPipe = '\0';

    lastDot++;
    firstPipe++;
    secondPipe++;

    if (*szName == '\0' || *lastDot == '\0' || *firstPipe == '\0')
    {
        hr = PROFILER_E_STATICFIELD_INVALID_REQUEST;
        goto ErrExit;
    }

    *szType = szName;
    *szField = lastDot;

    *threadId = _wtoi(firstPipe);
    *maxTraceDepth = _wtoi(secondPipe);

    *exactTypeMatch = wcschr(*szType, L'.') == nullptr ? FALSE : TRUE;

ErrExit:
    return hr;
}

HRESULT CStaticTracer::GetClassInfo(
    _In_ LPWSTR szType,
    _In_ BOOL exactTypeMatch,
    _Out_ CClassInfo** ppInfo)
{
    HRESULT hr = S_OK;
    CClassInfo* match = nullptr;
    std::vector<CClassInfo*> candidates;

    //Lock scope
    {
        CLock classLock(&g_pProfiler->m_ClassMutex);

        for (auto& item : g_pProfiler->m_ClassInfoMap)
        {
            if (item.second->m_InfoType == ClassInfoType::Class)
            {
                CClassInfo* pInfo = (CClassInfo*)item.second;

                if (exactTypeMatch)
                {
                    if (lstrcmpiW(pInfo->m_szName, szType) == 0)
                    {
                        match = pInfo;
                        break;
                    }
                }
                else
                {
                    //Get the string after the last dot and compare the typename ignoring namespace
                    LPWSTR name = wcsrchr(pInfo->m_szName, L'.');

                    if (name)
                    {
                        if (lstrcmpiW(name + 1, szType) == 0)
                            candidates.push_back(pInfo);
                    }
                }
            }
        }
    }

    if (exactTypeMatch)
    {
        if (match == nullptr)
            hr = PROFILER_E_STATICFIELD_CLASS_NOT_FOUND;
    }
    else
    {
        if (candidates.size() == 1)
            match = candidates[0];
        else
        {
            if (candidates.size() == 0)
                hr = PROFILER_E_STATICFIELD_CLASS_NOT_FOUND;
            else
                hr = PROFILER_E_STATICFIELD_CLASS_AMBIGUOUS;
        }
    }

    if (SUCCEEDED(hr))
        *ppInfo = match;

    return hr;
}

HRESULT CStaticTracer::GetFieldToken(
    _In_ CClassInfo* pInfo,
    _In_ LPWSTR szField,
    _Out_ mdFieldDef* fieldDef,
    _Out_ CSigField** ppField)
{
    HRESULT hr = S_OK;
    IMetaDataImport2* pMDI = nullptr;
    HCORENUM hEnum = nullptr;
    ULONG cTokens = 0;
    mdFieldDef* rFields = nullptr;

    //CClassInfo stores fields, however it only stores fields when GetClassLayout returns a field offset, implying it only stores
    //instance fields (which would make sense)

    IfFailGo(g_pProfiler->m_pInfo->GetModuleMetaData(pInfo->m_ModuleID, ofRead, IID_IMetaDataImport2, (IUnknown**)&pMDI));

    IfFailGo(pMDI->EnumFields(&hEnum, pInfo->m_TypeDef, nullptr, 0, &cTokens));

    IfFailGo(pMDI->CountEnum(hEnum, &cTokens));

    rFields = new mdFieldDef[cTokens];

    IfFailGo(pMDI->EnumFields(&hEnum, pInfo->m_TypeDef, rFields, cTokens, &cTokens));

    for (ULONG i = 0; i < cTokens; i++)
    {
        ULONG chField = 0;
        PCCOR_SIGNATURE pSigBlob;
        ULONG cbSigBlob = 0;

        IfFailGo(pMDI->GetFieldProps(
            rFields[i],
            NULL,
            g_szFieldName,
            NAME_BUFFER_SIZE,
            &chField,
            NULL,
            &pSigBlob,
            &cbSigBlob,
            NULL,
            NULL,
            NULL
        ));

        if (lstrcmpiW(g_szFieldName, szField) == 0)
        {
            *fieldDef = rFields[i];

            CSigReader reader(rFields[i], pMDI, pSigBlob);
            IfFailGo(reader.ParseField(szField, ppField));
            goto ErrExit;
        }
    }

    hr = PROFILER_E_STATICFIELD_FIELD_NOT_FOUND;

ErrExit:
    if (rFields)
        delete[] rFields;

    if (pMDI)
        pMDI->Release();

    return hr;
}

HRESULT CStaticTracer::GetFieldAddress(
    _In_ CClassInfo* pInfo,
    _In_ mdFieldDef fieldDef,
    _In_ ULONG targetThreadId,
    _Out_ void** ppAddress)
{
    HRESULT hr = S_OK;
    COR_PRF_STATIC_TYPE fieldType;
    AppDomainID appDomainId = 0;
    ThreadID threadId = 0;
    ContextID contextId = 0;
    void* pAddress = nullptr;

    IfFailGo(g_pProfiler->m_pInfo->GetStaticFieldInfo(pInfo->m_ClassID, fieldDef, &fieldType));
    switch (fieldType)
    {
    case COR_PRF_FIELD_NOT_A_STATIC:
        hr = PROFILER_E_STATICFIELD_NOT_STATIC;
        break;
    case COR_PRF_FIELD_APP_DOMAIN_STATIC:
        IfFailGo(GetAppDomainId(pInfo->m_ModuleID, &appDomainId));
        IfFailGo(g_pProfiler->m_pInfo->GetAppDomainStaticAddress(pInfo->m_ClassID, fieldDef, appDomainId, &pAddress));
        break;
    case COR_PRF_FIELD_THREAD_STATIC:
        IfFailGo(GetThreadId(targetThreadId , &threadId));
        IfFailGo(g_pProfiler->m_pInfo->GetThreadStaticAddress(pInfo->m_ClassID, fieldDef, threadId, &pAddress));
        break;
    case COR_PRF_FIELD_CONTEXT_STATIC:
        IfFailGo(GetContextId(targetThreadId , &contextId));
        g_pProfiler->m_pInfo->GetContextStaticAddress(pInfo->m_ClassID, fieldDef, contextId, &pAddress);
        break;
    case COR_PRF_FIELD_RVA_STATIC:
        IfFailGo(g_pProfiler->m_pInfo->GetRVAStaticAddress(pInfo->m_ClassID, fieldDef, &pAddress));
        break;
    default:
        hr = PROFILER_E_STATICFIELD_FIELDTYPE_UNKNOWN;
        break;
    }

    *ppAddress = pAddress;

ErrExit:
    switch (hr)
    {
    case E_NOTIMPL:
        hr = PROFILER_E_STATICFIELD_FIELDTYPE_NOT_SUPPORTED;
        break;

    case CORPROF_E_DATAINCOMPLETE:
        hr = PROFILER_E_STATICFIELD_NOT_INITIALIZED;
        break;
    }

    return hr;
}

HRESULT CStaticTracer::GetAppDomainId(
    _In_ ModuleID moduleId,
    _Out_ AppDomainID* appDomainId
)
{
    HRESULT hr = S_OK;
    ULONG32 cAppDomainIds = 0;

    IfFailGo(g_pProfiler->m_pInfo->GetAppDomainsContainingModule(
        moduleId,
        0,
        &cAppDomainIds,
        NULL
    ));

    if (cAppDomainIds != 1)
    {
        hr = PROFILER_E_STATICFIELD_MULTIPLE_APPDOMAIN;
        goto ErrExit;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetAppDomainsContainingModule(
        moduleId,
        cAppDomainIds,
        &cAppDomainIds,
        appDomainId
    ));

ErrExit:
    return hr;
}

HRESULT CStaticTracer::GetThreadId(
    _In_ ULONG targetThreadId,
    _Out_ ThreadID* threadId)
{
    if (targetThreadId == 0)
        return PROFILER_E_STATICFIELD_NEED_THREADID;

    HRESULT hr = S_OK;
    DWORD dwWin32ThreadId = 0;

    CLock threadLock(&g_pProfiler->m_ThreadIDToSequenceMutex);

    for (auto& item : g_pProfiler->m_ThreadIDToSequenceMap)
    {
        hr = g_pProfiler->m_pInfo->GetThreadInfo(item.first, &dwWin32ThreadId);

        if (FAILED(hr))
            return hr;

        if (targetThreadId == dwWin32ThreadId)
        {
            *threadId = item.first;
            return S_OK;
        }
    }

    return PROFILER_E_STATICFIELD_THREAD_NOT_FOUND;
}

HRESULT CStaticTracer::GetContextId(
    _In_ ULONG targetThreadId,
    _Out_ ContextID* contextId)
{
    HRESULT hr = S_OK;
    ThreadID threadId;

    IfFailGo(GetThreadId(targetThreadId, &threadId));

    //This is the only place ContextID appears to be referenced
    IfFailGo(g_pProfiler->m_pInfo->GetThreadContext(threadId, contextId));

ErrExit:
    return hr;
}