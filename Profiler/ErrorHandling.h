#pragma once

void dprintf(LPCWSTR format, ...);

//#define LOG_HRESULT 1
//#define LOG_SHOULDHOOK 1
//#define DEBUG_BLOB 1

#ifdef _DEBUG

#ifdef LOG_HRESULT
#define LogError(EXPR) dprintf(L"Error 0x%X occurred calling %S at %S(%d)\n", hr, #EXPR, __FILE__, __LINE__); if (IsDebuggerPresent() && (hr < 0x80041001 || hr > 0x80042000)) DebugBreak()
#endif //LOG_HRESULT

#ifdef LOG_SHOULDHOOK
#define LogShouldHook dprintf
#endif //LOG_SHOULDHOOK

#ifdef DEBUG_BLOB

extern thread_local BOOL g_DebugBlob;

#define DebugBlob(str) if(g_DebugBlob) dprintf(L"%s %d\n", str, g_ValueBufferPosition)
#define DebugBlobCtx(str, ctx) if(g_DebugBlob) dprintf(L"%s %s %d\n", str, ctx, g_ValueBufferPosition

#define DebugBlobHeader(HEADER) \
    DebugBlob(L"**************************************************************");
    DebugBlob(HEADER);
    DebugBlob(L"**************************************************************")

#define BeginDebugBlob(METHOD, TYPE) \
    if (wcscmp(pMethod->m_szName, METHOD) == 0 && wcscmp(((CClassInfo*)pMethodClassInfo)->m_szName, TYPE) == 0) \
    { \
        g_DebugBlob = TRUE; \
    }

#define HaltDebugBlob(METHOD, TYPE) \
    if (wcscmp(pMethod->m_szName, METHOD) == 0 && wcscmp(((CClassInfo*)pMethodClassInfo)->m_szName, TYPE) == 0) \
    { \
        if (IsDebuggerPresent()) \
            DebugBreak(); \
    }

#endif //DEBUG_BLOB

#endif //_DEBUG

#ifndef LogError
#define LogError(EXPR)
#endif
#ifndef LogShouldHook
#define LogShouldHook
#endif
#ifndef DebugBlob
#define DebugBlob(str)
#define DebugBlobCtx(str, ctx)
#define DebugBlobHeader(HEADER)
#define BeginDebugBlob(METHOD, TYPE)
#define HaltDebugBlob(METHOD, TYPE)
#endif

#define PROFILER_E_BUFFERFULL MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1001);
#define PROFILER_E_GENERICCLASSID MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1002);
#define PROFILER_E_UNKNOWN_GENERIC_ARRAY MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1003);
#define PROFILER_E_NO_CLASSID MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1004);
#define PROFILER_E_UNKNOWN_RESOLUTION_SCOPE MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1005);
#define PROFILER_E_MISSING_MODULE MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1006);
#define PROFILER_E_UNKNOWN_FRAME MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x1007);