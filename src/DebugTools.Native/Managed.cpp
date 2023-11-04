#include "pch.h"
#include <metahost.h>
#include <CorError.h>

#pragma comment(lib, "mscoree.lib")

typedef struct _CallArgs {
    LPCWSTR pwzAssemblyPath;
    LPCWSTR pwzTypeName;
    LPCWSTR pwzMethodName;
    LPCWSTR pwzArgument;
} CallArgs;

HRESULT GetLoadedRuntime(
    _In_ ICLRMetaHost* pMetaHost,
    _Out_ ICLRRuntimeInfo** ppRuntimeInfo);

HRESULT LoadCLRRuntime(
    _In_ ICLRMetaHost* pMetaHost,
    _Out_ ICLRRuntimeInfo** ppRuntimeInfo);

typedef HRESULT (__stdcall *fnGetCLRRuntimeHost)(REFIID riid, IUnknown** ppUnk);

extern "C" STDMETHODIMP CallManaged(CallArgs* args)
{
    HRESULT hr = S_OK;
    ICLRMetaHost* pMetaHost = NULL;
    ICLRRuntimeInfo* pRuntimeInfo = NULL;
    ICLRRuntimeHost* pRuntimeHost = NULL;
    
    DWORD returnValue;

    HMODULE coreclr = GetModuleHandle(L"coreclr.dll");

    if (coreclr)
    {
        fnGetCLRRuntimeHost fnPtr = (fnGetCLRRuntimeHost) GetProcAddress(coreclr, "GetCLRRuntimeHost");

        IfFailGo(fnPtr(IID_ICLRRuntimeHost, (IUnknown**)&pRuntimeHost));
    }
    else
    {
        IfFailGo(CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&pMetaHost));
        IfFailGo(GetLoadedRuntime(pMetaHost, &pRuntimeInfo));
        IfFailGo(pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID*)&pRuntimeHost));
    }

    IfFailGo(pRuntimeHost->ExecuteInDefaultAppDomain(args->pwzAssemblyPath, args->pwzTypeName, args->pwzMethodName, args->pwzArgument, &returnValue));

ErrExit:
    if (pRuntimeHost)
        pRuntimeHost->Release();

    if (pRuntimeInfo)
        pRuntimeInfo->Release();

    if (pMetaHost)
        pMetaHost->Release();

    return hr;
}

HRESULT GetLoadedRuntime(
    _In_ ICLRMetaHost* pMetaHost,
    _Out_ ICLRRuntimeInfo** ppRuntimeInfo)
{
    HRESULT hr = S_OK;
    IUnknown* pUnk = NULL;
    ICLRRuntimeInfo* pRuntimeInfo = NULL;
    IEnumUnknown* pEnumerator = NULL;
    ULONG fetched = 0;

    //Is the runtime already loaded?
    IfFailGo(pMetaHost->EnumerateLoadedRuntimes(GetCurrentProcess(), &pEnumerator));

    while (pEnumerator->Next(1, &pUnk, &fetched) == S_OK)
    {
        hr = pUnk->QueryInterface(&pRuntimeInfo);

        pUnk->Release();
        pUnk = NULL;

        break;
    }

ErrExit:
    if (pEnumerator)
        pEnumerator->Release();

    if (pRuntimeInfo)
    {
        if (hr == S_OK)
            *ppRuntimeInfo = pRuntimeInfo;
        else
            pRuntimeInfo->Release();
    }
    else
        hr = HOST_E_CLRNOTAVAILABLE;

    return hr;
}

HRESULT LoadCLRRuntime(
    _In_ ICLRMetaHost* pMetaHost,
    _Out_ ICLRRuntimeInfo** ppRuntimeInfo)
{
    //No runtime is currently loaded

    HRESULT hr = S_OK;
    IUnknown* pUnk = NULL;
    ICLRRuntimeInfo* pRuntimeInfo = NULL;
    IEnumUnknown* pEnumerator = NULL;
    ULONG fetched = 0;

    IfFailGo(pMetaHost->EnumerateInstalledRuntimes(&pEnumerator));

    while (pEnumerator->Next(1, &pUnk, &fetched) == S_OK)
    {
        hr = pUnk->QueryInterface(&pRuntimeInfo);

        pUnk->Release();
        pUnk = NULL;

        if (SUCCEEDED(hr))
        {
            DWORD size = 0;

            pRuntimeInfo->GetVersionString(NULL, &size);

            LPWSTR buffer = (LPWSTR)malloc(size * 2);

            if (SUCCEEDED(pRuntimeInfo->GetVersionString(buffer, &size)))
            {
                if (!wcscmp(buffer, L"v4.0.30319"))
                {
                    break;
                }
            }

            free(buffer);
        }

        pRuntimeInfo->Release();
        pRuntimeInfo = NULL;
    }

ErrExit:
    if (pEnumerator)
        pEnumerator->Release();

    if (pRuntimeInfo)
    {
        if (hr == S_OK)
            *ppRuntimeInfo = pRuntimeInfo;
        else
            pRuntimeInfo->Release();
    }
    else
        hr = HOST_E_CLRNOTAVAILABLE;

    return hr;
}