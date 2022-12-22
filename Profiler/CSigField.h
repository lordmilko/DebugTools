#pragma once

#include "CSigType.h"

class CSigField
{
public:
    CSigField(LPWSTR szName, CSigType* pType)
    {
        m_szName = _wcsdup(szName);
        m_pType = pType;
    }

    ~CSigField()
    {
        if (m_szName)
            free(m_szName);

        if (m_pType)
            delete m_pType;
    }

    LPWSTR m_szName;
    CSigType* m_pType;
};