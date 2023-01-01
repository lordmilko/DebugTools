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
    IClassInfo* pMethodClassInfo;

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
                IfFailGo(GetClassInfoFromClassId(typeArgs[i], &info));

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

        //Get the class info of the class the method being invoked is defined in
        IfFailGo(GetClassInfoFromClassId(classId, &pMethodClassInfo));

        IfFailGo(TraceParameters(argumentInfo, pMethod, pMethodClassInfo));
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
    _In_ IClassInfo* pMethodClassInfo)
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

        IfFailGo(TraceParameter(&range, pParameter, pMethodClassInfo, pMethod->m_ModuleID));
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

    CorElementType type = pType->m_Type;;
    long genericIndex = -1;

    GetGenericInfo(pType, &type, &genericIndex);

    TraceValueContext ctx = MakeTraceValueContext(typeToken, typeTokenModule, genericIndex, pClassInfo, (CSigGenericType*)pType);

    IfFailGo(TraceValue(
        pAddress,
        type,
        &ctx,
        bytesRead
    ));

ErrExit:
    return hr;
}

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
    //Unsafe.IsNullRef can pass an address of 0
    if (startAddress == 0)
        return E_FAIL;

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
    case ELEMENT_TYPE_SZARRAY:
        return TraceArray(startAddress, elementType, bytesRead);

    case ELEMENT_TYPE_GENERICINST:
        return TraceGenericType(startAddress, pContext, bytesRead);

    #pragma endregion

    case ELEMENT_TYPE_VALUETYPE:
        return TraceValueType(startAddress, pContext, bytesRead);

    case ELEMENT_TYPE_VAR:
        return TraceTypeGenericType(startAddress, pContext, bytesRead);

    case ELEMENT_TYPE_MVAR:
        return TraceMethodGenericType(startAddress, pContext, bytesRead);

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

    __int64* pPtr = (__int64*)startAddress;

    Write(pPtr, ELEMENT_TYPE_I, 8);
    bytesRead += sizeof(INT_PTR);

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceUIntPtr(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    __int64* pPtr = (__int64*)startAddress;

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
    ULONG innerBytesRead = 0;

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

    IfFailGo(GetClassInfoFromClassId(classId, &info));

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

HRESULT CValueTracer::TraceArray(_In_ UINT_PTR startAddress, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

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

        IfFailGo(GetClassId(
            pContext->GenericArg.ClassInfo,
            pContext->GenericInst.GenericType,
            pContext->ValueType.ModuleOfTypeToken,
            pContext->ValueType.TypeToken,
            pContext->GenericInst.GenericType,
            -1,
            &classId
        ));

        IfFailGo(GetClassInfoFromClassId(classId, (IClassInfo**)&pClassInfo));

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

    if (objectId == NULL)
    {
        WriteType(type);
        WriteType(ELEMENT_TYPE_END);

        bytesRead += sizeof(void*);

        return S_OK;
    }

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromObject(objectId, &classId));
    IfFailGo(GetClassInfoFromClassId(classId, (IClassInfo**)&pArrayInfo));

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

    ULONG32* dimensionSizes = new ULONG32[pArrayInfo->m_Rank];
    int* dimensionLowerBounds = new int[pArrayInfo->m_Rank];
    BYTE* pData;

    ULONG arrayLength;
    ULONG arrBytesRead = 0;

    IfFailGo(g_pProfiler->m_pInfo->GetArrayObjectInfo(objectId, pArrayInfo->m_Rank, dimensionSizes, dimensionLowerBounds, &pData));

    if (pArrayInfo->m_Rank == 1)
        WriteType(ELEMENT_TYPE_SZARRAY);
    else
    {
        WriteType(ELEMENT_TYPE_ARRAY);
        WriteValue(&pArrayInfo->m_Rank, 4);
    }

    WriteType(pArrayInfo->m_CorElementType);

    for(ULONG i = 0; i < pArrayInfo->m_Rank; i++)
    {
        arrayLength = dimensionSizes[i];

        WriteValue(&arrayLength, 4);

        if (pArrayInfo->m_pElementType->m_InfoType == ClassInfoType::StandardType)
        {
            CStandardTypeInfo* pStandardTypeInfo = (CStandardTypeInfo*)pArrayInfo->m_pElementType;

            for (ULONG i = 0; i < arrayLength; i++)
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

            for (ULONG i = 0; i < arrayLength; i++)
            {
                if (pArrayInfo->m_CorElementType == ELEMENT_TYPE_VALUETYPE)
                {
                    IfFailGo(TraceClassOrStruct(
                        pElementType,
                        (UINT_PTR)(pData + arrBytesRead),
                        pArrayInfo->m_CorElementType,
                        arrBytesRead
                    ));
                }
                else
                {
                    TraceValueContext ctx = MakeTraceValueContext(pElementType->m_TypeDef, pElementType->m_ModuleID, -1, pElementType, nullptr);

                    IfFailGo(TraceValue(
                        (UINT_PTR)(pData + arrBytesRead),
                        pArrayInfo->m_CorElementType,
                        &ctx,
                        arrBytesRead
                    ));
                }
            }
        }
    }

    bytesRead += sizeof(void*);

ErrExit:
    if (dimensionSizes)
        delete dimensionSizes;

    if (dimensionLowerBounds)
        delete dimensionLowerBounds;

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

    IfFailGo(GetModuleInfo(moduleId, &pModuleInfo));

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

HRESULT CValueTracer::GetModuleInfo(ModuleID moduleId, CModuleInfo** ppModuleInfo)
{
    HRESULT hr = S_OK;

    auto match = g_pProfiler->m_ModuleInfoMap.find(moduleId);

    if (match == g_pProfiler->m_ModuleInfoMap.end())
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    *ppModuleInfo = match->second;

ErrExit:
    return hr;
}

HRESULT CValueTracer::GetClassId(
    _In_ CClassInfo* pClassInfo, //The current class that any ELEMENT_TYPE_VAR references will be resolved from
    _In_ CSigType* pType,
    _In_ ModuleID moduleOfTypeToken,
    _In_ mdToken typeToken,
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
                typeToken,
                pGenericType,
                i,
                &childClassId
            ));

            typeArgIds[i] = childClassId;
        }

        ModuleID outerModuleId;
        mdTypeDef outerTypeDef;

        IfFailGo(GetTypeDefAndModule(moduleOfTypeToken, typeToken, &outerModuleId, &outerTypeDef));

        CModuleInfo* moduleInfo = g_pProfiler->m_ModuleInfoMap[outerModuleId];

        moduleInfo->m_pMDI->GetTypeDefProps(outerTypeDef, typeName, NAME_BUFFER_SIZE, NULL, NULL, NULL);

        IfFailGo(g_pProfiler->m_pInfo->GetClassFromTokenAndTypeArgs(
            outerModuleId,
            outerTypeDef,
            pGenericType->m_NumGenericArgs,
            typeArgIds,
            classId
        ));
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
            IClassInfo* info = m_GenericTypeArgs[((CSigMethodGenericArgType*)pType)->m_Index];

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

        case ELEMENT_TYPE_OBJECT:
            hr = PROFILER_E_GENERICCLASSID;
            break;
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
        delete typeArgIds;

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
    mdToken elmToken = GetTypeToken(arr->m_pElementType);

    IfFailGo(GetClassId(
        pClassInfo,
        arr->m_pElementType,
        moduleOfTypeToken,
        elmToken,
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
                IfFailGo(GetClassInfoFromClassId(info->m_GenericTypeArgs[curGenericArg], &genericTypeArg, false));

                if (genericTypeArg->m_InfoType == ClassInfoType::Array)
                {
                    CArrayInfo* curArr = (CArrayInfo*)genericTypeArg;

                    IClassInfo* temp;

                    GetClassInfoFromClassId(elmClassId, &temp, false);

                    if (curArr->m_pElementType->m_ClassID == elmClassId && IsArrayMatch(arr, curArr))
                    {
                        *classId = curArr->m_ClassID;
                        dprintf(L"matched array with classId %llX\n", curArr->m_ClassID);
                        return S_OK;
                    }
                }
            }
        }
    }

    hr = E_FAIL;

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

    IfFailGo(GetClassInfoFromClassId(classId, &pClassInfo));

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

    IClassInfo* genericInfo;

    ClassID classId = pContext->GenericArg.ClassInfo->m_GenericTypeArgs[pContext->GenericArg.GenericIndex];

    IfFailGo(GetClassInfoFromClassId(classId, &genericInfo));

    IfFailGo(TraceGenericTypeInternal(startAddress, genericInfo, bytesRead));

ErrExit:
    return hr;
}

HRESULT CValueTracer::TraceMethodGenericType(_In_ UINT_PTR startAddress, _In_ TraceValueContext* pContext, _Out_opt_ ULONG& bytesRead)
{
    HRESULT hr = S_OK;

    IClassInfo* info = m_GenericTypeArgs[pContext->GenericArg.GenericIndex];

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
        CArrayInfo* arr = (CArrayInfo*)info;

        if (objectId == NULL)
        {
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
    long genericIndex = -1;

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

        CorElementType type = field->m_pType->m_Type;
        UINT_PTR fieldAddress = objectId + offset.ulOffset;

        GetGenericInfo(field->m_pType, &type, &genericIndex);

        TraceValueContext ctx = MakeTraceValueContext(
            GetTypeToken(field->m_pType),
            pClassInfo->m_ModuleID,
            genericIndex,
            pClassInfo,
            (CSigGenericType*)field->m_pType
        );

        IfFailGo(TraceValue(
            fieldAddress,
            field->m_pType->m_Type,
            &ctx,
            bytesRead
        ));
    }

ErrExit:
    return hr;
}

HRESULT CValueTracer::GetClassInfoFromClassId(ClassID classId, IClassInfo** ppClassInfo, bool lock)
{
    HRESULT hr = S_OK;
    IClassInfo* pClassInfo = nullptr;

    CLock* classLock = nullptr;

    if (lock)
        classLock = new CLock(&g_pProfiler->m_ClassMutex, true);

    auto match = g_pProfiler->m_ClassInfoMap.find(classId);

    if (match == g_pProfiler->m_ClassInfoMap.end())
    {
        //Array types don't seem to hit ClassLoadFinished, so if we got an unknown type it's probably because it's an array

        IfFailGo(CCorProfilerCallback::g_pProfiler->GetClassInfo(classId, &pClassInfo));

        //We already hold the class lock (see above)
        g_pProfiler->AddClassNoLock(pClassInfo);
    }
    else
        pClassInfo = match->second;

ErrExit:
    if (classLock)
        delete classLock;

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
    CorElementType* type,
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