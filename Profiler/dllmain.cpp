// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "CClassFactory.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

/* In .NET Framework, profiling can be enabled for an application by defining the following environment variables
 * prior to launching it
 *     COR_ENABLE_PROFILING=1
 *     COR_PROFILER={9FA9EA80-BE5D-419E-A667-15A672CBD280}
 *     COR_PROFILER_PATH_32/COR_PROFILER_PATH_64=$(TargetPath)
 * 
 * The GUID specified to COR_PROFILER is passed as the rclsid to DllGetClassObject(). */
class __declspec(uuid("9FA9EA80-BE5D-419E-A667-15A672CBD280")) Profiler;

/// <summary>
/// Retrieves the IClassFactory that should be used for creating an ICorProfilerCallback*.
/// </summary>
/// <param name="rclsid">The class identifier of the profiler to be created. This value is specified to the COR_PROFILER environment variable prior to launching the application to be profiled.</param>
/// <param name="riid">The identifier of the interface that is to be returned. This is always IID_IClassFactory.</param>
/// <param name="ppv">Stores a reference to the class factory that is created by this function.</param>
/// <returns>S_OK if an IClassFactory is successfully created, CLASS_E_CLASSNOTAVAILABLE if
/// some other profiler is requested, or another HRESULT indicating failure.</returns>
extern "C" HRESULT WINAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    if (IsEqualCLSID(rclsid, __uuidof(Profiler)))
    {
        IClassFactory* pClassFactory = new CClassFactory();

        if (pClassFactory == nullptr)
            return E_OUTOFMEMORY;

        return pClassFactory->QueryInterface(riid, ppv);
    }

	return CLASS_E_CLASSNOTAVAILABLE;
}

extern "C" HRESULT WINAPI DllCanUnloadNow()
{
	return S_OK;
}