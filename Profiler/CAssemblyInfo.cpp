#include "pch.h"
#include "CAssemblyInfo.h"
#include "CModuleInfo.h"

CAssemblyInfo::~CAssemblyInfo()
{
    if (m_szShortName)
        free(m_szShortName);

    if (m_szName)
        free(m_szName);

    if (m_pMDAI)
        m_pMDAI->Release();

    if (m_pbPublicKeyToken)
        free((void*)m_pbPublicKeyToken);

    //Don't need to free the modules themselves, we just hold a reference to them
    if (m_Modules)
        delete m_Modules;
}

void CAssemblyInfo::AddModule(CModuleInfo* pModuleInfo)
{
    if (m_Modules == nullptr)
    {
        m_Modules = new CModuleInfo*[1];
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

        newModules[m_NumModules] = pModuleInfo;

        m_NumModules++;
    }
}

void CAssemblyInfo::RemoveModule(CModuleInfo* pModuleInfo)
{
    if (m_Modules == nullptr)
        return;

    if (m_NumModules == 1)
    {
        delete m_Modules;
    }
    else
    {
        BOOL found = FALSE;

        for (ULONG i = 0; i < m_NumModules; i++)
        {
            if (m_Modules[i] == pModuleInfo)
            {
                found = TRUE;
                break;
            }
        }

        if (found)
        {
            CModuleInfo** newModules = new CModuleInfo * [m_NumModules - 1];

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
