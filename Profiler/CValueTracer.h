#pragma once

#include <unordered_map>

class CSigMethodDef;
class CSigType;
class ISigParameter;
class IClassInfo;
class CClassInfo;
class CArrayInfo;

#undef GetClassInfo

#define WriteType(elementType) \
    if (g_ValueBufferPosition - 1 >= VALUE_BUFFER_SIZE) \
    { \
        hr = PROFILER_E_BUFFERFULL; \
        LogError("WriteType"); \
        goto ErrExit; \
    } \
    *(g_ValueBuffer + g_ValueBufferPosition) = elementType; \
    g_ValueBufferPosition++

#define WriteValue(pValue, length) \
    if (g_ValueBufferPosition >= VALUE_BUFFER_SIZE - (int)(length)) \
    { \
        hr = PROFILER_E_BUFFERFULL; \
        LogError("WriteValue"); \
        goto ErrExit; \
    } \
    memcpy(g_ValueBuffer + g_ValueBufferPosition, pValue, length); \
    g_ValueBufferPosition += length

#define Write(pValue, elementType, expectedSize) \
    _ASSERTE(sizeof(*(pValue)) == (expectedSize)); \
     WriteType(elementType); \
     WriteValue(pValue, sizeof(*(pValue)))
    

class CValueTracer
{
public:
    ~CValueTracer()
    {
        if (m_GenericTypeArgs)
            delete m_GenericTypeArgs;
    }

    static HRESULT Initialize(ICorProfilerInfo3* pInfo);

    HRESULT EnterWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT LeaveWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT TailcallWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

private:

    HRESULT TraceParameters(
        _In_ COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo,
        _In_ CSigMethodDef* pMethod,
        _In_ IClassInfo* pClassInfo);

    HRESULT TraceParameter(
        _In_ COR_PRF_FUNCTION_ARGUMENT_RANGE* range,
        _In_ ISigParameter* pParameter,
        _In_ IClassInfo* pClassInfo,
        _In_ ModuleID typeTokenModule);

    HRESULT TraceValue(
        _In_ UINT_PTR startAddress,
        _In_ CorElementType elementType,
        _In_ mdToken typeToken,
        _In_ ModuleID typeTokenModule,
        _In_ long genericIndex,
        _In_ IClassInfo* pClassInfo,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TraceBool(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceChar(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
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
    HRESULT TraceClass(_In_ UINT_PTR startAddress, _In_ CorElementType classElementType, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceGenericType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceObject(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceSZArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceSZArrayInternal(
        _In_ CArrayInfo* pArrayInfo,
        _In_ ObjectID objectId,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TraceValueType(_In_ UINT_PTR startAddress, _In_ mdToken typeToken, _In_ ModuleID typeTokenModule, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceTypeGenericType(_In_ UINT_PTR startAddress, _In_ long genericIndex, _In_ CClassInfo* pClassInfo, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceMethodGenericType(_In_ UINT_PTR startAddress, _In_ long genericIndex, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceGenericTypeInternal(
        _In_ UINT_PTR startAddress,
        _In_ IClassInfo* info,
        _Out_opt_ ULONG& bytesRead);

    HRESULT TracePtrType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);
    HRESULT TraceFnPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead);

    HRESULT TraceClassOrStruct(CClassInfo* pClassInfo, ObjectID objectId, CorElementType elementType, ULONG& bytesRead);

    HRESULT GetClassInfo(ClassID classId, IClassInfo** ppClassInfo);
    mdToken GetTypeToken(CSigType* pType);
    long GetGenericIndex(CSigType* pType);

    static ULONG s_StringLengthOffset;
    static ULONG s_StringBufferOffset;

    ULONG m_NumGenericTypeArgs;
    IClassInfo** m_GenericTypeArgs;
};