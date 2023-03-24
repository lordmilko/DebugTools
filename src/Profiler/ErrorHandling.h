#pragma once

void dprintf(LPCWSTR format, ...);

//#define LOG_CALL 1
//#define LOG_HIERARCHY 1
//#define LOG_HRESULT 1 //And break on error
//#define LOG_EXCEPTION 1
//#define LOG_SHOULDHOOK 1
//#define LOG_HOOK 1
//#define LOG_SEQUENCE 1
//#define LOG_THREAD 1
//#define DEBUG_BLOB 1
//#define DEBUG_UNKNOWN 1

#ifdef _DEBUG

#ifdef LOG_CALL
#define LogCall(KIND, FUNCTIONID) dprintf(L"%d %d " KIND " " FORMAT_PTR "\n", GetCurrentThreadId(), g_Sequence, FUNCTIONID);
#endif //LOG_CALL
#ifdef LOG_HRESULT
//#define BreakCondition hr == E_FAIL
#ifndef BreakCondition
#define BreakCondition (hr < 0x80041001 || hr > 0x80042000)
#endif

#define LogError(EXPR) dprintf(L"Error 0x%X occurred calling %S at %S(%d)\n", hr, #EXPR, __FILE__, __LINE__); if (BreakCondition) DebugBreakSafe()
#endif //LOG_HRESULT
#ifdef LOG_HIERARCHY
void EnterLevel(LPCWSTR format, ...);
void ExitLevel(LPCWSTR format, ...);
#endif //LOG_HIERARCHY
#ifdef LOG_EXCEPTION
#define LogException dprintf
#endif //LOG_EXCEPTION
#ifdef LOG_SHOULDHOOK
#define LogShouldHook dprintf
#endif //LOG_SHOULDHOOK
#ifdef LOG_HOOK
#define LogHook dprintf
#endif
#ifdef LOG_SEQUENCE
#define LogSequence dprintf
#endif
#ifdef LOG_THREAD
#define LogThread dprintf
#endif

#ifdef DEBUG_BLOB

extern thread_local BOOL g_DebugBlob;

#define DebugBlob(str) if(g_DebugBlob) dprintf(L"%s %d\n", str, g_ValueBufferPosition)
#define DebugBlobCtx(str, ctx) if(g_DebugBlob) dprintf(L"%s %s %d\n", str, ctx, g_ValueBufferPosition)

#define DebugBlobHeader(HEADER) \
    DebugBlob(L"**************************************************************"); \
    DebugBlob(HEADER); \
    DebugBlob(L"**************************************************************")

#define BeginDebugBlob(METHOD, TYPE) \
    g_DebugBlob = FALSE; \
    if (wcscmp(pMethod->m_szName, METHOD) == 0 && wcscmp(((CClassInfo*)pMethodClassInfo)->m_szName, TYPE) == 0) \
    { \
        g_DebugBlob = TRUE; \
    }

#define HaltDebugBlob(METHOD, TYPE) \
    if (wcscmp(pMethod->m_szName, METHOD) == 0 && wcscmp(((CClassInfo*)pMethodClassInfo)->m_szName, TYPE) == 0) \
    { \
        DebugBreakSafe(); \
    }

#endif //DEBUG_BLOB

#endif //_DEBUG

#define DO_NOTHING do { } while(0)

#ifndef LOG_HIERARCHY
#define EnterLevel(format, ...) DO_NOTHING
#define ExitLevel(format, ...) DO_NOTHING
#endif
#ifndef LogCall
#define LogCall(...) DO_NOTHING
#endif
#ifndef LogError
#define LogError(EXPR)
#endif
#ifndef LogException
#define LogException(...) DO_NOTHING
#endif
#ifndef LogShouldHook
#define LogShouldHook(...) DO_NOTHING
#endif
#ifndef LogHook
#define LogHook
#endif
#ifndef LogSequence
#define LogSequence
#endif
#ifndef LogThread
#define LogThread(...) DO_NOTHING
#endif
#ifndef DebugBlob
#define DebugBlob(str)
#define DebugBlobCtx(str, ctx)
#define DebugBlobHeader(HEADER)
#define BeginDebugBlob(METHOD, TYPE)
#define HaltDebugBlob(METHOD, TYPE)
#endif

#define DebugBreakSafe() do \
    { \
        if (IsDebuggerPresent()) \
            DebugBreak(); \
    } while(0)

#define MAKE_ERROR(code) MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, code)

#define PROFILER_E_BUFFERFULL MAKE_ERROR(0x1001)
#define PROFILER_E_GENERICCLASSID MAKE_ERROR(0x1002)
#define PROFILER_E_UNKNOWN_GENERIC_ARRAY MAKE_ERROR(0x1003)
#define PROFILER_E_NO_CLASSID MAKE_ERROR(0x1004)
#define PROFILER_E_UNKNOWN_RESOLUTION_SCOPE MAKE_ERROR(0x1005)
#define PROFILER_E_MISSING_MODULE MAKE_ERROR(0x1006)
#define PROFILER_E_UNKNOWN_FRAME MAKE_ERROR(0x1007)
#define PROFILER_E_UNKNOWN_METHOD MAKE_ERROR(0x1008)
#define PROFILER_E_STATICFIELD_DETAILED_REQUIRED MAKE_ERROR(0x1009)
#define PROFILER_E_STATICFIELD_INVALID_REQUEST MAKE_ERROR(0x100A)
#define PROFILER_E_STATICFIELD_CLASS_NOT_FOUND MAKE_ERROR(0x100B)
#define PROFILER_E_STATICFIELD_CLASS_AMBIGUOUS MAKE_ERROR(0x100C)
#define PROFILER_E_STATICFIELD_FIELD_NOT_FOUND MAKE_ERROR(0x100D)
#define PROFILER_E_STATICFIELD_NOT_STATIC MAKE_ERROR(0x100E)
#define PROFILER_E_STATICFIELD_FIELDTYPE_UNKNOWN MAKE_ERROR(0x100F)
#define PROFILER_E_STATICFIELD_FIELDTYPE_NOT_SUPPORTED MAKE_ERROR(0x1010)
#define PROFILER_E_STATICFIELD_MULTIPLE_APPDOMAIN MAKE_ERROR(0x1011)
#define PROFILER_E_STATICFIELD_NEED_THREADID MAKE_ERROR(0x1012)
#define PROFILER_E_STATICFIELD_THREAD_NOT_FOUND MAKE_ERROR(0x1013)
#define PROFILER_E_STATICFIELD_INVALID_MEMORY MAKE_ERROR(0x1014)