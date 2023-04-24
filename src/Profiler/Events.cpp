#include "pch.h"
#include "Events.h"
#include "SafeQueue.h"

#define MMF_BUFFER_SIZE 1000000000

//Get the size of an MMFRecord, factoring in its size and the number of bytes it would take to store the size
#define RECORD_SIZE(r) (sizeof(DWORD) + (r).Size)
#define BUFFER_POSITION(ptr) (ptr - g_pEventBuffer)

#define WRITE_RECORD(r) \
    *(DWORD*)ptr = (r).Size; \
    ptr += sizeof(DWORD); \
    memcpy(ptr, record.Ptr, (r).Size); \
    ptr += record.Size; \
    free(record.Ptr)

typedef struct MMFRecord {
    ULONG Size;
    void* Ptr;
} MMFRecord;

BOOL g_IsETW = FALSE;
HANDLE g_hFile = NULL;
BYTE* g_pEventBuffer = NULL;
HANDLE g_HasDataEvent = NULL;
HANDLE g_WasProcessedEvent = NULL;

BOOL g_Stopping = FALSE;
SafeQueue<MMFRecord> g_MMFQueue;
HANDLE g_hMMFThread = NULL;

std::mutex g_WriteMutex;

#pragma region Write

DWORD WINAPI MMFThreadProc
    (LPVOID lpThreadParameter)
{
    HRESULT hr = S_OK;

    while (!g_Stopping)
    {
        DWORD numEntries = 1; //There'll always be at least 1
        BYTE* ptr = g_pEventBuffer + sizeof(DWORD); //Number of entries

        MMFRecord record;
        g_MMFQueue.Pop(record);

        if (g_Stopping)
            break;

        WRITE_RECORD(record);

        while (BUFFER_POSITION(ptr) < MMF_BUFFER_SIZE)
        {
            MMFRecord* next = g_MMFQueue.Peek();

            if (next == nullptr)
                break;

            if (BUFFER_POSITION(ptr) + RECORD_SIZE(*next) < MMF_BUFFER_SIZE)
            {
                numEntries++;
                g_MMFQueue.Pop(record);

                if (g_Stopping)
                    break;

                WRITE_RECORD(record);
            }
            else
                break;
        }

        size_t size = g_MMFQueue.Size();

        if (size > 10000)
            dprintf(L"Backlog is %d. Read %d\n", size, numEntries);

        *(DWORD*)g_pEventBuffer = numEntries;

        if (g_Stopping)
            break;

        DWORD result = SignalObjectAndWait(
            g_HasDataEvent,
            g_WasProcessedEvent,
            INFINITE,
            FALSE
        );

        if (result == WAIT_FAILED)
            ValidateETW(GetLastError());
    }

    return 0;
}

typedef struct MMFEventHeader {
    LONGLONG QPC;
    DWORD ThreadId;
    DWORD UserDataSize;
    USHORT EventType;
};

ULONG __stdcall EventWriteMMF(
    _In_ PCEVENT_DESCRIPTOR EventDescriptor,
    _In_range_(0, MAX_EVENT_DATA_DESCRIPTORS) ULONG UserDataCount,
    _In_reads_opt_(UserDataCount) PEVENT_DATA_DESCRIPTOR UserData)
{
    //Figure out how much space we'll need

    LARGE_INTEGER qpc;
    QueryPerformanceCounter(&qpc);

    DWORD userDataSize = 0;

    for (ULONG i = 1; i < UserDataCount; i++)
    {
        EVENT_DATA_DESCRIPTOR data = UserData[i];

        userDataSize += data.Size;
    }

    MMFEventHeader header = { qpc.QuadPart, GetCurrentThreadId(), userDataSize, EventDescriptor->Id };

    //This value has trailing padding
    DWORD headerSize = sizeof(MMFEventHeader);

    DWORD recordSize = headerSize + userDataSize;

    void* originalPtr = malloc(recordSize);
    BYTE* ptr = (BYTE*)originalPtr;

    memcpy(ptr, &header, headerSize);
    ptr += headerSize;

    for (ULONG i = 1; i < UserDataCount; i++)
    {
        EVENT_DATA_DESCRIPTOR data = UserData[i];

        memcpy(ptr, (void*)data.Ptr, data.Size);
        ptr += data.Size;
    }

    g_MMFQueue.Push({ recordSize, originalPtr });

    return ERROR_SUCCESS;
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

    ZeroMemory(g_pEventBuffer, MMF_BUFFER_SIZE);

    g_HasDataEvent = CreateEvent(NULL, FALSE, FALSE, szHasDataEventName);
    g_WasProcessedEvent = CreateEvent(NULL, FALSE, FALSE, szWasProcessedEventName);

    if (g_HasDataEvent == NULL || g_WasProcessedEvent == NULL)
        return GetLastError();

    g_hMMFThread = CreateThread(
        NULL,
        0,
        MMFThreadProc,
        NULL,
        0,
        NULL
    );

    if (g_hMMFThread == NULL)
        return GetLastError();

    //We won't get unregistered if the REGHANDLE hasn't been initialized!
    DebugToolsProfilerHandle = 1;

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
    g_Stopping = TRUE;
    g_MMFQueue.Stop();

    //If we're waiting for the profiler UI to notify us we've been processed, break the wait,
    //we're shutting down
    if (g_WasProcessedEvent)
        SetEvent(g_WasProcessedEvent);

    if (g_HasDataEvent)
        CloseHandle(g_HasDataEvent);

    if (g_WasProcessedEvent)
        CloseHandle(g_WasProcessedEvent);

    if (g_pEventBuffer)
        UnmapViewOfFile(g_pEventBuffer);

    if (g_hFile)
        CloseHandle(g_hFile);

    if (g_hMMFThread)
        CloseHandle(g_hMMFThread);

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