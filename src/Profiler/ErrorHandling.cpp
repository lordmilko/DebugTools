#include "pch.h"
#include "ErrorHandling.h"

#ifdef DEBUG_BLOB
thread_local BOOL g_DebugBlob = TRUE;
#endif

thread_local WCHAR debugBuffer[2000];

void dprintf(LPCWSTR format, ...)
{
    va_list args;
    va_start(args, format);
    vswprintf_s(debugBuffer, format, args);
    va_end(args);
    OutputDebugString(debugBuffer);
}

#ifdef LOG_HIERARCHY
thread_local ULONG indentLevel = 0;


void EnterLevel(LPCWSTR format, ...)
{
    for (ULONG i = 0; i < indentLevel; i++)
        OutputDebugString(L"    ");

    va_list args;
    va_start(args, format);
    vswprintf_s(debugBuffer, format, args);
    va_end(args);
    OutputDebugString(debugBuffer);
    OutputDebugString(L"\n");

    indentLevel++;
}

void ExitLevel(LPCWSTR format, ...)
{
    indentLevel--;

    for (ULONG i = 0; i < indentLevel; i++)
        OutputDebugString(L"    ");

    va_list args;
    va_start(args, format);
    vswprintf_s(debugBuffer, format, args);
    va_end(args);
    OutputDebugString(debugBuffer);
    OutputDebugString(L"\n");
}

#endif