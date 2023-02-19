#pragma once

#include "CUnknown.h"
#include "CSigType.h"

class CSigField : public CUnknown
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
            m_pType->Release();
    }

    LPWSTR m_szName;
    CSigType* m_pType;
};