#pragma once

#include "CUnknown.h"
#include <shared_mutex>
#include <unordered_map>

class CModuleIDAndTypeDef : public CUnknown
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

class CModuleInfo : public CUnknown
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

    ~CModuleInfo();

    AssemblyID m_AssemblyID;
    ModuleID m_ModuleID;
    IMetaDataImport2* m_pMDI;

    std::shared_mutex m_TypeRefMutex;
    std::unordered_map<mdTypeRef, CModuleIDAndTypeDef*> m_TypeRefMap;
};