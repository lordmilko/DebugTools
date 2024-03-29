// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

#define _CRT_SECURE_NO_WARNINGS

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#include <Windows.h>

#define IfFailGoto(EXPR, LABEL) \
    do { \
        hr = (EXPR); \
        if(FAILED(hr)) \
        { \
            goto LABEL; \
        } \
    } while (0)

#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)

#endif //PCH_H
