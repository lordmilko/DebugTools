#pragma once
#include "../Profiler/SafeQueue.h"

#define MMF_BUFFER_SIZE 1000000000

ULONG MMFInitialize();
void MMFCleanup();

typedef struct MMFRecord {
    ULONG Size;
    void* Ptr;
} MMFRecord;

#define BUFFER_POSITION(ptr) (ptr - g_pEventBuffer)

extern BYTE* g_pEventBuffer;
extern HANDLE g_HasDataEvent;
extern HANDLE g_WasProcessedEvent;

extern SafeQueue<MMFRecord> g_MMFQueue;