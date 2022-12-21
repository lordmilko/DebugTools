#pragma once

#include "CSigMethod.h"

class CSigReader
{
public:
    CSigReader(mdMethodDef methodDef, IMetaDataImport2* pMDI, PCCOR_SIGNATURE pSigBlob)
    {
        m_MethodDef = methodDef;
        m_pMDI = pMDI;
        m_pSigBlob = pSigBlob;
    }

    HRESULT ParseSigMethodDefOrRef(
        BOOL topLevel,
        _Out_ CSigMethod** ppMethod
    );

    HRESULT ParseSigMethodParams(
        _In_ ULONG sigParamCount,
        _In_ BOOL topLevel,
        _In_ CSigReader& reader,
        _Out_ ISigParameter*** pppParameters);

    HRESULT ParseParamNames(CSigMethod* pMethod, CSigReader& reader);
    HRESULT GetMethodGenericArgNames(ULONG genericArgsLength, LPWSTR** names);

    HRESULT WithGenericParams(
        const std::function<HRESULT(mdGenericParam, ULONG, ULONG, ULONG, BOOL&)>& callback,
        _In_ BOOL requireValue) const;

    PCCOR_SIGNATURE m_pSigBlob;
    IMetaDataImport2* m_pMDI;
    mdMethodDef m_MethodDef;
};