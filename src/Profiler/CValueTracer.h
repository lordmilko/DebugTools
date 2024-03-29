#pragma once

#include <stack>
#include <unordered_map>
#include "CClassInfoResolver.h"

class CSigMethodDef;
class CSigType;
class ISigParameter;
class IClassInfo;
class CClassInfo;
class CArrayInfo;
class CModuleInfo;
class CSigGenericType;
class CSigPtrType;
class ISigArrayType;

#undef GetClassInfo

#define VALUE_BUFFER_SIZE 62000 //ETW is limited to 64KB

extern thread_local BYTE g_ValueBuffer[VALUE_BUFFER_SIZE];
extern thread_local signed long g_ValueBufferPosition;

//Stores a number that uniquely identifies each Enter/Leave/Tailcall event for the current thread.
extern thread_local ULONG g_Sequence;

enum class FrameKind
{
    Managed = 0,
    U2M,
    M2U
};

struct Frame
{
    FunctionID FunctionId;
    FrameKind Kind;

    Frame(FunctionID functionId, FrameKind kind) :
        FunctionId(functionId),
        Kind(kind)
    {
    }
};

//Stores the current stack of function calls for the current thread.
extern thread_local std::stack<Frame> g_CallStack;

extern thread_local BOOL g_CheckM2UUnwind;

#define ENTER_FUNCTION(FUNCTIONID, ENTERKIND) \
    do { \
    g_Sequence++; \
    LogSequence(L"Sequence is now %d %S(%d) (Enter)\n", g_Sequence, __FILE__, __LINE__); \
    g_CallStack.emplace(FUNCTIONID, ENTERKIND); \
    } while(0)

#define LEAVE_FUNCTION(FUNCTIONID) \
    g_Sequence++; \
    do { \
        LogSequence(L"Sequence is now %d %S(%d) (Leave)\n", g_Sequence, __FILE__, __LINE__); \
        /* If we started tracing after process start, we may see a series of leaves for enters that we never recorded */ \
        if (!g_CallStack.empty()) \
        { \
            Frame old = g_CallStack.top(); \
            g_CallStack.pop(); \
            if (old.FunctionId != (FUNCTIONID)) \
            { \
                dprintf(L"Stack Error: Expected " FORMAT_PTR " but got " FORMAT_PTR "\n", old.FunctionId, FUNCTIONID); \
                DebugBreakSafe(); \
                hr = PROFILER_E_UNKNOWN_FRAME; \
                goto ErrExit; \
            } \
        } \
    } while(0)

//An extra -1 on comparing the buffer size because the maximum index is VALUE_BUFFER_SIZE - 1 (arrays are 0 based!)

#define WriteType(elementType) \
    do { if (g_ValueBufferPosition >= VALUE_BUFFER_SIZE - 1 - 1) \
    { \
        hr = PROFILER_E_BUFFERFULL; \
        LogError("WriteType"); \
        goto ErrExit; \
    } \
    *(g_ValueBuffer + g_ValueBufferPosition) = elementType; \
    g_ValueBufferPosition++; \
    if (g_ValueBufferPosition >= VALUE_BUFFER_SIZE) DebugBreakSafe(); \
    } while(0)

#define WriteValue(pValue, length) \
    do { if (g_ValueBufferPosition >= VALUE_BUFFER_SIZE - (int)(length) - 1) \
    { \
        hr = PROFILER_E_BUFFERFULL; \
        LogError("WriteValue"); \
        goto ErrExit; \
    } \
    memcpy(g_ValueBuffer + g_ValueBufferPosition, pValue, length); \
    g_ValueBufferPosition += length; \
    if (g_ValueBufferPosition >= VALUE_BUFFER_SIZE) DebugBreakSafe(); \
    } while(0)

#define Write(pValue, elementType, expectedSize) \
     do { _ASSERTE(sizeof(*(pValue)) == (expectedSize)); \
     WriteType(elementType); \
     WriteValue(pValue, sizeof(*(pValue))); } while(0)

#define WriteRecursion(elementType) \
    do { WriteType(ELEMENT_TYPE_END); \
    WriteType(elementType); \
    if (g_ValueBufferPosition > VALUE_BUFFER_SIZE) DebugBreakSafe(); \
    } while(0)

#define WriteMaxTraceDepth() \
    do { WriteType(ELEMENT_TYPE_END); \
    WriteType(ELEMENT_TYPE_END); if (g_ValueBufferPosition > VALUE_BUFFER_SIZE) DebugBreakSafe(); \
    } while(0)

typedef struct _ValueTypeContext {
    mdToken TypeToken;
    ModuleID ModuleOfTypeToken;
} ValueTypeContext;

typedef struct _GenericArgContext {
    long GenericIndex;
    CClassInfoResolver* ClassInfoResolver; //Used by ELEMENT_TYPE_VAR only
} GenericArgContext;

typedef struct _GenericInstContext {
    CSigGenericType* GenericType;
} GenericInstContext;

typedef struct _PtrTypeContext {
    CSigPtrType* PtrType;
} PtrTypeContext;

typedef struct _TraceValueContext {
    ValueTypeContext ValueType;
    GenericArgContext GenericArg;

    union {
        CSigType* SigType;
        GenericInstContext GenericInst;
        PtrTypeContext Ptr;
    };
    CSigType* ParentType;
} TraceValueContext;

#define MakeTraceValueContext(typeToken, moduleOfTypeToken, genericIndex, classInfo, pType, pParentType) { \
    {typeToken, moduleOfTypeToken}, \
    {genericIndex, classInfo}, \
    {pType}, \
    pParentType \
}

class CSigField;

class CValueTracer
{
public:
    CValueTracer() :
        m_TraceDepth(0),
        m_MethodGenericTypeArgs(nullptr)
    {
        m_MaxTraceDepth = s_MaxTraceDepth;
    }

    ~CValueTracer()
    {
        if (m_MethodGenericTypeArgs)
            delete[] m_MethodGenericTypeArgs;
    }

    static HRESULT Initialize(ICorProfilerInfo4* pInfo);
    FORCEINLINE static BOOL IsInvalidObject(ObjectID objectId);
    static BOOL IsInvalidPointer(ObjectID objectId);

    HRESULT EnterWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT LeaveWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT TailcallWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

    HRESULT GetFieldValue(
        _In_ void* pAddress,
        _In_ CClassInfo* pInfo,
        _In_ CSigField* pField);

private:

    HRESULT GetMethodInfoNoLock(_In_ FunctionIDOrClientID functionId, _Out_ CSigMethodDef** ppMethod);

    HRESULT TraceParameters(
        _In_ COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo,
        _In_ CSigMethodDef* pMethod,
        _In_ CClassInfoResolver* resolver);

    HRESULT TraceParameter(
        _In_ COR_PRF_FUNCTION_ARGUMENT_RANGE* range,
        _In_ ISigParameter* pParameter,
        _In_ CClassInfoResolver* resolver,
        _In_ ModuleID typeTokenModule);

    HRESULT TraceValue(
        _In_ UINT_PTR startAddress,
        _In_ CorElementType elementType,
        _In_ TraceValueContext* pContext,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TraceBool(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    
    HRESULT TraceChar(
        _In_ UINT_PTR startAddress,
        _In_ TraceValueContext* pContext,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TraceSByte(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceByte(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceShort(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceUShort(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceInt(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceUInt(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceLong(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceULong(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceFloat(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceDouble(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceUIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceString(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceClassIgnoreDepth(_In_ UINT_PTR startAddress, _In_ CorElementType classElementType, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceClass(_In_ UINT_PTR startAddress, _In_ CorElementType classElementType, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceGenericType(_In_ UINT_PTR startAddress, _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceObject(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceArray(_In_ UINT_PTR startAddress, _In_ CorElementType type, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceArrayInternal(
        _In_ CArrayInfo* pArrayInfo,
        _In_ ObjectID objectId,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TraceValueType(
        _In_ UINT_PTR startAddress,
        _In_ TraceValueContext* pContext,
        _Out_opt_ ULONG& bytesRead);

    HRESULT GetTypeDefAndModule(
        _In_ ModuleID moduleOfTypeToken,
        _In_ mdToken typeToken,
        _Out_ ModuleID* moduleId,
        _Out_ mdTypeDef* typeDef);

    HRESULT GetClassId(
        _In_ CClassInfo* pClassInfo,
        _In_ CSigType* pType,
        _In_ ModuleID moduleOfTypeToken,
        _In_ CSigGenericType* curGenericType,
        _In_ ULONG curGenericArg,
        _Out_ ClassID* classId);

    HRESULT GetArrayClassId(
        _In_ CClassInfo* pClassInfo, //The current class that any ELEMENT_TYPE_VAR references will be resolved from
        _In_ CSigType* pType,
        _In_ ModuleID moduleOfTypeToken,
        _In_ CSigGenericType* curGenericType,
        _In_ ULONG curGenericArg,
        _Out_ ClassID* classId);

    HRESULT GetArrayClassIdFast(
        _In_ ISigArrayType* arr,
        _In_ ClassID elmClassId,
        _Out_ ClassID* classId);

    HRESULT GetArrayClassIdSlow(
        _In_ ISigArrayType* arr,
        _In_ ClassID elmClassId,
        _In_ CSigGenericType* curGenericType,
        _In_ ULONG curGenericArg,
        _Out_ ClassID* classId);

    HRESULT GetCachedGenericType(
        _In_ ModuleID moduleId,
        _In_ mdTypeDef typeDef,
        _In_ ULONG numGenericArgs,
        _In_ ClassID* typeArgs,
        _Out_ ClassID* pClassId);

    HRESULT GetAndCacheNewGenericType(
        _In_ ModuleID outerModuleId,
        _In_ mdTypeDef outerTypeDef,
        _In_ CSigGenericType* pGenericType,
        _In_ ClassID* typeArgIds,
        _Out_ ClassID* classId);

    BOOL IsArrayMatch(ISigArrayType* arr, CArrayInfo* info);

    HRESULT TraceTypeGenericType(_In_ UINT_PTR startAddress, _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceMethodGenericType(_In_ UINT_PTR startAddress, _In_ _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceGenericTypeInternal(
        _In_ UINT_PTR startAddress,
        _In_ IClassInfo* info,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TracePtrType(_In_ UINT_PTR startAddress, _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceFnPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceClassOrStruct(CClassInfo* pClassInfo, ObjectID objectId, CorElementType elementType, ULONG& bytesRead);

    HRESULT GetClassInfoFromTypeDef(CModuleInfo* pModuleInfo, mdTypeDef typeDef, IClassInfo** ppClassInfo);

    mdToken GetTypeToken(CSigType* pType);
    void GetGenericInfo(CSigType* pType, long* genericIndex);

    static ULONG s_StringLengthOffset;
    static ULONG s_StringBufferOffset;
    static ULONG s_MaxTraceDepth;
    static BOOL s_IgnorePointerValue;

    ULONG m_TraceDepth;

public:
    IClassInfo** m_MethodGenericTypeArgs;
    ULONG m_MaxTraceDepth;
};