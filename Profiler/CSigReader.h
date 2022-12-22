#pragma once

#include "CSigMethod.h"

class CSigReader
{
public:
    CSigReader(mdToken token, IMetaDataImport2* pMDI, PCCOR_SIGNATURE pSigBlob)
    {
        m_Token = token;
        m_pMDI = pMDI;
        m_pSigBlob = pSigBlob;
    }

    HRESULT ParseMethod(
        LPWSTR name,
        BOOL topLevel,
        _Out_ CSigMethod** ppMethod
    );

    HRESULT ParseSigMethodParams(
        _In_ ULONG sigParamCount,
        _In_ BOOL topLevel,
        _Out_ ISigParameter*** pppParameters,
        _Out_ ULONG* numVarArgParameters,
        _Out_ ISigParameter*** pppVarArgParameters);

    HRESULT GetMethodGenericArgNames(ULONG genericArgsLength, LPWSTR** names);

    HRESULT WithGenericParams(
        mdToken token,
        const std::function<HRESULT(mdGenericParam, ULONG, ULONG, ULONG, BOOL&)>& callback,
        _In_ BOOL requireValue) const;

    PCCOR_SIGNATURE m_pSigBlob;
    IMetaDataImport2* m_pMDI;
    mdToken m_Token;
};