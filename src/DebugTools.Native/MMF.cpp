#include "pch.h"
#include "MMF.h"
#include "../Profiler/SafeQueue.h"

//Get the size of an MMFRecord, factoring in its size and the number of bytes it would take to store the size
#define RECORD_SIZE(r) (sizeof(DWORD) + (r).Size)

#define WRITE_RECORD(r) \
    *(DWORD*)ptr = (r).Size; \
    ptr += sizeof(DWORD); \
    memcpy(ptr, record.Ptr, (r).Size); \
    ptr += record.Size; \
    free(record.Ptr)

BOOL g_Initialized = FALSE;

HANDLE g_hFile = NULL;
BYTE* g_pEventBuffer = NULL;
HANDLE g_HasDataEvent = NULL;
HANDLE g_WasProcessedEvent = NULL;

BOOL g_Stopping = FALSE;
SafeQueue<MMFRecord> g_MMFQueue;
HANDLE g_hMMFThread = NULL;

DWORD WINAPI MMFThreadProc(LPVOID lpThreadParameter);

ULONG MMFInitialize()
{
    std::mutex mutex;
    std::lock_guard<std::mutex> lock(mutex);

    if (!g_Initialized)
    {
#define BUFFER_SIZE 200
        WCHAR szMapName[BUFFER_SIZE];
        WCHAR szHasDataEventName[BUFFER_SIZE];
        WCHAR szWasProcessedEventName[BUFFER_SIZE];

        DWORD pid = GetCurrentProcessId();

        swprintf_s(szMapName, L"DebugToolsMemoryMappedFile_WndProc_%d", pid);
        swprintf_s(szHasDataEventName, L"DebugToolsHasDataEvent_WndProc_%d", pid);
        swprintf_s(szWasProcessedEventName, L"DebugToolsWasProcessedEvent_WndProc_%d", pid);

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

        g_Initialized = true;
    }

    return 0;
}

void MMFCleanup()
{
    std::mutex mutex;
    std::lock_guard<std::mutex> lock(mutex);

    if (g_Initialized)
    {
        g_Stopping = TRUE;

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

        g_Initialized = false;
    }
}

DWORD WINAPI MMFThreadProc(LPVOID lpThreadParameter)
{
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

        *(DWORD*)g_pEventBuffer = numEntries;

        if (g_Stopping)
            break;

        DWORD result = SignalObjectAndWait(
            g_HasDataEvent,
            g_WasProcessedEvent,
            INFINITE,
            FALSE
        );
    }

    return 0;
}