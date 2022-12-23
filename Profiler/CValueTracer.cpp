#include "pch.h"
#include "CValueTracer.h"
#include "CCorProfilerCallback.h"
#include "DebugToolsProfiler.h"

#define VALUE_BUFFER_SIZE 10000

thread_local std::unordered_map<UINT_PTR, int> g_SeenMap;
thread_local BYTE g_ValueBuffer[VALUE_BUFFER_SIZE];
thread_local ULONG g_ValueBufferPosition = 0;
thread_local ModuleID g_ModuleID;

HRESULT CValueTracer::Initialize()
{
    HRESULT hr = S_OK;

    IfFailGo(m_pInfo->GetStringLayout2(&m_StringLengthOffset, &m_StringBufferOffset));

ErrExit:
    return hr;
}

HRESULT CValueTracer::EnterWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

    g_SeenMap.clear();
    g_ValueBufferPosition = 0;

    CSigMethodDef* pMethod = nullptr;

    CCorProfilerCallback* pProfiler = CCorProfilerCallback::g_pProfiler;

    //GetFunctionEnter3Info
    COR_PRF_ELT_INFO frameInfo;
    ULONG cbArgumentInfo = 0;
    COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo = nullptr;

    pProfiler->m_MethodMutex.lock();

    pMethod = pProfiler->m_MethodInfoMap[functionId.functionID];

    pProfiler->m_MethodMutex.unlock();

    if (!pMethod)
        return E_FAIL;

    g_ModuleID = pMethod->m_ModuleID;

    if (pMethod->m_NumParameters == 0)
    {
        WriteValue(&pMethod->m_NumParameters, 4);
        goto ErrExit;
    }

    hr = pProfiler->m_pInfo->GetFunctionEnter3Info(
        functionId.functionID,
        eltInfo,
        &frameInfo,
        &cbArgumentInfo,
        nullptr
    );

    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        argumentInfo = static_cast<COR_PRF_FUNCTION_ARGUMENT_INFO*>(malloc(cbArgumentInfo));

        IfFailGo(pProfiler->m_pInfo->GetFunctionEnter3Info(
            functionId.functionID,
            eltInfo,
            &frameInfo,
            &cbArgumentInfo,
            argumentInfo
        ));

        IfFailGo(TraceParameters(argumentInfo, pMethod));
    }

ErrExit:
    EventWriteCallEnterDetailedEvent(functionId.functionID, hr, g_ValueBufferPosition, g_ValueBuffer);

    if (argumentInfo != nullptr)
        free(argumentInfo);

    return hr;
}

HRESULT CValueTracer::LeaveWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

ErrExit:
    EventWriteCallExitDetailedEvent(functionId.functionID, hr, 0, NULL);

    return hr;
}

HRESULT CValueTracer::TailcallWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

ErrExit:
    EventWriteTailcallDetailedEvent(functionId.functionID, hr, 0, NULL);

    return hr;
}

HRESULT CValueTracer::TraceParameters(COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo, CSigMethodDef* pMethod)
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

    WriteValue(&validRanges, 4);

    for(ULONG i = 0; i < validRanges; i++)
    {
        COR_PRF_FUNCTION_ARGUMENT_RANGE range = argumentInfo->ranges[i + offset];

        ISigParameter* pParameter = pMethod->m_Parameters[i];

        IfFailGo(TraceParameter(&range, pParameter));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceParameter(COR_PRF_FUNCTION_ARGUMENT_RANGE* range, ISigParameter* pParameter)
{
    HRESULT hr = S_OK;

    UINT_PTR pAddress = pParameter->m_pType->m_IsByRef ? *(UINT_PTR*)range->startAddress : range->startAddress;

    ULONG bytesRead = 0;
    
    CSigType* pType = pParameter->m_pType;

    mdToken typeToken = GetTypeToken(pType);

    IfFailGo(TraceValue(pAddress, pType->m_Type, typeToken, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceValue(
    _In_ UINT_PTR startAddress,
    _In_ CorElementType elementType,
    _In_ mdToken typeToken,
    _Out_opt_ ULONG& bytesRead)
{
    if (g_SeenMap[startAddress] && elementType == ELEMENT_TYPE_CLASS)
        return S_OK;

    g_SeenMap[startAddress] = 1;

    HRESULT hr = S_OK;

    switch (elementType)
    {
    #pragma region Primitives

    case ELEMENT_TYPE_BOOLEAN:
        return TraceBool(startAddress, bytesRead);

    case ELEMENT_TYPE_CHAR:
        return TraceChar(startAddress, bytesRead);

    case ELEMENT_TYPE_I1:
        return TraceSByte(startAddress, bytesRead);

    case ELEMENT_TYPE_U1:
        return TraceByte(startAddress, bytesRead);

    case ELEMENT_TYPE_I2:
        return TraceShort(startAddress, bytesRead);

    case ELEMENT_TYPE_U2:
        return TraceUShort(startAddress, bytesRead);

    case ELEMENT_TYPE_I4:
        return TraceInt(startAddress, bytesRead);

    case ELEMENT_TYPE_U4:
        return TraceUInt(startAddress, bytesRead);

    case ELEMENT_TYPE_I8:
        return TraceLong(startAddress, bytesRead);

    case ELEMENT_TYPE_U8:
        return TraceULong(startAddress, bytesRead);

    case ELEMENT_TYPE_R4:
        return TraceFloat(startAddress, bytesRead);

    case ELEMENT_TYPE_R8:
        return TraceDouble(startAddress, bytesRead);

    case ELEMENT_TYPE_I:
        return TraceIntPtr(startAddress, bytesRead);

    case ELEMENT_TYPE_U:
        return TraceUIntPtr(startAddress, bytesRead);

    #pragma endregion
    #pragma region ObjectID

    case ELEMENT_TYPE_STRING:
        return TraceString(startAddress, bytesRead);

    case ELEMENT_TYPE_CLASS:
        return TraceClass(startAddress, bytesRead);

    case ELEMENT_TYPE_ARRAY:
        return TraceArray(startAddress, bytesRead);

    case ELEMENT_TYPE_GENERICINST:
        return TraceGenericType(startAddress, bytesRead);

    case ELEMENT_TYPE_OBJECT:
        return TraceGenericType(startAddress, bytesRead);

    case ELEMENT_TYPE_SZARRAY:
        return TraceSZArray(startAddress, bytesRead);

    #pragma endregion

    case ELEMENT_TYPE_VALUETYPE:
        return TraceValueType(startAddress, typeToken, bytesRead);

    case ELEMENT_TYPE_VAR:
        return TraceTypeGenericType(startAddress, bytesRead);

    case ELEMENT_TYPE_MVAR:
        return TraceMethodGenericType(startAddress, bytesRead);

    case ELEMENT_TYPE_PTR:
        return TracePtrType(startAddress, bytesRead);

    case ELEMENT_TYPE_FNPTR:
        return TraceFnPtr(startAddress, bytesRead);

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

    return hr;
}

#pragma region Primitives

HRESULT CValueTracer::TraceBool(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    BOOL* pBool = (BOOL*)startAddress;

    Write((BYTE*)pBool, ELEMENT_TYPE_BOOLEAN, 1);
    bytesRead += sizeof(BOOL);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceChar(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    WCHAR* pChar = (WCHAR*)startAddress;

    Write(pChar, ELEMENT_TYPE_CHAR, 2);
    bytesRead += sizeof(WCHAR);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceSByte(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    signed char* pByte = (signed char*)startAddress;

    Write(pByte, ELEMENT_TYPE_I1, 1);
    bytesRead += sizeof(signed char);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceByte(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    BYTE* pByte = (BYTE*)startAddress;

    Write(pByte, ELEMENT_TYPE_U1, 1);
    bytesRead += sizeof(BYTE);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceShort(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    SHORT* pShort = (SHORT*)startAddress;

    Write(pShort, ELEMENT_TYPE_I2, 2);
    bytesRead += sizeof(SHORT);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUShort(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    USHORT* pShort = (USHORT*)startAddress;

    Write(pShort, ELEMENT_TYPE_U2, 2);
    bytesRead += sizeof(USHORT);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceInt(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    INT32* pInt = (INT32*)startAddress;

    Write(pInt, ELEMENT_TYPE_I4, 4);
    bytesRead += sizeof(INT32);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUInt(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    UINT32* pInt = (UINT32*)startAddress;

    Write(pInt, ELEMENT_TYPE_U4, 4);
    bytesRead += sizeof(UINT32);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceLong(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    INT64* pInt = (INT64*)startAddress;

    Write(pInt, ELEMENT_TYPE_I8, 8);
    bytesRead += sizeof(INT64);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceULong(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    UINT64* pInt = (UINT64*)startAddress;

    Write(pInt, ELEMENT_TYPE_U8, 8);
    bytesRead += sizeof(UINT64);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceFloat(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    FLOAT* pFloat = (FLOAT*)startAddress;

    Write(pFloat, ELEMENT_TYPE_R4, 4);
    bytesRead += sizeof(FLOAT);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceDouble(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    DOUBLE* pDouble = (DOUBLE*)startAddress;

    Write(pDouble, ELEMENT_TYPE_R8, 8);
    bytesRead += sizeof(DOUBLE);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    INT_PTR* pPtr = (INT_PTR*)startAddress;

    Write(pPtr, ELEMENT_TYPE_I, 8);
    bytesRead += sizeof(INT_PTR);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    UINT_PTR* pPtr = (UINT_PTR*)startAddress;

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

    if (objectId == NULL)
    {
        ULONG size = 0;

        WriteType(ELEMENT_TYPE_STRING);
        WriteValue(&size, 4);

        bytesRead += sizeof(void*);
        return hr;
    }

    length = *(ULONG*)((BYTE*)objectId + m_StringLengthOffset) + 1;
    buffer = (LPWSTR)((BYTE*)objectId + m_StringBufferOffset);

    WriteType(ELEMENT_TYPE_STRING);
    WriteValue(&length, 4);
    WriteValue(buffer, (length - 1) * sizeof(WCHAR));
    WriteValue(L"\0", sizeof(WCHAR));

    bytesRead += sizeof(void*);

ErrExit:
    return S_OK;
}

HRESULT CValueTracer::TraceClass(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    //https://chnasarre.medium.com/accessing-arrays-and-class-fields-with-net-profiling-apis-d5ff21114a5d
    //https://github.com/wickyhu/simple-assembly-explorer/blob/8686fe5b82194b091dcfef4d29e78775591258a8/SimpleProfiler/ProfilerCallback.cpp

    HRESULT hr = S_OK;

    ObjectID objectId = *(ObjectID*)startAddress;

    if (objectId == NULL)
    {
        //It's a null object
        bytesRead += sizeof(void*);
        return S_OK;
    }

    ClassID classId;
    IClassInfo* info = nullptr;

    IfFailGo(m_pInfo->GetClassFromObject(objectId, &classId));

    IfFailGo(GetClassInfo(classId, &info));

    if (info->m_IsArray)
    {
        CArrayInfo* pArrayInfo = (CArrayInfo*)info;

        if (pArrayInfo->m_Rank == 1)
            IfFailGo(TraceSZArray(startAddress, bytesRead));
        else
            IfFailGo(TraceArray(startAddress, bytesRead));

        goto ErrExit;
    }
    else
    {
        if (info->m_IsString)
        {
            IfFailGo(TraceString(startAddress, bytesRead));
            goto ErrExit;
        }

        CClassInfo* pClassInfo = (CClassInfo*)info;

        IfFailGo(TraceClassOrStruct(pClassInfo, objectId, ELEMENT_TYPE_CLASS));
    }

    bytesRead += sizeof(void*);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceGenericType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceObject(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceSZArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    ObjectID objectId = *(ObjectID*)startAddress;
    ClassID classId;
    CArrayInfo* info;
    ULONG arrayLength;
    ULONG arrBytesRead = 0;

    IfFailGo(m_pInfo->GetClassFromObject(objectId, &classId));
    IfFailGo(GetClassInfo(classId, (IClassInfo**)&info));

    ULONG32 dimensionSizes[1];
    int dimensionLowerBounds[1];
    BYTE* pData;

    IfFailGo(m_pInfo->GetArrayObjectInfo(objectId, 1, dimensionSizes, dimensionLowerBounds, &pData));

    WriteType(ELEMENT_TYPE_SZARRAY);
    WriteType(info->m_CorElementType);

    arrayLength = dimensionSizes[0];

    WriteValue(&arrayLength, 4);

    for(ULONG i = 0; i < arrayLength; i++)
    {
        //todo: we dont have a csigtype here...do we need one in order to be able to pass a token to tracevaluetype?
        IfFailGo(TraceValue(
            (UINT_PTR)(pData + arrBytesRead),
            info->m_CorElementType,
            ((CClassInfo*) info->m_pElementType)->m_TypeDef, //todo: can we have a carrayinfo inside another carrayinfo?
        arrBytesRead));
    }

    bytesRead += sizeof(void*);

ErrExit:
    return hr;
}

#pragma endregion

HRESULT CValueTracer::TraceValueType(
    _In_ UINT_PTR startAddress,
    _In_ mdToken typeToken,
    _Out_opt_ ULONG& bytesRead)
{
    //todo: apparently resolving value types from another assembly is very complicated https://github.com/wickyhu/simple-assembly-explorer/blob/master/SimpleProfiler/ProfilerCallback.cpp
    //lets test with datetime

    HRESULT hr = S_OK;

    CorTokenType tokenType = (CorTokenType) TypeFromToken(typeToken);

    if (tokenType == mdtTypeDef)
    {
        ClassID classId;
        CClassInfo* pClassInfo;

        IfFailGo(m_pInfo->GetClassFromToken(g_ModuleID, typeToken, &classId));

        IfFailGo(GetClassInfo(classId, (IClassInfo**)&pClassInfo));

        ObjectID objectId = startAddress;

        IfFailGo(TraceClassOrStruct(pClassInfo, objectId, ELEMENT_TYPE_VALUETYPE));
    }
    else if (tokenType == mdtTypeRef)
    {
        hr = E_NOTIMPL;
    }
    else
        hr = E_NOTIMPL;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceTypeGenericType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceMethodGenericType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TracePtrType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceFnPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceClassOrStruct(CClassInfo* pClassInfo, ObjectID objectId, CorElementType elementType)
{
    HRESULT hr = S_OK;
    ULONG nameLength;
    ULONG classBytesRead = 0;

    WriteType(elementType);

    nameLength = (ULONG)wcslen(pClassInfo->m_szName) + 1;

    WriteValue(&nameLength, 4);
    WriteValue(pClassInfo->m_szName, (nameLength - 1) * sizeof(WCHAR));
    WriteValue(L"\0", sizeof(WCHAR));

    WriteValue(&pClassInfo->m_NumFields, 4);

    for (ULONG i = 0; i < pClassInfo->m_NumFields; i++)
    {
        COR_FIELD_OFFSET offset = pClassInfo->m_FieldOffsets[i];
        CSigField* field = pClassInfo->m_Fields[i];

        UINT_PTR fieldAddress = objectId + offset.ulOffset;

        IfFailGo(TraceValue(fieldAddress, field->m_pType->m_Type, GetTypeToken(field->m_pType), classBytesRead));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::GetClassInfo(ClassID classId, IClassInfo** ppClassInfo)
{
    HRESULT hr = S_OK;
    IClassInfo* info = nullptr;

    CCorProfilerCallback::g_pProfiler->m_ClassMutex.lock();
    info = CCorProfilerCallback::g_pProfiler->m_ClassInfoMap[classId];
    CCorProfilerCallback::g_pProfiler->m_ClassMutex.unlock();

    if (!info)
    {
        //Array types don't seem to hit ClassLoadFinished, so if we got an unknown type it's probably because it's an aarray
        IfFailGo(CCorProfilerCallback::g_pProfiler->ClassLoadFinished(classId, S_OK));

        CCorProfilerCallback::g_pProfiler->m_ClassMutex.lock();
        info = CCorProfilerCallback::g_pProfiler->m_ClassInfoMap[classId];
        CCorProfilerCallback::g_pProfiler->m_ClassMutex.unlock();

        if (!info)
        {
            hr = E_FAIL;
        }
    }

ErrExit:
    if (SUCCEEDED(hr))
        *ppClassInfo = info;

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
    default:
        break;
    }

    return typeToken;
}