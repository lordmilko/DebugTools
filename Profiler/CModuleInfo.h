#pragma once

#include <mutex>
#include <unordered_map>

class CModuleIDAndTypeDef
{
public:
    CModuleIDAndTypeDef(ModuleID moduleId, mdTypeDef typeDef, BOOL failed)
    {
        m_ModuleID = moduleId;
        m_TypeDef = typeDef;
        m_Failed = failed;

    }

    ModuleID m_ModuleID;
    mdTypeDef m_TypeDef;
    BOOL m_Failed;
};

class CModuleInfo
{
public:
    CModuleInfo(AssemblyID assemblyID, ModuleID moduleID, IMetaDataImport2* pMDI)
    {
        if (pMDI)
            pMDI->AddRef();

        m_AssemblyID = assemblyID;
        m_ModuleID = moduleID;
        m_pMDI = pMDI;
    }

    ~CModuleInfo()
    {
        for (auto const& kv : m_AsmRefMap)
            delete kv.second;

        if (m_pMDI)
            m_pMDI->Release();
    }

    AssemblyID m_AssemblyID;
    ModuleID m_ModuleID;
    IMetaDataImport2* m_pMDI;

    std::shared_mutex m_AsmRefMutex;
    std::unordered_map<mdAssemblyRef, CModuleIDAndTypeDef*> m_AsmRefMap;
};