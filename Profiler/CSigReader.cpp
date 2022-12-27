#include "pch.h"
#include "CSigReader.h"

HRESULT CSigReader::ParseMethod(
    _In_ LPWSTR szName,
    _In_ BOOL topLevel,
    _Out_ CSigMethod** ppMethod
)
{
    HRESULT hr = S_OK;

    CSigType* retType = nullptr;
    ULONG numParameters = 0;
    ULONG numVarArgParameters = 0;
    ISigParameter** parameters = nullptr;
    ISigParameter** varargParameters = nullptr;

    ULONG numGenericArgNames = 0;
    LPWSTR* genericTypeArgs = nullptr;

    ULONG i = 0;

    CorCallingConvention callingConvention = static_cast<CorCallingConvention>(CorSigUncompressCallingConv(m_pSigBlob));

    if (callingConvention & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        numGenericArgNames = CorSigUncompressData(m_pSigBlob);

        IfFailGo(GetMethodGenericArgNames(numGenericArgNames, &genericTypeArgs));
    }

    numParameters = CorSigUncompressData(m_pSigBlob);

    IfFailGo(CSigType::New(*this, &retType));

    IfFailGo(ParseSigMethodParams(numParameters, topLevel, &parameters, &numVarArgParameters, &varargParameters));

ErrExit:
    if (SUCCEEDED(hr))
    {
        if (((callingConvention & IMAGE_CEE_CS_CALLCONV_VARARG) == IMAGE_CEE_CS_CALLCONV_VARARG) && varargParameters == nullptr)
        {
            ISigParameter** newParameters = new ISigParameter*[numParameters + 1];

            for (ULONG i = 0; i < numParameters; i++)
                newParameters[i] = parameters[i];

            newParameters[numParameters] = new SigArgListParameter();
            delete parameters;
            parameters = newParameters;
            numParameters++;
        }

        if (varargParameters == nullptr)
        {
            *ppMethod = new CSigMethodDef(
                szName,
                callingConvention,
                retType,
                numParameters,
                parameters,
                numGenericArgNames,
                genericTypeArgs
            );
        }
        else
        {
            *ppMethod = new CSigMethodRef(
                szName,
                callingConvention,
                retType,
                numParameters - numVarArgParameters,
                parameters,
                numVarArgParameters,
                varargParameters
            );
        }
    }
    else
    {
        if (parameters)
        {
            for(ULONG j = 0; j < i; j++)
            {
                parameters[i]->Release();
            }

            delete parameters;
        }

        if (retType)
            retType->Release();
    }

    return hr;
}

HRESULT CSigReader::ParseField(
    _In_ LPWSTR szName,
    _Out_ CSigField** ppField)
{
    CorCallingConvention callingConvention = static_cast<CorCallingConvention>(CorSigUncompressCallingConv(m_pSigBlob));

    if (callingConvention != IMAGE_CEE_CS_CALLCONV_FIELD)
        return E_FAIL;

    HRESULT hr = S_OK;

    CSigType* pType = nullptr;
    IfFailGo(CSigType::New(*this, &pType));

ErrExit:
    if (SUCCEEDED(hr))
    {
        *ppField = new CSigField(szName, pType);
    }
    else
    {
        if (pType)
            pType->Release();
    }

    return hr;
}

HRESULT CSigReader::ParseSigMethodParams(
    _In_ ULONG sigParamCount,
    _In_ BOOL topLevel,
    _Out_ ISigParameter*** pppParameters,
    _Out_ ULONG* numVarArgParameters,
    _Out_ ISigParameter*** pppVarArgParameters)
{
    *numVarArgParameters = 0;

    if (sigParamCount == 0)
    {
        pppParameters = nullptr;
        return S_OK;
    }

    HRESULT hr = S_OK;

    ISigParameter** ppParameters = nullptr;
    ISigParameter** ppVarArgParameters = nullptr;
    ULONG i = 0;
    BOOL haveSentinel = FALSE;

    ppParameters = new ISigParameter*[sigParamCount];

    for(; i < sigParamCount; i++)
    {
        CSigType* sigType;
        IfFailGo(CSigType::New(*this, &sigType));

        if (sigType == CSigType::Sentinel)
        {
            ISigParameter** newParameters = new ISigParameter*[i];

            for (ULONG j = 0; j < i; j++)
                newParameters[j] = ppParameters[j];

            delete ppParameters;
            ppParameters = newParameters;

            ppVarArgParameters = new ISigParameter * [sigParamCount - i];
            haveSentinel = TRUE;
        }

        if (haveSentinel)
        {
            ppVarArgParameters[*numVarArgParameters] = new SigVarArgParameter(sigType);
            (*numVarArgParameters)++;
            continue;
        }

        if (topLevel)
        {
            ppParameters[i] = new SigParameter(sigType);
        }
        else
            ppParameters[i] = new SigFnPtrParameter(sigType);
    }

ErrExit:
    if (SUCCEEDED(hr))
    {
        *pppParameters = ppParameters;
        *pppVarArgParameters = ppVarArgParameters;
    }
    else
    {
        for(ULONG j = 0; j < i; j++)
        {
            ppParameters[i]->Release();
        }

        delete ppParameters;

        if (ppVarArgParameters)
        {
            for (ULONG j = 0; j < *numVarArgParameters; j++)
                ppVarArgParameters[j]->Release();

            delete ppVarArgParameters;
        }
    }

    return S_OK;
}

HRESULT CSigReader::GetMethodGenericArgNames(ULONG genericArgsLength, LPWSTR** szNames)
{
    ULONG allocated = 0;

    *szNames = (LPWSTR*)malloc(genericArgsLength * sizeof(LPWSTR));

    LPWSTR* namesPtr = *szNames;

    HRESULT hr = WithGenericParams(m_Token, [&allocated, &namesPtr, this](mdGenericParam token, ULONG ulParamSeq, ULONG cchName, ULONG pchName, BOOL& found) -> BOOL
        {
            HRESULT hr = S_OK;

            cchName = pchName;
            LPWSTR ptr = static_cast<LPWSTR>(malloc(cchName * sizeof(WCHAR)));

            hr = m_pMDI->GetGenericParamProps(
                token,
                &ulParamSeq,
                NULL,
                NULL,
                NULL,
                ptr,
                cchName,
                &pchName
            );

            *namesPtr = ptr;
            namesPtr++;
            allocated++;

            return hr;
        }, FALSE);

    if (hr != S_OK)
    {
        for (ULONG i = 0; i < allocated; i++)
            free((*szNames)[i]);

        free(*szNames);
        *szNames = nullptr;
    }

    return hr;
}

HRESULT CSigReader::WithGenericParams(
    mdToken token,
    const std::function<HRESULT(mdGenericParam, ULONG, ULONG, ULONG, BOOL&)>& callback,
    _In_ BOOL requireValue) const
{
    HRESULT hr = S_OK;

    BOOL found = FALSE;

    HCORENUM hEnum = nullptr;
    ULONG cGenericParams;
    mdGenericParam* genericParams = nullptr;

    ULONG ulParamSeq;
    ULONG cchName = 0;
    ULONG pchName;

    IfFailGo(m_pMDI->EnumGenericParams(
        &hEnum,
        token,
        nullptr,
        0,
        &cGenericParams
    ));

    //Implicitly we MUST have an enumerator, as we know we should have generic parameters based on the fact we're an ELEMENT_TYPE_MVAR
    if (hEnum == nullptr)
        return S_OK;

    IfFailGo(m_pMDI->CountEnum(hEnum, &cGenericParams));

    genericParams = new mdGenericParam[cGenericParams];

    IfFailGo(m_pMDI->EnumGenericParams(
        &hEnum,
        token,
        genericParams,
        cGenericParams,
        &cGenericParams
    ));

    for (ULONG i = 0; i < cGenericParams; i++)
    {
        IfFailGo(m_pMDI->GetGenericParamProps(
            genericParams[i],
            &ulParamSeq,
            NULL,
            NULL,
            NULL,
            NULL,
            cchName,
            &pchName
        ));

        IfFailGo(callback(genericParams[i], ulParamSeq, cchName, pchName, found));

        if (found)
            break;
    }

    if (!found && requireValue)
        hr = E_FAIL;

ErrExit:
    if (genericParams)
        delete[] genericParams;

    if (hEnum)
        m_pMDI->CloseEnum(hEnum);

    return hr;
}