// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#include <Windows.h>
#include <cor.h>
#include <corprof.h>
#include <shared_mutex>

#include "ErrorHandling.h"

#define IfFailGoto(EXPR, LABEL) do { hr = (EXPR); if(FAILED(hr)) { LogError(EXPR); goto LABEL;  } } while (0)
#define IfFailWin32Goto(EXPR, LABEL) do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); goto LABEL; } } while (0)
#define IfFailRet(EXPR)         do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)

#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)
#define IfFailWin32Go(EXPR) IfFailWin32Goto(EXPR, ErrExit)

#define ValidateETW(call) do {\
        hr = HRESULT_FROM_WIN32(call); \
        if (hr != S_OK) \
        { \
            dprintf(L"###### %S failed with \n", #call, hr); \
            if (IsDebuggerPresent()) \
                DebugBreak(); \
        }\
    } while(0)

inline BOOL GetBoolEnv(LPCSTR name)
{
    CHAR buffer[2];
    DWORD size = GetEnvironmentVariableA(name, buffer, 2);
    return size == 1 && buffer[0] == '1';
}

class CLock
{
public:
    CLock(std::shared_mutex* mutex, bool exclusive = false)
    {
        m_Mutex = mutex;
        m_Exclusive = exclusive;

        if (exclusive)
            mutex->lock();
        else
            mutex->lock_shared();
    }

    ~CLock()
    {
        if (m_Exclusive)
            m_Mutex->unlock();
        else
            m_Mutex->unlock_shared();
    }

private:
    std::shared_mutex* m_Mutex;
    bool m_Exclusive;
};

#endif //PCH_H
