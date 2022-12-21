#include "pch.h"
#include "CSigReader.h"

HRESULT CSigReader::ParseSigMethodDefOrRef(
    BOOL topLevel,
    _Out_ CSigMethod** ppMethod
)
{
    HRESULT hr = S_OK;

    CSigType* retType = nullptr;
    ULONG numParameters = 0;
    ISigParameter** parameters = nullptr;

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

    IfFailGo(ParseSigMethodParams(numParameters, topLevel, *this, &parameters));

ErrExit:
    if (SUCCEEDED(hr))
    {
        *ppMethod = new CSigMethodDef(
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
        if (parameters)
        {
            for(ULONG j = 0; j < i; j++)
            {
                delete parameters[i];
            }

            delete parameters;
        }
    }

    return hr;
}

HRESULT CSigReader::ParseSigMethodParams(
    _In_ ULONG sigParamCount,
    _In_ BOOL topLevel,
    _In_ CSigReader& reader,
    _Out_ ISigParameter*** pppParameters)
{
    if (sigParamCount == 0)
    {
        pppParameters = nullptr;
        return S_OK;
    }

    HRESULT hr = S_OK;

    ISigParameter** ppParameters = nullptr;
    ULONG i = 0;

    ppParameters = new ISigParameter*[sigParamCount];

    for(; i < sigParamCount; i++)
    {
        CSigType* sigType;
        IfFailGo(CSigType::New(reader, &sigType));

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
    }
    else
    {
        for(ULONG j = 0; j < i; j++)
        {
            delete ppParameters[i];
        }

        delete ppParameters;
    }

    return S_OK;
}

HRESULT CSigReader::GetMethodGenericArgNames(ULONG genericArgsLength, LPWSTR** names)
{
    ULONG allocated = 0;

    *names = (LPWSTR*)malloc(genericArgsLength * sizeof(LPWSTR));

    LPWSTR* namesPtr = *names;

    HRESULT hr = WithGenericParams([&allocated, &namesPtr, this](mdGenericParam token, ULONG ulParamSeq, ULONG cchName, ULONG pchName, BOOL& found) -> BOOL
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
            free((*names)[i]);

        free(*names);
        *names = nullptr;
    }

    return hr;
}

HRESULT CSigReader::WithGenericParams(
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
        m_MethodDef,
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
        m_MethodDef,
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