#include "pch.h"
#include "CModuleInfo.h"
#include "CClassInfo.h"

CModuleInfo::~CModuleInfo()
{
    for (auto const& kv : m_TypeRefMap)
        kv.second->Release();

    if (m_pMDI)
        m_pMDI->Release();
}
