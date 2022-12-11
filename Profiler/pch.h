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

#define IfFailGoto(EXPR, LABEL) do { hr = (EXPR); if(FAILED(hr)) { goto LABEL;  } } while (0)
#define IfFailWin32Goto(EXPR, LABEL) do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); goto LABEL; } } while (0)
#define IfFailRet(EXPR)         do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)

#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)
#define IfFailWin32Go(EXPR) IfFailWin32Goto(EXPR, ErrExit)

#endif //PCH_H
