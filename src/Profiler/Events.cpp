#include "pch.h"
#include "Events.h"

BOOL g_IsETW = FALSE;
HANDLE g_hFile = NULL;
BYTE* g_pEventBuffer = NULL;
HANDLE g_HasDataEvent = NULL;
HANDLE g_WasProcessedEvent = NULL;

std::shared_mutex g_WriteMutex;

#pragma region Write

ULONG __stdcall EventWriteMMF(
    _In_ PCEVENT_DESCRIPTOR EventDescriptor,
    _In_range_(0, MAX_EVENT_DATA_DESCRIPTORS) ULONG UserDataCount,
    _In_reads_opt_(UserDataCount) PEVENT_DATA_DESCRIPTOR UserData)
{
    CLock lock(&g_WriteMutex, true);

    BYTE* ptr = g_pEventBuffer;

    memcpy(ptr, &EventDescriptor->Id, sizeof(EventDescriptor->Id));
    ptr += sizeof(EventDescriptor->Id);

    DWORD threadId = GetCurrentThreadId();
    memcpy(ptr, &threadId, sizeof(threadId));
    ptr += sizeof(threadId);

    BYTE* userDataPtr = ptr;
    ptr += 4;

    ULONG userDataSize = 0;

    for (ULONG i = 1; i < UserDataCount; i++)
    {
        EVENT_DATA_DESCRIPTOR data = UserData[i];

        memcpy(ptr, (void*)data.Ptr, data.Size);
        ptr += data.Size;
        userDataSize += data.Size;
    }

    memcpy(userDataPtr, &userDataSize, sizeof(userDataSize));

    DWORD tid = GetCurrentThreadId();

    DWORD result = SignalObjectAndWait(
        g_HasDataEvent,
        g_WasProcessedEvent,
        INFINITE,
        FALSE
    );

    if (result == WAIT_FAILED)
        return GetLastError();

    return 0;
}

FORCEINLINE ULONG __stdcall EventWriteTransferImpl(
    _In_ REGHANDLE RegHandle,
    _In_ PCEVENT_DESCRIPTOR EventDescriptor,
    _In_opt_ LPCGUID ActivityId,
    _In_opt_ LPCGUID RelatedActivityId,
    _In_range_(0, MAX_EVENT_DATA_DESCRIPTORS) ULONG UserDataCount,
    _In_reads_opt_(UserDataCount) PEVENT_DATA_DESCRIPTOR UserData
)
{
    if (g_IsETW)
        return EventWriteTransfer(RegHandle, EventDescriptor, ActivityId, RelatedActivityId, UserDataCount, UserData);
    else
        return EventWriteMMF(EventDescriptor, UserDataCount, UserData);
}

#pragma endregion
#pragma region Register

ULONG __stdcall EventRegisterMMF()
{
#define BUFFER_SIZE 200
    WCHAR envBuffer[BUFFER_SIZE];
    WCHAR szMapName[BUFFER_SIZE];
    WCHAR szHasDataEventName[BUFFER_SIZE];
    WCHAR szWasProcessedEventName[BUFFER_SIZE];

    DWORD actualSize = GetEnvironmentVariableW(L"DEBUGTOOLS_PARENT_PID", envBuffer, BUFFER_SIZE);

    if (actualSize == 0 || actualSize >= BUFFER_SIZE)
        return ERROR_BAD_ENVIRONMENT;

    DWORD pid = GetCurrentProcessId();

    swprintf_s(szMapName, L"DebugToolsMemoryMappedFile_%s_%d", envBuffer, pid);
    swprintf_s(szHasDataEventName, L"DebugToolsProfilerHasDataEvent_%s_%d", envBuffer, pid);
    swprintf_s(szWasProcessedEventName, L"DebugToolsProfilerWasProcessedEvent_%s_%d", envBuffer, pid);

    g_hFile = OpenFileMapping(
        FILE_MAP_READ | FILE_MAP_WRITE,
        FALSE,
        szMapName
    );

    if (g_hFile == NULL)
        return GetLastError();

    g_pEventBuffer = (BYTE*)MapViewOfFile(
        g_hFile,
        FILE_MAP_READ | FILE_MAP_WRITE,
        0,
        0,
        0
    );

    if (g_pEventBuffer == NULL)
        return GetLastError();

    ZeroMemory(g_pEventBuffer, 100000);

    g_HasDataEvent = CreateEvent(NULL, FALSE, FALSE, szHasDataEventName);
    g_WasProcessedEvent = CreateEvent(NULL, FALSE, FALSE, szWasProcessedEventName);

    if (g_HasDataEvent == NULL || g_WasProcessedEvent == NULL)
        return GetLastError();

    return ERROR_SUCCESS;
}

FORCEINLINE ULONG __stdcall EventRegisterImpl(
    _In_ LPCGUID ProviderId,
    _In_opt_ PENABLECALLBACK EnableCallback,
    _In_opt_ PVOID CallbackContext,
    _Out_ PREGHANDLE RegHandle
)
{
    if (g_IsETW)
        return EventRegister(ProviderId, EnableCallback, CallbackContext, RegHandle);
    else
        return EventRegisterMMF();
}

#pragma endregion
#pragma region Unregister

ULONG __stdcall EventUnregisterMMF()
{
    if (g_HasDataEvent)
        CloseHandle(g_HasDataEvent);

    if (g_WasProcessedEvent)
        CloseHandle(g_WasProcessedEvent);

    if (g_pEventBuffer)
        UnmapViewOfFile(g_pEventBuffer);

    if (g_hFile)
        CloseHandle(g_hFile);

    return ERROR_SUCCESS;
}

FORCEINLINE ULONG __stdcall EventUnregisterImpl(
    _In_ REGHANDLE RegHandle
)
{
    if (g_IsETW)
        return EventUnregister(RegHandle);
    else
        return EventUnregisterMMF();
}

#pragma endregion