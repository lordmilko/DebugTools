#include "pch.h"
#include "ErrorHandling.h"

#ifdef DEBUG_BLOB
thread_local BOOL g_DebugBlob;
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