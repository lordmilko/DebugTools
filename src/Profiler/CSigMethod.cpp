#include "pch.h"
#include "CSigMethod.h"
#include "CClassInfo.h"

CSigMethodDef::~CSigMethodDef()
{
    if (m_GenericTypeArgNames)
    {
        for (ULONG i = 0; i < m_NumGenericTypeArgNames; i++)
            free(m_GenericTypeArgNames[i]);

        free(m_GenericTypeArgNames);
    }

    if (m_CanonicalType)
        m_CanonicalType->Release();
}