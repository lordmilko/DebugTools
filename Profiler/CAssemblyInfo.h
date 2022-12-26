#pragma once

class CModuleInfo;

class CAssemblyInfo
{
public:
    CAssemblyInfo(
        LPWSTR szShortName,
        LPWSTR szName,
        const BYTE* pbPublicKey,
        ULONG cbPublicKey,
        const BYTE* pbPublicKeyToken,
        IMetaDataAssemblyImport* pMDAI)
    {
        m_szShortName = _wcsdup(szShortName);
        m_szName = _wcsdup(szName);
        m_pbPublicKey = pbPublicKey;
        m_cbPublicKey = cbPublicKey;
        m_pbPublicKeyToken = pbPublicKeyToken;

        if (pMDAI)
            pMDAI->AddRef();

        m_NumModules = 0;
        m_Modules = nullptr;

        m_pMDAI = pMDAI;
    }

    ~CAssemblyInfo();

    void AddModule(CModuleInfo* pModuleInfo);
    void RemoveModule(CModuleInfo* pModuleInfo);

    LPWSTR m_szShortName;
    LPWSTR m_szName;
    const BYTE* m_pbPublicKey;
    ULONG m_cbPublicKey;
    const BYTE* m_pbPublicKeyToken;

    ULONG m_NumModules;
    CModuleInfo** m_Modules;

    IMetaDataAssemblyImport* m_pMDAI;
};