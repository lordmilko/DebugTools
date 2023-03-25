#pragma once

#include "CUnknown.h"
#include "CAssemblyName.h"

class CModuleInfo;

class CAssemblyInfo : public CUnknown
{
public:
    CAssemblyInfo(
        CAssemblyName* pAssemblyName,
        const BYTE* pbPublicKey,
        ULONG cbPublicKey,
        IMetaDataAssemblyImport* pMDAI)
    {
        m_pAssemblyName = pAssemblyName;
        m_pbPublicKey = pbPublicKey;
        m_cbPublicKey = cbPublicKey;

        if (pMDAI)
            pMDAI->AddRef();

        m_NumModules = 0;
        m_Modules = nullptr;

        m_pMDAI = pMDAI;
    }

    ~CAssemblyInfo();

    void AddModule(CModuleInfo* pModuleInfo);
    void RemoveModule(CModuleInfo* pModuleInfo);

    CAssemblyName* m_pAssemblyName;
    const BYTE* m_pbPublicKey;
    ULONG m_cbPublicKey;

    ULONG m_NumModules;
    CModuleInfo** m_Modules;

    IMetaDataAssemblyImport* m_pMDAI;
};