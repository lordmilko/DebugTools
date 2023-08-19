#include "pch.h"
#include "Serialization.h"
#include "MMF.h"

#define WRITE_VALUE(v) \
    memcpy(ptr, &v, sizeof(v)); \
    ptr += sizeof(v)

#define WRITE_POINTER(p) \
    memcpy(ptr, p, sizeof(*(p))); \
    ptr += sizeof(*(p))

#define WRITE_STRING(s) \
    int length = (int) strlen(s); \
    WRITE_VALUE(length); \
    strncpy((char*)ptr, s, length); \
    ptr += length

#define WRITE_BEGIN \
    /* Make room for the amount of data we've written */ \
    BYTE* original = (BYTE*) malloc(MMF_BUFFER_SIZE / 100); \
    BYTE* ptr = original; \
    WRITE_VALUE(message); \
    WRITE_VALUE(hWnd); \
    WRITE_VALUE(wParam); \
    WRITE_VALUE(lParam)

#define WRITE_END \
    ULONG bufferSize = (ULONG) (ptr - original); \
    g_MMFQueue.Push({ bufferSize, original })

//A simple message with basic numeric values
void Simple(UINT message, HWND hWnd, WPARAM wParam, LPARAM lParam)
{
    WRITE_BEGIN;
    WRITE_END;
}

#define X(w, l) \
    WRITE_L_POINTER(w, l) \
    { \
        WRITE_BEGIN; \
        WRITE_POINTER(wParam); \
        WRITE_POINTER(lParam); \
        WRITE_END; \
    }
WPARAM_LPARAM_POINTERS
#undef X

#define X(w) \
    WRITE_L_POINTER(w, LPARAM) \
    { \
        WRITE_BEGIN; \
        WRITE_POINTER(wParam); \
        WRITE_END; \
    }
WPARAM_POINTERS
#undef X

#define X(l) \
    WRITE_L_POINTER(WPARAM, l) \
    { \
        WRITE_BEGIN; \
        WRITE_POINTER(lParam); \
        WRITE_END; \
    }
LPARAM_POINTERS
#undef X

WRITE_L_POINTER(WPARAM, char*)
{
    //If SetWindowsHookExA is called, ANSI strings are passed to the hook. If SetWindowsHookExW is called,
    //wide strings are passed to the hook. When no charset is specified to a DllImport, by default ANSI is used.

    WRITE_BEGIN;
    WRITE_STRING(lParam);
    WRITE_END;
}