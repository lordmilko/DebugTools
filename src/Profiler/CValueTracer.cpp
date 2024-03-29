#include "pch.h"
#include "CValueTracer.h"
#include "CCorProfilerCallback.h"
#include "Events.h"
#include "CTypeRefResolver.h"
#include "CTypeIdentifier.h"
#include <unordered_set>

thread_local ULONG g_Sequence = 0;
thread_local std::stack<Frame> g_CallStack;
thread_local BOOL g_CheckM2UUnwind = FALSE;

thread_local std::unordered_set<UINT_PTR> g_SeenMap;
thread_local BYTE g_ValueBuffer[VALUE_BUFFER_SIZE];

//If a value is passed that is longer than VALUE_BUFFER_SIZE, the calculated remaining length will be negative,
//so we need to have a signed buffer length so that the comparison works correctly
thread_local signed long g_ValueBufferPosition = 0;

ULONG CValueTracer::s_StringLengthOffset;
ULONG CValueTracer::s_StringBufferOffset;
ULONG CValueTracer::s_MaxTraceDepth;
BOOL CValueTracer::s_IgnorePointerValue;

HRESULT CValueTracer::Initialize(ICorProfilerInfo4* pInfo)
{
#define BUFFER_SIZE 100

    HRESULT hr = S_OK;
    CHAR envBuffer[BUFFER_SIZE];
    DWORD actualSize;

    IfFailGo(pInfo->GetStringLayout2(&s_StringLengthOffset, &s_StringBufferOffset));

    actualSize = GetEnvironmentVariableA("DEBUGTOOLS_TRACEVALUEDEPTH", envBuffer, BUFFER_SIZE);

    if (actualSize != 0 && actualSize < BUFFER_SIZE)
        s_MaxTraceDepth = strtol(envBuffer, NULL, 10);
    else
        s_MaxTraceDepth = -1;

    s_IgnorePointerValue = GetBoolEnv("DEBUGTOOLS_IGNORE_POINTERVALUE");

ErrExit:
    return hr;
}

#pragma region ELT

HRESULT CValueTracer::EnterWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

    g_SeenMap.clear();
    g_ValueBufferPosition = 0;

    CSigMethodDef* pMethod = nullptr;

    //GetFunctionEnter3Info
    COR_PRF_FRAME_INFO frameInfo;
    ULONG cbArgumentInfo = 0;
    COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo = nullptr;

    //Lock scope
    {
        CLock methodLock(&g_pProfiler->m_MethodMutex);
        IfFailGo(GetMethodInfoNoLock(functionId, &pMethod));
    }

    if (pMethod->m_NumParameters == 0)
    {
        WriteValue(&pMethod->m_NumParameters, 4);
        goto ErrExit;
    }

    hr = g_pProfiler->m_pInfo->GetFunctionEnter3Info(
        functionId.functionID,
        eltInfo,
        &frameInfo,
        &cbArgumentInfo,
        nullptr
    );

    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        argumentInfo = static_cast<COR_PRF_FUNCTION_ARGUMENT_INFO*>(malloc(cbArgumentInfo));

        IfFailGo(g_pProfiler->m_pInfo->GetFunctionEnter3Info(
            functionId.functionID,
            eltInfo,
            &frameInfo,
            &cbArgumentInfo,
            argumentInfo
        ));

        CClassInfoResolver resolver(functionId, pMethod, frameInfo, this);

        DebugBlobHeader(L"Enter Start");

        IfFailGo(TraceParameters(argumentInfo, pMethod, &resolver));
    }

ErrExit:
    DebugBlobHeader(L"Enter End");

    ValidateETW(EventWriteCallEnterDetailedEvent(functionId.functionID, g_Sequence, hr, g_ValueBufferPosition, g_ValueBuffer));

    if (argumentInfo != nullptr)
        free(argumentInfo);

    return hr;
}

HRESULT CValueTracer::LeaveWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    /* As far as the runtime is concerned, there is no difference between ref and out parameters: both of these are represented as being byref parameters
     * in the method's sigblob. When a parameter is a byref, the parameter's startAddress is not a UINT_PTR, but a UINT_PTR* pointing to a location on the stack
     * or heap that the value should be read from. Given that the address of a byref parameter is only available on function enter, it would appear that in order
     * to trace the value written to the parameter inside the function on function leave, you ostensibly would need to record the startAddress during the enter call
     * so that you may check what value was written (if any) to it during the leave call. For values stored on the stack this seems safe enough; for values on the heap,
     * it's hard to say. If a GC occurred and the containing object was moved, would the startAddress now point to invalid memory? ECMA 335 I.12.1.1.2, PDF page 99 indicates
     * that the value pointed to by startAddress may be updated to point to the new memory location. There don't seem to be any examples of reading byref parameters on leave
     * anywhere on the internet that I can find. As there's a little bit of complexity here, and this would hurt performance a ittle bit, we do not currently support reading
     * byref values on leave. Only the value recorded on enter will be logged.
     *
     * Note that when it comes to distinguishing ref/out parameters, this can be done by checking whether the parameter metadata contains CorParamAttr.pdOut (which can be done
     * in C++ using the IsPdOut macro). As mentioned above however, knowing whether a value IsPdOut or not makes no difference to how the value is traced. 
     */

    HRESULT hr = S_OK;

    g_SeenMap.clear();
    g_ValueBufferPosition = 0;

    CSigMethodDef* pMethod = nullptr;

    COR_PRF_FRAME_INFO frameInfo;
    COR_PRF_FUNCTION_ARGUMENT_RANGE retvalRange;

    ULONG bytesRead = 0;
    TraceValueContext ctx;
    mdToken typeToken;
    long genericIndex = -1;
    CSigType* pType;

    //Lock scope
    {
        CLock methodLock(&g_pProfiler->m_MethodMutex);
        IfFailGo(GetMethodInfoNoLock(functionId, &pMethod));
    }

    pType = pMethod->m_pRetType;

    DebugBlobHeader(L"Return Start");

    if (pType->m_Type == ELEMENT_TYPE_VOID)
    {
        DebugBlobCtx(L"Return Type", pMethod->m_szName);
        WriteType(ELEMENT_TYPE_VOID);
        goto ErrExit;
    }

    typeToken = GetTypeToken(pType);

    GetGenericInfo(pType, &genericIndex);

    IfFailGo(g_pProfiler->m_pInfo->GetFunctionLeave3Info(
        functionId.functionID,
        eltInfo,
        &frameInfo,
        &retvalRange
    ));

    {
        //Don't resolve the method's CClassInfo unless we actually need it (because we're tracing an ELEMENT_TYPE_VAR)
        CClassInfoResolver resolver(functionId, pMethod, frameInfo, this);

        ctx = MakeTraceValueContext(typeToken, pMethod->m_ModuleID, genericIndex, &resolver, pType, nullptr);

        IfFailGo(TraceValue(
            retvalRange.startAddress,
            pMethod->m_pRetType->m_Type,
            &ctx,
            bytesRead
        ));
    }

ErrExit:
    DebugBlobHeader(L"Return End");

    ValidateETW(EventWriteCallLeaveDetailedEvent(functionId.functionID, g_Sequence, hr, g_ValueBufferPosition, g_ValueBuffer));

    return hr;
}

HRESULT CValueTracer::TailcallWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;
    CSigMethodDef* pMethod;
    g_ValueBufferPosition = 0;

    CLock methodLock(&g_pProfiler->m_MethodMutex);

    //HRESULT needs to be reported to profiler controller
    hr = GetMethodInfoNoLock(functionId, &pMethod);

    ValidateETW(EventWriteTailcallDetailedEvent(functionId.functionID, g_Sequence, hr, 0, NULL));

    return hr;
}

#pragma endregion
#pragma region Parameters

HRESULT CValueTracer::TraceParameters(
    _In_ COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo,
    _In_ CSigMethodDef* pMethod,
    _In_ CClassInfoResolver* resolver)
{
    HRESULT hr = S_OK;

    ULONG offset = 0;

    if (pMethod->m_CallingConv & IMAGE_CEE_CS_CALLCONV_HASTHIS)
        offset++;

    ULONG validRanges = argumentInfo->numRanges - offset;

    if (validRanges != pMethod->m_NumParameters)
    {
        dprintf(L"Had %d valid ranges (after ignoring %d ranges) however expected %d parameters\n", validRanges, offset, pMethod->m_NumParameters);
        hr = E_FAIL;
        goto ErrExit;
    }

    DebugBlobCtx(L"Num Method Parameters", pMethod->m_szName);
    WriteValue(&validRanges, 4);

    for(ULONG i = 0; i < validRanges; i++)
    {
        COR_PRF_FUNCTION_ARGUMENT_RANGE range = argumentInfo->ranges[i + offset];

        ISigParameter* pParameter = pMethod->m_Parameters[i];

        DebugBlob(L"\n*** TRACE PARAMETER ***");

        IfFailGo(TraceParameter(&range, pParameter, resolver, pMethod->m_ModuleID));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceParameter(
    _In_ COR_PRF_FUNCTION_ARGUMENT_RANGE* range,
    _In_ ISigParameter* pParameter,
    _In_ CClassInfoResolver* resolver,
    _In_ ModuleID typeTokenModule)
{
    HRESULT hr = S_OK;

    ULONG bytesRead = 0;
    
    CSigType* pType = pParameter->m_pType;

    mdToken typeToken = GetTypeToken(pType);

    long genericIndex = -1;
    GetGenericInfo(pType, &genericIndex);

    TraceValueContext ctx = MakeTraceValueContext(typeToken, typeTokenModule, genericIndex, resolver, pType, nullptr);

    IfFailGo(TraceValue(
        range->startAddress,
        pType->m_Type,
        &ctx,
        bytesRead
    ));

ErrExit:
    return hr;
}

#pragma endregion

#define TraceSimpleValue(STARTADDRESS, ELEMENTTYPE, BYTESREAD) \
    TraceValue( \
        (STARTADDRESS), /* startAddress */ \
        (ELEMENTTYPE),  /* elementType */ \
        nullptr,        /* pContext */ \
        (BYTESREAD)     /* bytesRead */ \
    )

//typeTokenModule is the module that applies to the context in which typeToken was resolved. If typeToken is an mdTypeDef, that means it was resolved within the scope of typeTokenModule
HRESULT CValueTracer::TraceValue(
    _In_ UINT_PTR startAddress,
    _In_ CorElementType elementType,
    _In_ TraceValueContext* pContext,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;
    BOOL needDecrease = FALSE;

    BOOL needSeenMap = elementType == ELEMENT_TYPE_OBJECT || elementType == ELEMENT_TYPE_CLASS || elementType == ELEMENT_TYPE_GENERICINST || elementType == ELEMENT_TYPE_SZARRAY || elementType == ELEMENT_TYPE_ARRAY;

    if (needSeenMap && g_SeenMap.find(startAddress) != g_SeenMap.end())
    {
        DebugBlob(L"Recursion");
        WriteRecursion(elementType);
        return hr;
    }

    if (m_MaxTraceDepth != (ULONG)-1 && m_TraceDepth >= m_MaxTraceDepth)
    {
        DebugBlob(L"MaxTraceDepth");
        WriteMaxTraceDepth();
        return hr;
    }

    /* pContext is only ever null when tracing a simple value.Is it possible we could end up with a byref value without the pContext we need to unwrap it? No.
     *
     * - ref values cannot be stored inside arrays
     * - If we have a struct ref struct a { ref object b = 1 }, ELEMENT_TYPE_OBJECT will be reported as byref, and upon unwrapping it we'll see it has an ELEMENT_TYPE_I4 - but
     *   at that point we're already unwrapped.
     * - If we have an ELEMENT_TYPE_VAR or ELEMENT_TYPE_MVAR (e.g. foo<int>), the VAR/MVAR element will be be passed to TraceValue first, we'll see its byref and unwrap it, and then
     *   and then TraceGenericTypeInternal() will pass the real I4 to us - but its no longer byref at that point so there's no problems */
    if (pContext != nullptr && pContext->SigType != nullptr && pContext->SigType->m_IsByRef)
    {
        //A ByRef value can come in many forms. It could be foo(ref value), foo() { return ref value } and even ref struct foo { ref int bar }.
        //In all 3 of these scenarios, the sigtype reports IsByRef and we indirect it here
        startAddress = *(UINT_PTR*)startAddress;
    }

    if (startAddress == 0)
    {
        /* There are some scenarios where the location of a ByRef value is null. For example, Unsafe.IsNullRef(), as well as some COM interfaces called by Visual Studio.
         * A ByRef parameter should always point to a valid stack or heap location, so it seems impossible that an address of 0 would be returned. There isn't any error occurring
         * while retrieving the args for the profiler - the address really is 0.
         * Per https://github.com/dotnet/runtime/issues/31170, in IL it is perfectly valid for a "ref" to point to null. That is, the "ref" itself is null,
         * not the value pointed to by the ref. As such, in the following code
         *
         *    object val = null;
         *    Unsafe.IsNullRef(ref val)
         *
         * IsNullRef would return FALSE: the ref points to the stack variable "val", which contains null. A null ref can be created in C# as follows
         *
         *    ref int nullRef = ref Unsafe.NullRef<int>();
         */
        WriteType(ELEMENT_TYPE_PTR);
        WriteType(ELEMENT_TYPE_END);
        WriteType(elementType);
        IfFailGo(TraceIntPtr((UINT_PTR)&startAddress, bytesRead));

        return S_OK;
    }

    needDecrease = TRUE;
    m_TraceDepth++;

    if (needSeenMap)
        g_SeenMap.insert(startAddress);

    switch (elementType)
    {
    #pragma region Primitives

    case ELEMENT_TYPE_BOOLEAN:
        IfFailGo(TraceBool(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_CHAR:
        IfFailGo(TraceChar(startAddress, pContext, bytesRead));
        break;

    case ELEMENT_TYPE_I1:
        IfFailGo(TraceSByte(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_U1:
        IfFailGo(TraceByte(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_I2:
        IfFailGo(TraceShort(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_U2:
        IfFailGo(TraceUShort(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_I4:
        IfFailGo(TraceInt(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_U4:
        IfFailGo(TraceUInt(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_I8:
        IfFailGo(TraceLong(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_U8:
        IfFailGo(TraceULong(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_R4:
        IfFailGo(TraceFloat(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_R8:
        IfFailGo(TraceDouble(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_I:
        IfFailGo(TraceIntPtr(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_U:
        IfFailGo(TraceUIntPtr(startAddress, bytesRead));
        break;

    #pragma endregion
    #pragma region ObjectID

    case ELEMENT_TYPE_STRING:
        IfFailGo(TraceString(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_CLASS:
        IfFailGo(TraceClass(startAddress, elementType, bytesRead));
        break;

    //If a field or parameter is of type object, there's a level of indirection we need to traverse
    //in order to get to the real value we need to inspect
    case ELEMENT_TYPE_OBJECT:
        IfFailGo(TraceClassIgnoreDepth(startAddress, elementType, bytesRead));
        break;

    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
        IfFailGo(TraceArray(startAddress, elementType, bytesRead));
        break;

    case ELEMENT_TYPE_GENERICINST:
        IfFailGo(TraceGenericType(startAddress, pContext, bytesRead));
        break;

    #pragma endregion

    case ELEMENT_TYPE_VALUETYPE:
        IfFailGo(TraceValueType(startAddress, pContext, bytesRead));
        break;

    case ELEMENT_TYPE_VAR:
        IfFailGo(TraceTypeGenericType(startAddress, pContext, bytesRead));
        break;

    case ELEMENT_TYPE_MVAR:
        IfFailGo(TraceMethodGenericType(startAddress, pContext, bytesRead));
        break;

    case ELEMENT_TYPE_PTR:
        IfFailGo(TracePtrType(startAddress, pContext, bytesRead));
        break;

    case ELEMENT_TYPE_FNPTR:
        IfFailGo(TraceFnPtr(startAddress, bytesRead));
        break;

    case ELEMENT_TYPE_END:
    case ELEMENT_TYPE_VOID:
    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_TYPEDBYREF:
    case ELEMENT_TYPE_CMOD_REQD:
    case ELEMENT_TYPE_CMOD_OPT:
    case ELEMENT_TYPE_INTERNAL:
    case ELEMENT_TYPE_MAX:
    case ELEMENT_TYPE_MODIFIER:
    case ELEMENT_TYPE_SENTINEL:
    case ELEMENT_TYPE_PINNED:
    default:
        dprintf(L"Don't know how to process element type %d\n", elementType);
        hr = E_FAIL;
        break;
    }

ErrExit:
    if (needDecrease)
        m_TraceDepth--;

    return hr;
}

#pragma region Primitives

HRESULT CValueTracer::TraceBool(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    BOOL* pBool = (BOOL*)startAddress;

    DebugBlob(L"Bool");
    Write((BYTE*)pBool, ELEMENT_TYPE_BOOLEAN, 1);
    bytesRead += sizeof(BOOL);

ErrExit:
    return hr;
}

int wcslen_memSafe(const WCHAR* str)
{
    const WCHAR* ptr = str;

    //Unlike wcslen, wcslen_memSafe includes a trailing \0

    __try
    {
        while (*ptr) //While its not \0
        {
            ptr++;
        }

        return (int)(ptr - str) + 1;
    }
    __except (GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        //The current location of ptr is invalid, so step back 1 character
        return (int)((ptr - 1) - str);
    }
}

HRESULT CValueTracer::TraceChar(
    _In_ UINT_PTR startAddress,
    _In_ TraceValueContext* pContext,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    WCHAR* pChar = (WCHAR*)startAddress;

    if (pContext && pContext->ParentType && pContext->ParentType->m_Type == ELEMENT_TYPE_PTR)
    {
        DebugBlob(L"Char*");
        WriteType(ELEMENT_TYPE_CHAR);

        /* Any reference to an ELEMENT_TYPE_CHAR could now be
         * a string. However you could just as easily pass (char*)1.
         * An invalid pointer would have been validated in TracePtrType().
         * However, just because the pointer may be valid, doesn't necessarily mean its null terminated.
         * Microsoft.CodeAnalysis.Workspaces.dll!Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory+DirectMemoryAccessStreamReader
         * takes a char* and a length. The string isn't null terminated, and beyond the end of the string is part of a string that previously existed,
         * followed by invalid memory */
        ULONG strLen = (ULONG)wcslen_memSafe(pChar);

        WriteValue(&strLen, 4);
        WriteValue(pChar, (strLen - 1) * sizeof(WCHAR));
        WriteValue(L"\0", sizeof(WCHAR));

        bytesRead += sizeof(WCHAR*);
    }
    else
    {
        DebugBlob(L"Char");
        Write(pChar, ELEMENT_TYPE_CHAR, 2);
        bytesRead += sizeof(WCHAR);
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceSByte(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    signed char* pByte = (signed char*)startAddress;

    DebugBlob(L"SByte");
    Write(pByte, ELEMENT_TYPE_I1, 1);
    bytesRead += sizeof(signed char);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceByte(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    BYTE* pByte = (BYTE*)startAddress;

    DebugBlob(L"Byte");
    Write(pByte, ELEMENT_TYPE_U1, 1);
    bytesRead += sizeof(BYTE);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceShort(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    SHORT* pShort = (SHORT*)startAddress;

    DebugBlob(L"Short");
    Write(pShort, ELEMENT_TYPE_I2, 2);
    bytesRead += sizeof(SHORT);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUShort(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    USHORT* pShort = (USHORT*)startAddress;

    DebugBlob(L"UShort");
    Write(pShort, ELEMENT_TYPE_U2, 2);
    bytesRead += sizeof(USHORT);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceInt(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    INT32* pInt = (INT32*)startAddress;

    DebugBlob(L"Int");
    Write(pInt, ELEMENT_TYPE_I4, 4);
    bytesRead += sizeof(INT32);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUInt(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    UINT32* pInt = (UINT32*)startAddress;

    DebugBlob(L"UInt");
    Write(pInt, ELEMENT_TYPE_U4, 4);
    bytesRead += sizeof(UINT32);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceLong(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    INT64* pInt = (INT64*)startAddress;

    DebugBlob(L"Long");
    Write(pInt, ELEMENT_TYPE_I8, 8);
    bytesRead += sizeof(INT64);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceULong(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    UINT64* pInt = (UINT64*)startAddress;

    DebugBlob(L"ULong");
    Write(pInt, ELEMENT_TYPE_U8, 8);
    bytesRead += sizeof(UINT64);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceFloat(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    FLOAT* pFloat = (FLOAT*)startAddress;

    DebugBlob(L"Float");
    Write(pFloat, ELEMENT_TYPE_R4, 4);
    bytesRead += sizeof(FLOAT);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceDouble(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    DOUBLE* pDouble = (DOUBLE*)startAddress;

    DebugBlob(L"Double");
    Write(pDouble, ELEMENT_TYPE_R8, 8);
    bytesRead += sizeof(DOUBLE);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    __int64* pPtr = (__int64*)startAddress;

    DebugBlob(L"IntPtr");
    Write(pPtr, ELEMENT_TYPE_I, 8);
    bytesRead += sizeof(INT_PTR);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    __int64* pPtr = (__int64*)startAddress;

    DebugBlob(L"UIntPtr");
    Write(pPtr, ELEMENT_TYPE_U, 8);
    bytesRead += sizeof(UINT_PTR);

ErrExit:
    return hr;
}

#pragma endregion
#pragma region ObjectID

HRESULT CValueTracer::TraceString(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    ULONG length;
    LPWSTR buffer;

    ObjectID objectId = *(ObjectID*)startAddress;

    if (IsInvalidObject(objectId))
    {
        ULONG size = 0;

        DebugBlob(L"Null String");
        WriteType(ELEMENT_TYPE_STRING);
        WriteValue(&size, 4);

        bytesRead += sizeof(void*);
        return hr;
    }

    length = *(ULONG*)((BYTE*)objectId + s_StringLengthOffset) + 1;
    buffer = (LPWSTR)((BYTE*)objectId + s_StringBufferOffset);

    DebugBlob(L"String");
    WriteType(ELEMENT_TYPE_STRING);
    WriteValue(&length, 4);
    WriteValue(buffer, (length - 1) * sizeof(WCHAR));
    WriteValue(L"\0", sizeof(WCHAR));

    bytesRead += sizeof(void*);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceClassIgnoreDepth(_In_ UINT_PTR startAddress, _In_ CorElementType classElementType, _Out_opt_ ULONG& bytesRead)
{
    m_TraceDepth--;
    HRESULT hr = TraceClass(startAddress, classElementType, bytesRead);
    m_TraceDepth++;

    return hr;
}

HRESULT CValueTracer::TraceClass(_In_ UINT_PTR startAddress, _In_ CorElementType classElementType, _Out_opt_ ULONG& bytesRead)
{
    //https://chnasarre.medium.com/accessing-arrays-and-class-fields-with-net-profiling-apis-d5ff21114a5d
    //https://github.com/wickyhu/simple-assembly-explorer/blob/8686fe5b82194b091dcfef4d29e78775591258a8/SimpleProfiler/ProfilerCallback.cpp

    HRESULT hr = S_OK;
    ULONG32 boxedValueOffset;
    ClassID classId = 0;
    IClassInfo* info = nullptr;
    ULONG innerBytesRead = 0;

    ObjectID objectId = *(ObjectID*)startAddress;

    if (IsInvalidObject(objectId))
    {
        //It's a null object
        ULONG size = 0;

        DebugBlob(L"Null Class");
        WriteType(ELEMENT_TYPE_CLASS);
        WriteValue(&size, 4);

        bytesRead += sizeof(void*);
        return S_OK;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromObject(objectId, &classId));
    IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, &info));

    if (info->m_InfoType == ClassInfoType::Array)
    {
        CArrayInfo* pArrayInfo = (CArrayInfo*)info;

        IfFailGo(TraceArrayInternal(pArrayInfo, objectId, innerBytesRead));
    }
    else
    {
        if (info->m_InfoType == ClassInfoType::StandardType)
        {
            CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)info;

            if (g_pProfiler->m_pInfo->GetBoxClassLayout(classId, &boxedValueOffset) == S_OK)
            {
                IfFailGo(TraceSimpleValue(
                    objectId + boxedValueOffset,
                    pStandardTypeInfo->m_ElementType,
                    innerBytesRead
                ));
            }
            else
            {
                IfFailGo(TraceSimpleValue(
                    startAddress,
                    pStandardTypeInfo->m_ElementType,
                    innerBytesRead
                ));
            }
        }
        else
        {
            CClassInfo* pClassInfo = (CClassInfo*)info;

            if (g_pProfiler->m_pInfo->GetBoxClassLayout(classId, &boxedValueOffset) == S_OK)
                IfFailGo(TraceClassOrStruct(pClassInfo, objectId + boxedValueOffset, ELEMENT_TYPE_VALUETYPE, innerBytesRead));
            else
                IfFailGo(TraceClassOrStruct(pClassInfo, objectId, classElementType, innerBytesRead));
        }
    }

    bytesRead += sizeof(void*);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceGenericType(_In_ UINT_PTR startAddress, _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead)
{
    //Someone has passed a value like 'new Foo<T>()' as a value to a function. T could be related to the method generic type,
    //the containing struct/class generic type, or something hardcoded

    HRESULT hr = S_OK;

    if (pContext->GenericInst.GenericType->m_GenericInstType == ELEMENT_TYPE_CLASS)
    {
        return TraceClass(startAddress, ELEMENT_TYPE_CLASS, bytesRead);
    }
    else
    {
        ClassID classId;

        CClassInfo* pClassInfo;
        CClassInfo* genericType;

        IfFailGo(pContext->GenericArg.ClassInfoResolver->Resolve(&genericType));

        IfFailGo(GetClassId(
            genericType,
            pContext->GenericInst.GenericType,
            pContext->ValueType.ModuleOfTypeToken,
            pContext->GenericInst.GenericType,
            -1,
            &classId
        ));

        IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, (IClassInfo**)&pClassInfo));

        IfFailGo(TraceClassOrStruct(pClassInfo, startAddress, ELEMENT_TYPE_VALUETYPE, bytesRead));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceObject(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    return hr;
}

HRESULT CValueTracer::TraceArray(_In_ UINT_PTR startAddress, _In_ CorElementType type, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    ObjectID objectId = *(ObjectID*)startAddress;
    ClassID classId;
    CArrayInfo* pArrayInfo;

    if (IsInvalidObject(objectId))
    {
        DebugBlob(L"Null Array");
        WriteType(type);

        if (type == ELEMENT_TYPE_ARRAY)
        {
            ULONG rank = 0;
            WriteValue(&rank, 4);
        }

        WriteType(ELEMENT_TYPE_END);

        bytesRead += sizeof(void*);

        return S_OK;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromObject(objectId, &classId));
    IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, (IClassInfo**)&pArrayInfo));

    IfFailGo(TraceArrayInternal(pArrayInfo, objectId, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceArrayInternal(
    _In_ CArrayInfo* pArrayInfo,
    _In_ ObjectID objectId,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    //We want to capture at least the first level of data inside of the array
    m_TraceDepth--;

    ULONG32* dimensionSizes = new ULONG32[pArrayInfo->m_Rank];
    int* dimensionLowerBounds = new int[pArrayInfo->m_Rank];
    BYTE* pData;

    ULONG totalLength = 0;
    ULONG dimensionLength;
    ULONG arrBytesRead = 0;

    IfFailGo(g_pProfiler->m_pInfo->GetArrayObjectInfo(objectId, pArrayInfo->m_Rank, dimensionSizes, dimensionLowerBounds, &pData));

    DebugBlob(L"Array Type");
    if (pArrayInfo->m_Rank == 1)
        WriteType(ELEMENT_TYPE_SZARRAY);
    else
    {
        WriteType(ELEMENT_TYPE_ARRAY);
        WriteValue(&pArrayInfo->m_Rank, 4);
    }

    DebugBlob(L"Array Elm Type");
    WriteType(pArrayInfo->m_CorElementType);

    for (ULONG i = 0; i < pArrayInfo->m_Rank; i++)
    {
        dimensionLength = dimensionSizes[i];

        DebugBlob(L"Dimension Length");
        WriteValue(&dimensionLength, 4);
    }

    totalLength = dimensionSizes[0];

    for (ULONG i = 1; i < pArrayInfo->m_Rank; i++)
        totalLength *= dimensionSizes[i];

    if (pArrayInfo->m_pElementType->m_InfoType == ClassInfoType::StandardType)
    {
        CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)pArrayInfo->m_pElementType;

        for (ULONG i = 0; i < totalLength; i++)
        {
            IfFailGo(TraceSimpleValue(
                (UINT_PTR)(pData + arrBytesRead),
                pStandardTypeInfo->m_ElementType,
                arrBytesRead
            ));
        }
    }
    else
    {
        //We're assuming we can't have a CArrayInfo inside another CArrayInfo
        CClassInfo* pElementType = (CClassInfo*)pArrayInfo->m_pElementType;

        for (ULONG i = 0; i < totalLength; i++)
        {
            UINT_PTR elmAddress = (UINT_PTR)(pData + arrBytesRead);

            EnterLevel(L"%s[%d] (ObjectId: 0x" FORMAT_PTR L", ElmAddress: " FORMAT_PTR ") !dumparray " FORMAT_PTR, pElementType->m_szName, i, objectId, elmAddress, objectId);

            if (pArrayInfo->m_CorElementType == ELEMENT_TYPE_VALUETYPE)
            {
                IfFailGo(TraceClassOrStruct(
                    pElementType,
                    elmAddress,
                    pArrayInfo->m_CorElementType,
                    arrBytesRead
                ));
            }
            else
            {
                CClassInfoResolver resolver(pElementType);

                TraceValueContext ctx = MakeTraceValueContext(pElementType->m_TypeDef, pElementType->m_ModuleID, -1, &resolver, nullptr, nullptr);

                IfFailGo(TraceValue(
                    elmAddress,
                    pArrayInfo->m_CorElementType,
                    &ctx,
                    arrBytesRead
                ));
            }

            ExitLevel(L"Exit Array");
        }
    }

    bytesRead += sizeof(void*);

ErrExit:
    m_TraceDepth++;

    if (dimensionSizes)
        delete[] dimensionSizes;

    if (dimensionLowerBounds)
        delete[] dimensionLowerBounds;

    return hr;
}

#pragma endregion

HRESULT CValueTracer::TraceValueType(
    _In_ UINT_PTR startAddress,
    _In_ TraceValueContext* pContext,
    _Out_opt_ ULONG& bytesRead)
{
    //todo: apparently resolving value types from another assembly is very complicated https://github.com/wickyhu/simple-assembly-explorer/blob/master/SimpleProfiler/ProfilerCallback.cpp
    //lets test with datetime

    HRESULT hr = S_OK;
    ObjectID objectId = startAddress;
    IMetaDataImport2* pMDI = nullptr;
    IClassInfo* pClassInfo;
    CModuleInfo* pModuleInfo;
    ModuleID moduleId = 0;
    mdTypeDef typeDef = 0;

    IfFailGo(GetTypeDefAndModule(pContext->ValueType.ModuleOfTypeToken, pContext->ValueType.TypeToken, &moduleId, &typeDef));

    IfFailGo(g_pProfiler->GetModuleInfo(moduleId, &pModuleInfo));

    //If the type is generic, it should have been handled in another call path (potentially resulting in TraceClassOrStruct() being called directly). If a generic struct goes through this code path GetClassFromToken() will explode
    IfFailGo(GetClassInfoFromTypeDef(pModuleInfo, typeDef, &pClassInfo));

    IfFailGo(TraceClassOrStruct((CClassInfo*)pClassInfo, objectId, ELEMENT_TYPE_VALUETYPE, bytesRead));

ErrExit:
    if (pMDI)
        pMDI->Release();

    return hr;
}

HRESULT CValueTracer::GetTypeDefAndModule(
    _In_ ModuleID moduleOfTypeToken,
    _In_ mdToken typeToken,
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    HRESULT hr = S_OK;

    CorTokenType tokenType = (CorTokenType)TypeFromToken(typeToken);

    if (tokenType == mdtTypeDef)
    {
        *moduleId = moduleOfTypeToken;
        *typeDef = typeToken;
    }
    else if (tokenType == mdtTypeRef)
    {
        CTypeRefResolver resolver(moduleOfTypeToken, typeToken);

        IfFailGo(resolver.Resolve(moduleId, typeDef));
    }
    else
    {
        hr = E_NOTIMPL;
        goto ErrExit;
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::GetClassId(
    _In_ CClassInfo* pClassInfo, //The current class that any ELEMENT_TYPE_VAR references will be resolved from
    _In_ CSigType* pType,
    _In_ ModuleID moduleOfTypeToken,
    _In_ CSigGenericType* curGenericType,
    _In_ ULONG curGenericArg,
    _Out_ ClassID* classId)
{
    HRESULT hr = S_OK;

    ClassID* typeArgIds = nullptr;
    ModuleID moduleId;
    mdTypeDef typeDef;
    WCHAR typeName[NAME_BUFFER_SIZE];

    if (pType->m_Type == ELEMENT_TYPE_GENERICINST)
    {
        CSigGenericType* pGenericType = (CSigGenericType*) pType;
        ClassID childClassId;

        typeArgIds = new ClassID[pGenericType->m_NumGenericArgs];

        for (ULONG i = 0; i < pGenericType->m_NumGenericArgs; i++)
        {
            CSigType* current = pGenericType->m_GenericArgs[i];

            IfFailGo(GetClassId(
                pClassInfo,
                current,
                moduleOfTypeToken,
                pGenericType,
                i,
                &childClassId
            ));

            typeArgIds[i] = childClassId;
        }

        ModuleID outerModuleId;
        mdTypeDef outerTypeDef;
        IfFailGo(GetTypeDefAndModule(moduleOfTypeToken, pGenericType->m_GenericTypeDefinitionToken, &outerModuleId, &outerTypeDef));

        CModuleInfo* moduleInfo = g_pProfiler->m_ModuleInfoMap[outerModuleId];

        moduleInfo->m_pMDI->GetTypeDefProps(outerTypeDef, typeName, NAME_BUFFER_SIZE, NULL, NULL, NULL);

        IfFailGo(GetCachedGenericType(outerModuleId, outerTypeDef, pGenericType->m_NumGenericArgs, typeArgIds, classId));

        //GetCachedGenericType returns S_OK on success, S_FALSE on failure and COR_E_TYPELOAD if we couldn't resolve the type to use below
        if (hr == S_FALSE)
        {
            hr = GetAndCacheNewGenericType(outerModuleId, outerTypeDef, pGenericType, typeArgIds, classId);
        }

        IfFailGo(hr);
    }
    else
    {
        switch (pType->m_Type)
        {
        #pragma region CStandardInfoType

        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_STRING: //String gets a CStandardTypeInfo, which is never used since ELEMENT_TYPE_STRING gets processed specially
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_OBJECT:
        {
            CLock classLock(&g_pProfiler->m_ClassMutex);

            auto match = g_pProfiler->m_StandardTypeMap.find(pType->m_Type);

            if (match == g_pProfiler->m_StandardTypeMap.end())
            {
                hr = E_FAIL;
                goto ErrExit;
            }

            *classId = match->second->m_ClassID;

            break;
        }

        #pragma endregion
        #pragma region Generic Type

        case ELEMENT_TYPE_VAR:
        {
            *classId = pClassInfo->m_GenericTypeArgs[((CSigTypeGenericArgType*)pType)->m_Index];
            break;
        }

        case ELEMENT_TYPE_MVAR:
        {
            IClassInfo* info = m_MethodGenericTypeArgs[((CSigMethodGenericArgType*)pType)->m_Index];

            *classId = info->m_ClassID;
            break;
        }

        #pragma endregion
        #pragma region Class / ValueType

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            mdToken token = GetTypeToken(pType);

            _ASSERTE(token != mdTokenNil);

            IfFailGo(GetTypeDefAndModule(moduleOfTypeToken, token, &moduleId, &typeDef));

            IfFailGo(g_pProfiler->m_pInfo->GetClassFromToken(moduleId, typeDef, classId));
            break;
        }

        #pragma endregion

        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
        {
            IfFailGo(GetArrayClassId(pClassInfo, pType, moduleOfTypeToken, curGenericType, curGenericArg, classId));
            break;
        }

        case ELEMENT_TYPE_END:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_FNPTR:
        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
        case ELEMENT_TYPE_INTERNAL:
        case ELEMENT_TYPE_MAX:
        case ELEMENT_TYPE_MODIFIER:
        case ELEMENT_TYPE_SENTINEL:
        case ELEMENT_TYPE_PINNED:
        default:
            hr = PROFILER_E_GENERICCLASSID;
            break;
        }
    }

ErrExit:
    if (typeArgIds != nullptr)
        delete[] typeArgIds;

    return hr;
}

HRESULT CValueTracer::GetCachedGenericType(
    _In_ ModuleID moduleId,
    _In_ mdTypeDef typeDef,
    _In_ ULONG numGenericArgs,
    _In_ ClassID* typeArgs,
    _Out_ ClassID* pClassId)
{
    //If we failed to query the given moduleId/typeDef/typeArgs combination previously (because it threw an EETypeLoadException),
    //we will have cached what the canonical generic type of this type is. Use that, rather than attempting to load again
    //and causing another EETypeLoadException

    CTypeIdentifier typeIdentifier(moduleId, typeDef, numGenericArgs, typeArgs, FALSE);

    CLock lock(&g_pProfiler->m_TypeIdMutex);

    auto match = g_pProfiler->m_TypeIdMap.find(typeIdentifier);

    if (match != g_pProfiler->m_TypeIdMap.end())
    {
        if (match->first.m_Failed)
            return COR_E_TYPELOAD;

        *pClassId = match->second;
        return S_OK;
    }

    return S_FALSE;
}

HRESULT CValueTracer::GetAndCacheNewGenericType(
    _In_ ModuleID outerModuleId,
    _In_ mdTypeDef outerTypeDef,
    _In_ CSigGenericType* pGenericType,
    _In_ ClassID* typeArgIds,
    _Out_ ClassID* classId)
{
    HRESULT hr = g_pProfiler->m_pInfo->GetClassFromTokenAndTypeArgs(
        outerModuleId,
        outerTypeDef,
        pGenericType->m_NumGenericArgs,
        typeArgIds,
        classId
    );

    if (hr == COR_E_TYPELOAD)
    {
        /* We failed to load the generic type. Each time a COR_E_TYPELOAD occurs, an EETypeLoadException is
         * thrown within the CLR. In a large program like Visual Studio you can easily generate close to 100,000
         * exceptions simply trying to start the program, drastically slowing things down. As such, we
         * attempt to fallback to using the generic type's canonical type instead. In many cases,
         * it may not matter what the generic type args were, or it will matter but we'll end up
         * being able to query this info later on (e.g. an array is of type T[] and we can simply
         * ask what the array's element type is). Worst case scenario we'll attempt to trace System.__Canon
         * which you'd _think_ behaves just like a regular object */

        dprintf(L"Type Load Error with module 0x" FORMAT_PTR ", type 0x" FORMAT_PTR "\n", outerModuleId, outerTypeDef);

        CLock classLock(&g_pProfiler->m_ClassMutex);

        for (auto& item : g_pProfiler->m_CanonicalGenericTypes)
        {
            if (item->m_ModuleID == outerModuleId && item->m_TypeDef == outerTypeDef)
            {
                *classId = item->m_ClassID;

                //No point owning the array because a copy will be made upon inserting it into the map anyway
                CTypeIdentifier identifier(outerModuleId, outerTypeDef, pGenericType->m_NumGenericArgs, typeArgIds, FALSE);

                CLock typeLock(&g_pProfiler->m_TypeIdMutex, true);
                g_pProfiler->m_TypeIdMap[identifier] = item->m_ClassID;

                hr = S_OK;
                break;
            }
        }

        if (hr != S_OK)
        {
            dprintf(L"Failed to resolve module 0x" FORMAT_PTR ", type 0x" FORMAT_PTR ". Flagging as failed\n", outerModuleId, outerTypeDef);

            CTypeIdentifier identifier(outerModuleId, outerTypeDef, pGenericType->m_NumGenericArgs, typeArgIds, FALSE);
            identifier.m_Failed = TRUE;

            CLock typeLock(&g_pProfiler->m_TypeIdMutex, true);
            g_pProfiler->m_TypeIdMap[identifier] = 0;
        }
    }

    CLock typeLock(&g_pProfiler->m_TypeIdMutex, true);

    CTypeIdentifier identifier(outerModuleId, outerTypeDef, pGenericType->m_NumGenericArgs, typeArgIds, FALSE);

    if (hr == S_OK)
    {
        g_pProfiler->m_TypeIdMap[identifier] = *classId;
    }
    else
    {
        g_pProfiler->m_TypeIdMap[identifier] = 0;
    }

    return hr;
}

HRESULT CValueTracer::GetArrayClassId(
    _In_ CClassInfo* pClassInfo, //The current class that any ELEMENT_TYPE_VAR references will be resolved from
    _In_ CSigType* pType,
    _In_ ModuleID moduleOfTypeToken,
    _In_ CSigGenericType* curGenericType,
    _In_ ULONG curGenericArg,
    _Out_ ClassID* classId)
{
    HRESULT hr = S_OK;
    ISigArrayType* arr = (ISigArrayType*)(pType);
    ClassID elmClassId;

    IfFailGo(GetClassId(
        pClassInfo,
        arr->m_pElementType,
        moduleOfTypeToken,
        curGenericType,
        curGenericArg,
        &elmClassId
    ));

    /* We have a generic instance like new GenericValueTypeType<Struct1WithField[,]>.We've got the class ID
     * of Struct1WithField (the element type), and we need to get the class ID of Struct1WithField[,] (the array itself).
     * If the array type has been seen before, it's been cached on m_ArrayTypeMap; if not, we need to search all the classes
     * for a GenericValueTypeType<> containing a compatible array of type Struct1WithField */
    if (GetArrayClassIdFast(arr, elmClassId, classId) != S_OK)
    {
        if (curGenericType == NULL)
            return E_FAIL;

        IfFailGo(GetArrayClassIdSlow(arr, elmClassId, curGenericType, curGenericArg, classId));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::GetArrayClassIdFast(
    _In_ ISigArrayType* arr,
    _In_ ClassID elmClassId,
    _Out_ ClassID* classId)
{
    CLock classLock(&g_pProfiler->m_ClassMutex);

    auto match = g_pProfiler->m_ArrayTypeMap.find(elmClassId);

    if (match != g_pProfiler->m_ArrayTypeMap.end())
    {
        CUnknownArray<CArrayInfo>* unkArr = match->second;

        for(ULONG i = 0; i < match->second->m_Length; i++)
        {
            CArrayInfo* item = unkArr->operator[](i);

            //We know elmClassId matches beacuse it was the index into m_ArrayTypeMap
            if (IsArrayMatch(arr, item))
            {
                *classId = item->m_ClassID;
                return S_OK;
            }
        }
    }

    return E_FAIL;
}

HRESULT CValueTracer::GetArrayClassIdSlow(
    _In_ ISigArrayType* arr,
    _In_ ClassID elmClassId,
    _In_ CSigGenericType* curGenericType,
    _In_ ULONG curGenericArg,
    _Out_ ClassID* classId)
{
    HRESULT hr = S_OK;

    CLock classLock(&g_pProfiler->m_ClassMutex, true);

#if _DEBUG
    //For debugging which class we're working with here
    IClassInfo* elmClassInfo;

    g_pProfiler->GetClassInfoFromClassId(elmClassId, &elmClassInfo, false);
#endif

    for (auto& v : g_pProfiler->m_ClassInfoMap)
    {
        if (v.second->m_InfoType == ClassInfoType::Class)
        {
            CClassInfo* info = (CClassInfo*)v.second;

            if (wcscmp(info->m_szName, curGenericType->m_szGenericTypeDefinitionName) == 0 && info->m_NumGenericTypeArgs == curGenericType->m_NumGenericArgs)
            {
                //Even if there's multiple generic types that define this array at a given generic type arg index, this doesn't matter;
                //the definition of the array is still the same. We'll get the true class ID of the generic type via GetClassFromTokenAndTypeArgs()
                IClassInfo* genericTypeArg;
                IfFailGo(g_pProfiler->GetClassInfoFromClassId(info->m_GenericTypeArgs[curGenericArg], &genericTypeArg, false));

                if (genericTypeArg->m_InfoType == ClassInfoType::Array)
                {
                    CArrayInfo* curArr = (CArrayInfo*)genericTypeArg;

                    if (curArr->m_pElementType->m_ClassID == elmClassId && IsArrayMatch(arr, curArr))
                    {
                        *classId = curArr->m_ClassID;
                        return S_OK;
                    }
                }
            }
        }
    }

    hr = PROFILER_E_UNKNOWN_GENERIC_ARRAY;

ErrExit:
    return hr;
}

BOOL CValueTracer::IsArrayMatch(ISigArrayType* arr, CArrayInfo* info)
{
    //We know elmClassId matches if this method was called; now we just need to compare the nature of the array

    if (arr->GetRank() != info->m_Rank)
        return FALSE;

    //In order to compare the sizes of the dimensions and the lower bounds, we need to call GetArrayObjectInfo() which relies on an ObjectID

    return TRUE;
}

HRESULT CValueTracer::GetClassInfoFromTypeDef(
    CModuleInfo* pModuleInfo,
    mdTypeDef typeDef,
    IClassInfo** ppClassInfo)
{
    HRESULT hr = S_OK;
    ClassID classId;
    IClassInfo* pClassInfo;

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromToken(pModuleInfo->m_ModuleID, typeDef, &classId));

    IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, &pClassInfo));

    *ppClassInfo = pClassInfo;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceTypeGenericType(
    _In_ UINT_PTR startAddress,
    _In_ TraceValueContext* pContext,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;
    ClassID classId;

    CClassInfo* parentType;
    IClassInfo* genericArgInfo;
    IfFailGo(pContext->GenericArg.ClassInfoResolver->Resolve(&parentType));

    classId = parentType->m_GenericTypeArgs[pContext->GenericArg.GenericIndex];

    IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, &genericArgInfo));
    IfFailGo(TraceGenericTypeInternal(startAddress, genericArgInfo, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceMethodGenericType(_In_ UINT_PTR startAddress, _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;
    CClassInfo* genericType;
    IClassInfo* info;

    //Both MVAR and VAR args are resolved in the resolver

    IfFailGo(pContext->GenericArg.ClassInfoResolver->Resolve(&genericType));

    info = m_MethodGenericTypeArgs[pContext->GenericArg.GenericIndex];

    IfFailGo(TraceGenericTypeInternal(startAddress, info, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceGenericTypeInternal(
    _In_ UINT_PTR startAddress,
    _In_ IClassInfo* info,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    ObjectID objectId = *(ObjectID*)startAddress;

    switch (info->m_InfoType)
    {
    case ClassInfoType::StandardType:
    {
        CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)info;

        return TraceSimpleValue(
            startAddress,
            pStandardTypeInfo->m_ElementType,
            bytesRead
        );
    }

    case ClassInfoType::Class:
    {
        CClassInfo* pClassInfo = (CClassInfo*)info;

        ULONG32 pBufferOffset;

        //All GetBoxClassLayout() does is return Object::GetOffsetOfFirstField() (which is sizeof(Object)) provided classId points to a value type.
        //Therefore this method can easily be used to check whether a type is a value type or not, regardless of whether a particular instance of it
        //is boxed or not
        if (g_pProfiler->m_pInfo->GetBoxClassLayout(pClassInfo->m_ClassID, &pBufferOffset) == S_OK)
            IfFailGo(TraceClassOrStruct(pClassInfo, startAddress, ELEMENT_TYPE_VALUETYPE, bytesRead));
        else
        {
            if (IsInvalidObject(objectId))
            {
                //It's a null object
                ULONG size = 0;

                DebugBlob(L"Null Class");
                WriteType(ELEMENT_TYPE_CLASS);
                WriteValue(&size, 4);

                bytesRead += sizeof(void*);
                goto ErrExit;
            }

            ULONG innerBytesRead = 0;
            IfFailGo(TraceClassOrStruct(pClassInfo, objectId, ELEMENT_TYPE_CLASS, innerBytesRead));
            bytesRead += sizeof(void*); //ValueType will increase bytesRead but class won't
        }

        break;
    }

    //In the case of specifying an array as a generic parameter for a method (an ELEMENT_TYPE_MVAR-like scenario), the parameter type will end up
    //simply being ELEMENT_TYPE_SZARRAY rather than ELEMENT_TYPE_MVAR, so this codepath isn't hit. But if an aray is specified as a generic parameter to a type,
    //it is hit
    case ClassInfoType::Array:
    {
        CArrayInfo* arr = (CArrayInfo*)info;

        if (IsInvalidObject(objectId))
        {
            DebugBlob(L"Null Array");
            if (arr->m_Rank == 1)
                WriteType(ELEMENT_TYPE_SZARRAY);
            else
            {
                WriteType(ELEMENT_TYPE_ARRAY);
                WriteValue(&arr->m_Rank, 4);
            }

            WriteType(ELEMENT_TYPE_END);

            bytesRead += sizeof(void*);

            return S_OK;
        }

        IfFailGo(TraceArrayInternal(arr, objectId, bytesRead)); //bytesRead will be increased

        break;
    }
    default:
        hr = E_FAIL;
        break;
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TracePtrType(
    _In_ UINT_PTR startAddress,
    _In_ TraceValueContext* pContext,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;
    UINT_PTR innerAddress = *(PUINT_PTR)startAddress;
    ULONG innerBytesRead = 0;
    BOOL revertDepth = FALSE;

    CSigType* elm = pContext->Ptr.PtrType->m_pPtrType;
    CorElementType elmType = elm->m_Type;

    mdToken typeToken = GetTypeToken(elm);

    TraceValueContext ctx = MakeTraceValueContext(
        typeToken,
        pContext->ValueType.ModuleOfTypeToken,
        -1,
        pContext->GenericArg.ClassInfoResolver,
        elm,
        pContext->Ptr.PtrType
    );

    DebugBlob(L"Ptr");
    WriteType(ELEMENT_TYPE_PTR);

    if (s_IgnorePointerValue || IsInvalidPointer(innerAddress))
    {
        WriteType(ELEMENT_TYPE_END);
        WriteType(elmType);

        IfFailGo(TraceIntPtr(startAddress, innerBytesRead));
    }
    else
    {
        WriteType(elmType);

        if (elmType == ELEMENT_TYPE_VOID)
        {
            IfFailGo(TraceIntPtr(startAddress, innerBytesRead));
        }
        else
        {
            //If we can't read what this pointer points to, it's a useless value
            m_TraceDepth--;
            revertDepth = TRUE;
            IfFailGo(TraceValue(
                innerAddress,
                elmType,
                &ctx,
                innerBytesRead
            ));
        }
    }

    bytesRead += sizeof(void*);

ErrExit:
    if (revertDepth)
        m_TraceDepth++;

    return hr;
}

HRESULT CValueTracer::TraceFnPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    DebugBlob(L"FnPtr");
    __int64 addr = (__int64)*(UINT_PTR*)startAddress;

    Write(&addr, ELEMENT_TYPE_FNPTR, 8);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceClassOrStruct(CClassInfo* pClassInfo, ObjectID objectId, CorElementType elementType, ULONG& bytesRead)
{
    HRESULT hr = S_OK;
    ULONG nameLength;
    long genericIndex = -1;
    ULONG innerBytesRead = 0;

    EnterLevel(L"%s (ObjectId: " FORMAT_PTR L") !DumpVC 0x" FORMAT_PTR L" 0x" FORMAT_PTR, pClassInfo->m_szName, objectId, pClassInfo->m_ClassID, objectId);

    DebugBlob(L"Class Or Struct Type");
    WriteType(elementType);

    nameLength = (ULONG)wcslen(pClassInfo->m_szName) + 1;

    DebugBlob(L"Class Or Struct Name");
    WriteValue(&nameLength, 4);
    WriteValue(pClassInfo->m_szName, (nameLength - 1) * sizeof(WCHAR));
    WriteValue(L"\0", sizeof(WCHAR));

    DebugBlob(L"Class TypeDef");
    WriteValue(&pClassInfo->m_TypeDef, 4);

    DebugBlob(L"Class Module");
    WriteValue(&pClassInfo->m_UniqueModuleID, 4);

    DebugBlobCtx(L"Class Or Struct Num Fields", pClassInfo->m_szName);
    WriteValue(&pClassInfo->m_NumFields, 4);

    for (ULONG i = 0; i < pClassInfo->m_NumFields; i++)
    {
        DebugBlobCtx(L"\n*** TRACE FIELD ***", pClassInfo->m_szName);

        COR_FIELD_OFFSET offset = pClassInfo->m_FieldOffsets[i];
        CSigField* field = pClassInfo->m_Fields[i];

        CorElementType type = field->m_pType->m_Type;
        UINT_PTR fieldAddress = objectId + offset.ulOffset;

        GetGenericInfo(field->m_pType, &genericIndex);

        CClassInfoResolver resolver(pClassInfo);

        TraceValueContext ctx = MakeTraceValueContext(
            GetTypeToken(field->m_pType),
            pClassInfo->m_ModuleID,
            genericIndex,
            &resolver,
            field->m_pType,
            nullptr
        );

        EnterLevel(L".%s (FieldAddress: 0x" FORMAT_PTR L", mdFieldDef: 0x" FORMAT_PTR L", ObjectId: 0x" FORMAT_PTR L", Offset: %d, InnerBytesRead: %d)", field->m_szName, fieldAddress, offset.ridOfField, objectId, offset.ulOffset, innerBytesRead);

        IfFailGo(TraceValue(
            fieldAddress,
            field->m_pType->m_Type,
            &ctx,
            innerBytesRead //todo: we should store an inner bytes read and then assign it to bytesread after alignment
        ));

        ExitLevel(L"Exit Field InnerBytesRead: %d", innerBytesRead);
    }

    _ASSERTE(elementType == ELEMENT_TYPE_CLASS || elementType == ELEMENT_TYPE_OBJECT || elementType == ELEMENT_TYPE_VALUETYPE);

    if (elementType == ELEMENT_TYPE_VALUETYPE)
    {
        /* Structures must be aligned to a certain extent. To demonstrate this, consider a Dictionary<string, int>. This type contains a field private Dictionary<TKey, TValue>.Entry[] entries, defined as
         * 
         *   public int hashCode;
         *   public int next;
         *   public TKey key;
         *   public TValue value;
         * 
         * On x64, the size of this structure would be 20 bytes. If you have two entries in this dictionary, things will explode upon trying to read the second entry, because we only looked 20 bytes ahead for the second element, when in fact
         * we needed to look 24 bytes ahead. We would similarly have an issue if the structure was less than a pointer large. The following alignment logic is taken from MethodTableBuilder::PlaceInstanceFields()
         *
         * Fortunately, GetClassLayout returns the properly aligned size, so we don't have to manually align it ourselves. One potential challenge is the fact that according to the CLR,
         * when a value type is boxed its true size is in fact 12 bytes. See SetupMethodTable2 in methodtablebuilder.cpp which calls pMT->SetBaseSize.
         */

        // The JITs like to copy full machine words,
        // so if the size is bigger than a void* round it up to minAlign
        // and if the size is smaller than void* round it up to next power of two
        innerBytesRead = pClassInfo->m_ClassSize;
    }

    ExitLevel(L"Exit Class/Struct %d -> %d", bytesRead, (bytesRead + innerBytesRead));

    bytesRead += innerBytesRead; //If this is a class, the bytesRead reported from this method will be ignored and sizeof(void*) will be used instead

ErrExit:
    return hr;
}

mdToken CValueTracer::GetTypeToken(CSigType* pType)
{
    mdToken typeToken = mdTokenNil;

    switch (pType->m_Type)
    {
    case ELEMENT_TYPE_CLASS:
        typeToken = ((CSigClassType*)pType)->m_Token;
        break;
    case ELEMENT_TYPE_VALUETYPE:
        typeToken = ((CSigValueType*)pType)->m_Token;
        break;
    case ELEMENT_TYPE_GENERICINST:
        typeToken = ((CSigGenericType*)pType)->m_GenericTypeDefinitionToken;
        break;
    default:
        break;
    }

    return typeToken;
}

void CValueTracer::GetGenericInfo(
    CSigType* pType,
    long* genericIndex)
{
    switch (pType->m_Type)
    {
    case ELEMENT_TYPE_VAR:
        *genericIndex = ((CSigTypeGenericArgType*)pType)->m_Index;
        break;

    case ELEMENT_TYPE_MVAR:
        *genericIndex = ((CSigMethodGenericArgType*)pType)->m_Index;
        break;
    }
}

BOOL CValueTracer::IsInvalidObject(ObjectID objectId)
{
    return objectId == NULL;
}

BOOL CValueTracer::IsInvalidPointer(ObjectID objectId)
{
    if (objectId == NULL)
        return TRUE;

#if defined(_X86_)
    if (objectId == (ULONG)-1)
        return TRUE;
#elif defined(_AMD64_)
    if (((objectId & 0xffffffff00000000) >> 32) == (ULONG)-1)
        return TRUE;
#endif

    //Cannot use destructors in functions that also use SEH
    g_pProfiler->m_ObjectIdBlacklistMutex.lock();

    BOOL invalid = FALSE;

    //find() returns an object that needs to be destructed, which cannot be called within a function that uses SEH
    if (g_pProfiler->IsObjectIdBlacklisted(objectId))
    {
        invalid = TRUE;
        goto Exit;
    }

    __try
    {
         //This is marked as volatile so that in Release builds the entire dereference on objectId isn't optimized away
        volatile UINT_PTR val = *(UINT_PTR*)objectId;
    }
    __except (GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        invalid = TRUE;
        g_pProfiler->m_ObjectIdBlacklist[objectId] = 1;
    }

Exit:
    g_pProfiler->m_ObjectIdBlacklistMutex.unlock();

    return invalid;
}

HRESULT CValueTracer::GetMethodInfoNoLock(FunctionIDOrClientID functionId, _Out_ CSigMethodDef** ppMethod)
{
    auto match = g_pProfiler->m_MethodInfoMap.find(functionId.functionID);

    if (match == g_pProfiler->m_MethodInfoMap.end())
    {
        dprintf(L"Unknown func " FORMAT_PTR " called\n", functionId.functionID);
        DebugBreakSafe();

        return PROFILER_E_UNKNOWN_METHOD;
    }

    CSigMethodDef* pMethod = match->second;

    if (!pMethod)
    {
        DebugBreakSafe();

        dprintf(L"Unknown func " FORMAT_PTR " called\n", functionId.functionID);
        return E_FAIL;
    }

    *ppMethod = pMethod;
    return S_OK;
}

HRESULT CValueTracer::GetFieldValue(
    _In_ void* pAddress,
    _In_ CClassInfo* pInfo,
    _In_ CSigField* pField)
{
    HRESULT hr = S_OK;
    mdToken typeToken = GetTypeToken(pField->m_pType);

    long genericIndex = -1;
    GetGenericInfo(pField->m_pType, &genericIndex);

    CClassInfoResolver resolver(pInfo);

    TraceValueContext ctx = MakeTraceValueContext(
        typeToken,
        pInfo->m_ModuleID,
        genericIndex,
        &resolver,
        pField->m_pType,
        nullptr
    );

    ULONG bytesRead = 0;

    __try
    {
        hr = TraceValue((UINT_PTR) pAddress, pField->m_pType->m_Type, &ctx, bytesRead);
    }
    __except (GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        hr = PROFILER_E_STATICFIELD_INVALID_MEMORY;
    }

    return hr;
}