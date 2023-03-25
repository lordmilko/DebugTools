#include "pch.h"
#include "CAssemblyInfo.h"
#include "CModuleInfo.h"

CAssemblyInfo::~CAssemblyInfo()
{
    if (m_pAssemblyName)
        delete m_pAssemblyName;

    if (m_pMDAI)
        m_pMDAI->Release();

    //Don't need to free the modules themselves, we just hold a reference to them
    if (m_Modules)
    {
        for (ULONG i = 0; i < m_NumModules; i++)
            m_Modules[i]->Release();

        delete[] m_Modules;
    }
}

void CAssemblyInfo::AddModule(CModuleInfo* pModuleInfo)
{
    if (m_Modules == nullptr)
    {
        m_Modules = new CModuleInfo*[1];
        pModuleInfo->AddRef();
        m_Modules[0] = pModuleInfo;
        m_NumModules++;
    }
    else
    {
        for (ULONG i = 0; i < m_NumModules; i++)
        {
            if (m_Modules[i] == pModuleInfo)
                return;
        }

        CModuleInfo** newModules = new CModuleInfo*[m_NumModules + 1];

        for (ULONG i = 0; i < m_NumModules; i++)
        {
            newModules[i] = m_Modules[i];
        }

        pModuleInfo->AddRef();
        newModules[m_NumModules] = pModuleInfo;

        m_NumModules++;
    }
}

void CAssemblyInfo::RemoveModule(CModuleInfo* pModuleInfo)
{
    if (m_Modules == nullptr)
        return;

    BOOL found = FALSE;

    for (ULONG i = 0; i < m_NumModules; i++)
    {
        if (m_Modules[i] == pModuleInfo)
        {
            pModuleInfo->Release();
            found = TRUE;
            break;
        }
    }

    if (found)
    {
        if (m_NumModules)
        {
            delete[] m_Modules;
            m_Modules = nullptr;
        }
        else
        {
            CModuleInfo** newModules = new CModuleInfo*[m_NumModules - 1];

            ULONG j = 0;

            for (ULONG i = 0; i < m_NumModules; i++)
            {
                if (m_Modules[i] != pModuleInfo)
                {
                    newModules[j] = m_Modules[i];
                    j++;
                }
            }
        }
    }
}
