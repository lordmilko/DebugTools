#include "pch.h"
#include "CSigReader.h"
#include "CSigType.h"

CSigType* CSigType::Sentinel = new CSigType(ELEMENT_TYPE_SENTINEL, FALSE);

HRESULT CSigType::New(
    _In_ CSigReader& reader,
    _Out_ CSigType** ppType)
{
    HRESULT hr = S_OK;

    CSigType* pType = nullptr;

    CorElementType elementType = CorSigUncompressElementType(reader.m_pSigBlob);

    while (true)
    {
        if (elementType == ELEMENT_TYPE_CMOD_OPT || elementType == ELEMENT_TYPE_CMOD_REQD)
        {
            //Read the modifier
            CorSigUncompressToken(reader.m_pSigBlob);

            //Read the real type (or next modifier) which follows
            elementType = CorSigUncompressElementType(reader.m_pSigBlob);
        }
        else
            break;
    }

    BOOL isByRef = FALSE;

    if (elementType == ELEMENT_TYPE_BYREF)
    {
        isByRef = TRUE;
        elementType = CorSigUncompressElementType(reader.m_pSigBlob);
    }

    switch (elementType)
    {
#pragma region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

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
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
        pType = new CSigType(elementType, isByRef);
        break;

#pragma endregion
#pragma region ARRAY Type ArrayShape (general array, see section II.23.2.13)

    case ELEMENT_TYPE_ARRAY:
        pType = new CSigArrayType(elementType, isByRef);
        break;

#pragma endregion
#pragma region CLASS TypeDefOrRefOrSpecEncoded | VALUETYPE TypeDefOrRefOrSpecEncoded

    case ELEMENT_TYPE_CLASS:
        pType = new CSigClassType(elementType, isByRef);
        break;

    case ELEMENT_TYPE_VALUETYPE:
        pType = new CSigValueType(elementType, isByRef);
        break;

#pragma endregion
#pragma region FNPTR MethodDefSig | FNPTR MethodRefSig

    case ELEMENT_TYPE_FNPTR:
        pType = new CSigFnPtrType(elementType, isByRef);
        break;

#pragma endregion
#pragma region GENERICINST(CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type*

    case ELEMENT_TYPE_GENERICINST:
        pType = new CSigGenericType(elementType, isByRef);
        break;

#pragma endregion
#pragma region MVAR number | VAR number

    case ELEMENT_TYPE_MVAR:
        pType = new CSigMethodGenericArgType(elementType, isByRef);
        break;

    case ELEMENT_TYPE_VAR:
        pType = new CSigTypeGenericArgType(elementType, isByRef);
        break;

#pragma endregion
#pragma region OBJECT | STRING

    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_STRING:
        pType = new CSigType(elementType, isByRef);
        break;

#pragma endregion
#pragma region PTR CustomMod* Type | PTR CustomMod* VOID

    case ELEMENT_TYPE_PTR:
        pType = new CSigPtrType(elementType, isByRef);
        break;

#pragma endregion
#pragma region SZARRAY CustomMod* Type (single dimensional, zero-based array i.e., vector)

    case ELEMENT_TYPE_SZARRAY:
        pType = new CSigSZArrayType(elementType, isByRef);
        break;

#pragma endregion

    case ELEMENT_TYPE_VOID:
    case ELEMENT_TYPE_TYPEDBYREF:
        pType = new CSigType(elementType, isByRef);
        break;

    case ELEMENT_TYPE_SENTINEL:
        pType = Sentinel;
        goto ErrExit;

    default:
        dprintf(L"Failed to parse element type %d\n", elementType);
        hr = E_FAIL;
        goto ErrExit;
    }

    IfFailGo(pType->Initialize(reader));

ErrExit:
    if (SUCCEEDED(hr))
    {
        *ppType = pType;
    }
    else
    {
        if (pType)
            pType->Release();
    }

    return hr;
}

HRESULT CSigType::GetName(
    _In_ mdToken token,
    _In_ IMetaDataImport2* pMDI,
    _Out_ LPWSTR* szName)
{
    HRESULT hr = S_OK;

    ULONG cchName = 0;
    ULONG pchName;

    CorTokenType type = static_cast<CorTokenType>(TypeFromToken(token));

    switch (type)
    {
    case mdtTypeDef:
        IfFailGo(pMDI->GetTypeDefProps(
            token,
            NULL,
            cchName,
            &pchName,
            NULL,
            NULL
        ));

        cchName = pchName;
        *szName = static_cast<LPWSTR>(malloc(cchName * sizeof(WCHAR)));

        IfFailGo(pMDI->GetTypeDefProps(
            token,
            *szName,
            cchName,
            &pchName,
            NULL,
            NULL
        ));
        break;

    case mdtTypeRef:
        IfFailGo(pMDI->GetTypeRefProps(
            token,
            NULL,
            NULL,
            cchName,
            &pchName
        ));

        cchName = pchName;
        *szName = static_cast<LPWSTR>(malloc(cchName * sizeof(WCHAR)));

        IfFailGo(pMDI->GetTypeRefProps(
            token,
            NULL,
            *szName,
            cchName,
            &pchName
        ));

        break;

    default:
        hr = E_FAIL;
        break;
    }

ErrExit:
    if (FAILED(hr))
    {
        if (szName)
            free(szName);

        szName = nullptr;
    }

    return hr;
}

HRESULT CSigType::GetGenericArgName(
    _In_ ULONG index,
    _In_ mdToken token,
    _In_ CSigReader& reader,
    _Out_ LPWSTR* szName)
{
    return reader.WithGenericParams(token, [index, szName, reader](mdGenericParam token, ULONG ulParamSeq, ULONG cchName, ULONG pchName, BOOL& found) -> BOOL
        {
            HRESULT hr = S_OK;

            if (ulParamSeq == index)
            {
                cchName = pchName;
                *szName = static_cast<LPWSTR>(malloc(cchName * sizeof(WCHAR)));

                HRESULT hr = reader.m_pMDI->GetGenericParamProps(
                    token,
                    &ulParamSeq,
                    NULL,
                    NULL,
                    NULL,
                    *szName,
                    cchName,
                    &pchName
                );

                if (SUCCEEDED(hr))
                    found = TRUE;
            }

            return hr;
        }, TRUE);
}

HRESULT CSigType::Initialize(CSigReader& reader)
{
    return S_OK;
}

HRESULT CSigArrayType::Initialize(CSigReader& reader)
{
    HRESULT hr = S_OK;

    IfFailGo(CSigType::New(reader, &m_pElementType));

    m_Rank = CorSigUncompressData(reader.m_pSigBlob);
    m_NumSizes = CorSigUncompressData(reader.m_pSigBlob);

    if (m_NumSizes)
    {
        m_Sizes = new ULONG[m_NumSizes];

        for (ULONG i = 0; i < m_NumSizes; i++)
            m_Sizes[i] = CorSigUncompressData(reader.m_pSigBlob);
    }

    m_NumLowerBounds = CorSigUncompressData(reader.m_pSigBlob);

    if (m_NumLowerBounds)
    {
        m_LowerBounds = new int[m_NumLowerBounds];

        for (ULONG i = 0; i < m_NumLowerBounds; i++)
        {
            ULONG bytesRead = CorSigUncompressSignedInt(reader.m_pSigBlob, &m_LowerBounds[i]);

            if (bytesRead > 0)
                reader.m_pSigBlob += bytesRead;
        }
    }

ErrExit:
    return hr;
}

HRESULT CSigClassType::Initialize(CSigReader& reader)
{
    HRESULT hr = S_OK;

    m_Token = CorSigUncompressToken(reader.m_pSigBlob);

    //If we ever add anything after this, its OK because the destructor will free m_szName
    IfFailGo(GetName(m_Token, reader.m_pMDI, &m_szName));

ErrExit:
    return hr;
}

HRESULT CSigValueType::Initialize(CSigReader& reader)
{
    HRESULT hr = S_OK;

    m_Token = CorSigUncompressToken(reader.m_pSigBlob);

    //If we ever add anything after this, its OK because the destructor will free m_szName
    IfFailGo(GetName(m_Token, reader.m_pMDI, &m_szName));

ErrExit:
    return hr;
}

CSigFnPtrType::~CSigFnPtrType()
{
    if (m_pMethod)
        m_pMethod->Release();
}

HRESULT CSigFnPtrType::Initialize(CSigReader& reader)
{
    return reader.ParseMethod((LPWSTR)L"delegate*", false, &m_pMethod);
}

HRESULT CSigGenericType::Initialize(CSigReader& reader)
{
    HRESULT hr = S_OK;

    CSigType** genericArgs = nullptr;
    ULONG i = 0;

    m_GenericInstType = CorSigUncompressElementType(reader.m_pSigBlob);

    m_GenericTypeDefinitionToken = CorSigUncompressToken(reader.m_pSigBlob);

    IfFailGo(GetName(m_GenericTypeDefinitionToken, reader.m_pMDI, &m_szGenericTypeDefinitionName));

    m_NumGenericArgs = CorSigUncompressData(reader.m_pSigBlob);

    genericArgs = new CSigType*[m_NumGenericArgs];

    for (; i < m_NumGenericArgs; i++)
    {
        CSigType* ptr = nullptr;

        IfFailGo(CSigType::New(reader, &ptr));

        genericArgs[i] = ptr;
    }

ErrExit:
    if (SUCCEEDED(hr))
    {
        m_GenericArgs = genericArgs;
    }
    else
    {
        for (ULONG j = 0; j < i; j++)
            genericArgs[j]->Release();

        delete[] genericArgs;
    }

    return hr;
}

HRESULT CSigMethodGenericArgType::Initialize(CSigReader& reader)
{
    HRESULT hr = S_OK;

    m_Index = CorSigUncompressData(reader.m_pSigBlob);

    CorTokenType type = (CorTokenType)TypeFromToken(reader.m_Token);

    if (type == mdtMethodDef)
    {
        IfFailGo(GetGenericArgName(m_Index, reader.m_Token, reader, &m_szName));
    }

ErrExit:
    return hr;
}

HRESULT CSigTypeGenericArgType::Initialize(CSigReader& reader)
{
    HRESULT hr = S_OK;

    m_Index = CorSigUncompressData(reader.m_pSigBlob);

    CorTokenType type = (CorTokenType)TypeFromToken(reader.m_Token);

    if (type == mdtMethodDef)
    {
        mdTypeDef pClass;

        IfFailGo(reader.m_pMDI->GetMethodProps(
            reader.m_Token,
            &pClass,
            NULL,
            0,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL
        ));

        IfFailGo(GetGenericArgName(m_Index, pClass, reader, &m_szName));
    }
    else if (type == mdtFieldDef)
    {
        mdTypeDef pClass;

        IfFailGo(reader.m_pMDI->GetFieldProps(
            reader.m_Token,
            &pClass,
            NULL,
            0,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            NULL
        ));

        IfFailGo(GetGenericArgName(m_Index, pClass, reader, &m_szName));
    }

ErrExit:
    return hr;
}

HRESULT CSigPtrType::Initialize(CSigReader& reader)
{
    return New(reader, &m_pPtrType);
}

HRESULT CSigSZArrayType::Initialize(CSigReader& reader)
{
    return New(reader, &m_pElementType);
}