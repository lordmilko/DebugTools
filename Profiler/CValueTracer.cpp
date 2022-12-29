#include "pch.h"
#include "CValueTracer.h"
#include "CCorProfilerCallback.h"
#include "DebugToolsProfiler.h"
#include "CTypeRefResolver.h"

#define VALUE_BUFFER_SIZE 1000000

thread_local std::unordered_map<UINT_PTR, int> g_SeenMap;
thread_local BYTE g_ValueBuffer[VALUE_BUFFER_SIZE];

//If a value is passed that is longer than VALUE_BUFFER_SIZE, the calculated remaining length will be negative,
//so we need to have a signed buffer length so that the comparison works correctly
thread_local signed long g_ValueBufferPosition = 0;

ULONG CValueTracer::s_StringLengthOffset;
ULONG CValueTracer::s_StringBufferOffset;

HRESULT CValueTracer::Initialize(ICorProfilerInfo3* pInfo)
{
    HRESULT hr = S_OK;

    IfFailGo(pInfo->GetStringLayout2(&s_StringLengthOffset, &s_StringBufferOffset));

ErrExit:
    return hr;
}

HRESULT CValueTracer::EnterWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

    g_SeenMap.clear();
    g_ValueBufferPosition = 0;

    CSigMethodDef* pMethod = nullptr;

    //GetFunctionEnter3Info
    COR_PRF_ELT_INFO frameInfo;
    ULONG cbArgumentInfo = 0;
    COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo = nullptr;
    ClassID* typeArgs = nullptr;
    ClassID classId;
    IClassInfo* pClassInfo;

    CLock methodLock(&g_pProfiler->m_MethodMutex);

    auto match = g_pProfiler->m_MethodInfoMap.find(functionId.functionID);

    if (match == g_pProfiler->m_MethodInfoMap.end())
        return E_FAIL;

    pMethod = match->second;

    if (!pMethod)
        return E_FAIL;

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

        if (pMethod->m_NumGenericTypeArgNames)
        {
            typeArgs = new ClassID[pMethod->m_NumGenericTypeArgNames];
            ULONG32 cTypeArgs;

            IfFailGo(g_pProfiler->m_pInfo->GetFunctionInfo2(
                functionId.functionID,
                frameInfo,
                &classId,
                NULL,
                NULL,
                pMethod->m_NumGenericTypeArgNames,
                &cTypeArgs,
                typeArgs
            ));

            m_GenericTypeArgs = new IClassInfo*[pMethod->m_NumGenericTypeArgNames];

            for(ULONG i = 0; i < pMethod->m_NumGenericTypeArgNames; i++)
            {
                IClassInfo* info;
                IfFailGo(GetClassInfo(typeArgs[i], &info));

                m_GenericTypeArgs[i] = info;
            }
        }
        else
        {
            IfFailGo(g_pProfiler->m_pInfo->GetFunctionInfo2(
                functionId.functionID,
                frameInfo,
                &classId,
                NULL,
                NULL,
                0,
                NULL,
                NULL
            ));
        }

        IfFailGo(GetClassInfo(classId, &pClassInfo));

        IfFailGo(TraceParameters(argumentInfo, pMethod, pClassInfo));
    }

ErrExit:
    EventWriteCallEnterDetailedEvent(functionId.functionID, hr, g_ValueBufferPosition, g_ValueBuffer);

    if (argumentInfo != nullptr)
        free(argumentInfo);

    if (typeArgs)
        delete typeArgs;

    return hr;
}

HRESULT CValueTracer::LeaveWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

    EventWriteCallExitDetailedEvent(functionId.functionID, hr, 0, NULL);

    return hr;
}

HRESULT CValueTracer::TailcallWithInfo(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    HRESULT hr = S_OK;

    EventWriteTailcallDetailedEvent(functionId.functionID, hr, 0, NULL);

    return hr;
}

HRESULT CValueTracer::TraceParameters(
    _In_ COR_PRF_FUNCTION_ARGUMENT_INFO* argumentInfo,
    _In_ CSigMethodDef* pMethod,
    _In_ IClassInfo* pClassInfo)
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

        IfFailGo(TraceParameter(&range, pParameter, pClassInfo, pMethod->m_ModuleID));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceParameter(
    _In_ COR_PRF_FUNCTION_ARGUMENT_RANGE* range,
    _In_ ISigParameter* pParameter,
    _In_ IClassInfo* pClassInfo,
    _In_ ModuleID typeTokenModule)
{
    HRESULT hr = S_OK;

    UINT_PTR pAddress = pParameter->m_pType->m_IsByRef ? *(UINT_PTR*)range->startAddress : range->startAddress;

    ULONG bytesRead = 0;
    
    CSigType* pType = pParameter->m_pType;

    mdToken typeToken = GetTypeToken(pType);
    long genericIndex = GetGenericIndex(pType);

    IfFailGo(TraceValue(
        pAddress,
        pType->m_Type,
        typeToken,
        typeTokenModule,
        genericIndex,
        pClassInfo,
        bytesRead
    ));

ErrExit:
    return hr;
}

//typeTokenModule is the module that applies to the context in which typeToken was resolved. If typeToken is an mdTypeDef, that means it was resolved within the scope of typeTokenModule
HRESULT CValueTracer::TraceValue(
    _In_ UINT_PTR startAddress,
    _In_ CorElementType elementType,
    _In_ mdToken typeToken,
    _In_ ModuleID typeTokenModule,
    _In_ long genericIndex,
    _In_ IClassInfo* pClassInfo,
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
    case ELEMENT_TYPE_OBJECT:
        return TraceClass(startAddress, elementType, bytesRead);

    case ELEMENT_TYPE_ARRAY:
        return TraceArray(startAddress, bytesRead);

    case ELEMENT_TYPE_GENERICINST:
        return TraceGenericType(startAddress, bytesRead);

    case ELEMENT_TYPE_SZARRAY:
        return TraceSZArray(startAddress, bytesRead);

    #pragma endregion

    case ELEMENT_TYPE_VALUETYPE:
        return TraceValueType(startAddress, typeToken, typeTokenModule, bytesRead);

    case ELEMENT_TYPE_VAR:
        return TraceTypeGenericType(startAddress, genericIndex, (CClassInfo*)pClassInfo, bytesRead);

    case ELEMENT_TYPE_MVAR:
        return TraceMethodGenericType(startAddress, genericIndex, bytesRead);

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

    length = *(ULONG*)((BYTE*)objectId + s_StringLengthOffset) + 1;
    buffer = (LPWSTR)((BYTE*)objectId + s_StringBufferOffset);

    WriteType(ELEMENT_TYPE_STRING);
    WriteValue(&length, 4);
    WriteValue(buffer, (length - 1) * sizeof(WCHAR));
    WriteValue(L"\0", sizeof(WCHAR));

    bytesRead += sizeof(void*);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceClass(_In_ UINT_PTR startAddress, _In_ CorElementType classElementType, _Out_opt_ ULONG& bytesRead)
{
    //https://chnasarre.medium.com/accessing-arrays-and-class-fields-with-net-profiling-apis-d5ff21114a5d
    //https://github.com/wickyhu/simple-assembly-explorer/blob/8686fe5b82194b091dcfef4d29e78775591258a8/SimpleProfiler/ProfilerCallback.cpp

    HRESULT hr = S_OK;
    ULONG32 boxedValueOffset;
    ClassID classId;
    IClassInfo* info = nullptr;
    ULONG innerBytesRead;

    ObjectID objectId = *(ObjectID*)startAddress;

    if (objectId == NULL)
    {
        //It's a null object
        ULONG size = 0;

        WriteType(ELEMENT_TYPE_CLASS);
        WriteValue(&size, 4);

        bytesRead += sizeof(void*);
        return S_OK;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromObject(objectId, &classId));

    IfFailGo(GetClassInfo(classId, &info));

    if (info->m_InfoType == ClassInfoType::Array)
    {
        CArrayInfo* pArrayInfo = (CArrayInfo*)info;

        if (pArrayInfo->m_Rank == 1)
            IfFailGo(TraceSZArray(startAddress, innerBytesRead));
        else
            IfFailGo(TraceArray(startAddress, innerBytesRead));
    }
    else
    {
        if (info->m_InfoType == ClassInfoType::StandardType)
        {
            CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)info;

            if (g_pProfiler->m_pInfo->GetBoxClassLayout(classId, &boxedValueOffset) == S_OK)
                IfFailGo(TraceValue(objectId + boxedValueOffset, pStandardTypeInfo->m_ElementType, mdTokenNil, 0, -1, nullptr, innerBytesRead));
            else
                TraceValue(startAddress, pStandardTypeInfo->m_ElementType, mdTokenNil, 0, -1, nullptr, innerBytesRead);
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

HRESULT CValueTracer::TraceArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    return hr;
}

HRESULT CValueTracer::TraceGenericType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    return hr;
}

HRESULT CValueTracer::TraceObject(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    return hr;
}

HRESULT CValueTracer::TraceSZArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    ObjectID objectId = *(ObjectID*)startAddress;
    ClassID classId;
    CArrayInfo* pArrayInfo;

    if (objectId == NULL)
    {
        WriteType(ELEMENT_TYPE_SZARRAY);
        WriteType(ELEMENT_TYPE_END);

        bytesRead += sizeof(void*);

        return S_OK;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromObject(objectId, &classId));
    IfFailGo(GetClassInfo(classId, (IClassInfo**)&pArrayInfo));

    IfFailGo(TraceSZArrayInternal(pArrayInfo, objectId, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceSZArrayInternal(
    _In_ CArrayInfo* pArrayInfo,
    _In_ ObjectID objectId,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    ULONG32 dimensionSizes[1];
    int dimensionLowerBounds[1];
    BYTE* pData;

    ULONG arrayLength;
    ULONG arrBytesRead = 0;

    IfFailGo(g_pProfiler->m_pInfo->GetArrayObjectInfo(objectId, 1, dimensionSizes, dimensionLowerBounds, &pData));

    WriteType(ELEMENT_TYPE_SZARRAY);
    WriteType(pArrayInfo->m_CorElementType);

    arrayLength = dimensionSizes[0];

    WriteValue(&arrayLength, 4);

    if (pArrayInfo->m_pElementType->m_InfoType == ClassInfoType::StandardType)
    {
        CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)pArrayInfo->m_pElementType;

        for (ULONG i = 0; i < arrayLength; i++)
        {
            IfFailGo(TraceValue(
                (UINT_PTR)(pData + arrBytesRead),
                pStandardTypeInfo->m_ElementType,
                mdTokenNil,
                0,
                -1,
                nullptr,
                arrBytesRead
            ));
        }
    }
    else
    {
        //We're assuming we can't have a CArrayInfo inside another CArrayInfo
        CClassInfo* pElementType = (CClassInfo*)pArrayInfo->m_pElementType;

        for (ULONG i = 0; i < arrayLength; i++)
        {
            IfFailGo(TraceValue(
                (UINT_PTR)(pData + arrBytesRead),
                pArrayInfo->m_CorElementType,
                pElementType->m_TypeDef,
                pElementType->m_ModuleID,
                arrBytesRead
            ));
        }
    }

    bytesRead += sizeof(void*);

ErrExit:
    return hr;
}

#pragma endregion

HRESULT CValueTracer::TraceValueType(
    _In_ UINT_PTR startAddress,
    _In_ mdToken typeToken,
    _In_ ModuleID typeTokenModule,
    _Out_opt_ ULONG& bytesRead)
{
    //todo: apparently resolving value types from another assembly is very complicated https://github.com/wickyhu/simple-assembly-explorer/blob/master/SimpleProfiler/ProfilerCallback.cpp
    //lets test with datetime

    HRESULT hr = S_OK;
    ObjectID objectId = startAddress;
    IMetaDataImport2* pMDI = nullptr;
    ClassID classId;
    CClassInfo* pClassInfo;
    ModuleID moduleId = 0;
    mdTypeDef typeDef = 0;

    CorTokenType tokenType = (CorTokenType) TypeFromToken(typeToken);

    if (tokenType == mdtTypeDef)
    {
        moduleId = typeTokenModule;
        typeDef = typeToken;
    }
    else if (tokenType == mdtTypeRef)
    {
        CTypeRefResolver resolver(typeTokenModule, typeToken);

        IfFailGo(resolver.Resolve(&moduleId, &typeDef));
    }
    else
    {
        hr = E_NOTIMPL;
        goto ErrExit;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromToken(moduleId, typeDef, &classId));

    IfFailGo(GetClassInfo(classId, (IClassInfo**)&pClassInfo));

    IfFailGo(TraceClassOrStruct(pClassInfo, objectId, ELEMENT_TYPE_VALUETYPE, bytesRead));

ErrExit:
    if (pMDI)
        pMDI->Release();

    return hr;
}

HRESULT CValueTracer::TraceTypeGenericType(
    _In_ UINT_PTR startAddress,
    _In_ long genericIndex,
    _In_ CClassInfo* pClassInfo,
    _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    IClassInfo* genericInfo;

    ClassID classId = pClassInfo->m_GenericTypeArgs[genericIndex];

    IfFailGo(GetClassInfo(classId, &genericInfo));

    IfFailGo(TraceGenericTypeInternal(startAddress, genericInfo, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceMethodGenericType(_In_ UINT_PTR startAddress, _In_ long genericIndex, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    IClassInfo* info = m_GenericTypeArgs[genericIndex];

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

    ClassID classId;
    ObjectID objectId = *(ObjectID*)startAddress;

    switch (info->m_InfoType)
    {
    case ClassInfoType::StandardType:
    {
        CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)info;

        return TraceValue(
            startAddress,
            pStandardTypeInfo->m_ElementType,
            mdTokenNil,
            0,
            -1,
            (CClassInfo*)info,
            bytesRead
        );
    }
        CClassInfo* pClassInfo = (CClassInfo*)info;

        IfFailGo(g_pProfiler->m_pInfo->GetClassFromToken(pClassInfo->m_ModuleID, pClassInfo->m_TypeDef, &classId));

        ULONG32 pBufferOffset;

        //All GetBoxClassLayout() does is return Object::GetOffsetOfFirstField() (which is sizeof(Object)) provided classId points to a value type.
        //Therefore this method can easily be used to check whether a type is a value type or not, regardless of whether a particular instance of it
        //is boxed or not
        if (g_pProfiler->m_pInfo->GetBoxClassLayout(classId, &pBufferOffset) == S_OK)
            IfFailGo(TraceClassOrStruct(pClassInfo, startAddress, ELEMENT_TYPE_VALUETYPE, bytesRead));
        else
        {
            if (objectId == NULL)
            {
                //It's a null object
                ULONG size = 0;

                WriteType(ELEMENT_TYPE_CLASS);
                WriteValue(&size, 4);

                bytesRead += sizeof(void*);
                goto ErrExit;
            }

            IfFailGo(TraceClassOrStruct(pClassInfo, objectId, ELEMENT_TYPE_CLASS, bytesRead));
            bytesRead += sizeof(void*); //ValueType will increase bytesRead but class won't
        }

        break;
    }

    //In the case of specifying an array as a generic parameter for a method (an ELEMENT_TYPE_MVAR-like scenario), the parameter type will end up
    //simply being ELEMENT_TYPE_SZARRAY rather than ELEMENT_TYPE_MVAR, so this codepath isn't hit. But if an aray is specified as a generic parameter to a type,
    //it is hit
    case ClassInfoType::Array:
    {
        if (objectId == NULL)
        {
            WriteType(ELEMENT_TYPE_SZARRAY);
            WriteType(ELEMENT_TYPE_END);

            bytesRead += sizeof(void*);

            return S_OK;
        }

        IfFailGo(TraceSZArrayInternal((CArrayInfo*)info, objectId, bytesRead)); //bytesRead will be increased

        break;
    }
    default:
        hr = E_FAIL;
        break;
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::TracePtrType(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    return hr;
}

HRESULT CValueTracer::TraceFnPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    return hr;
}

HRESULT CValueTracer::TraceClassOrStruct(CClassInfo* pClassInfo, ObjectID objectId, CorElementType elementType, ULONG& bytesRead)
{
    HRESULT hr = S_OK;
    ULONG nameLength;

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

        IfFailGo(TraceValue(
            fieldAddress,
            field->m_pType->m_Type,
            GetTypeToken(field->m_pType),
            pClassInfo->m_ModuleID,
            GetGenericIndex(field->m_pType),
            pClassInfo,
            bytesRead
        ));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::GetClassInfo(ClassID classId, IClassInfo** ppClassInfo)
{
    HRESULT hr = S_OK;
    IClassInfo* pClassInfo = nullptr;

    CLock classLock(&g_pProfiler->m_ClassMutex, true);

    auto match = g_pProfiler->m_ClassInfoMap.find(classId);

    if (match == g_pProfiler->m_ClassInfoMap.end())
    {
        //Array types don't seem to hit ClassLoadFinished, so if we got an unknown type it's probably because it's an array

        IfFailGo(CCorProfilerCallback::g_pProfiler->GetClassInfo(classId, &pClassInfo));

        CCorProfilerCallback::g_pProfiler->m_ClassInfoMap[classId] = pClassInfo;
    }
    else
        pClassInfo = match->second;

ErrExit:
    if (SUCCEEDED(hr))
        *ppClassInfo = pClassInfo;

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

long CValueTracer::GetGenericIndex(CSigType* pType)
{
    long index = -1;

    switch (pType->m_Type)
    {
    case ELEMENT_TYPE_MVAR:
        index = ((CSigMethodGenericArgType*)pType)->m_Index;
        break;
    case ELEMENT_TYPE_VAR:
        index = ((CSigTypeGenericArgType*)pType)->m_Index;
        break;
    default:
        break;
    }

    return index;
}