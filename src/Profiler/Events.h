#pragma once

#include <evntprov.h>

extern BOOL g_IsETW;

extern FORCEINLINE ULONG __stdcall EventWriteTransferImpl(
    _In_ REGHANDLE RegHandle,
    _In_ PCEVENT_DESCRIPTOR EventDescriptor,
    _In_opt_ LPCGUID ActivityId,
    _In_opt_ LPCGUID RelatedActivityId,
    _In_range_(0, MAX_EVENT_DATA_DESCRIPTORS) ULONG UserDataCount,
    _In_reads_opt_(UserDataCount) PEVENT_DATA_DESCRIPTOR UserData
);

extern FORCEINLINE ULONG __stdcall EventRegisterImpl(
    _In_ LPCGUID ProviderId,
    _In_opt_ PENABLECALLBACK EnableCallback,
    _In_opt_ PVOID CallbackContext,
    _Out_ PREGHANDLE RegHandle
);

extern FORCEINLINE ULONG __stdcall EventUnregisterImpl(
    _In_ REGHANDLE RegHandle
);

#define MCGEN_EVENTWRITETRANSFER EventWriteTransferImpl
#define MCGEN_EVENTREGISTER EventRegisterImpl
#define MCGEN_EVENTUNREGISTER EventUnregisterImpl
#define MCGEN_EVENT_ENABLED(EventName) (g_IsETW ? EventEnabled##EventName() : TRUE)

#include "DebugToolsProfiler.h"