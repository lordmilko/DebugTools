#pragma once

#include "CSigType.h"

class ISigParameter
{
protected:
    ISigParameter(CSigType* pType)
    {
        m_pType = pType;
    }

    CSigType* m_pType;
};

class SigParameter : public ISigParameter
{
public:
    SigParameter(CSigType* pType) : ISigParameter(pType)
    {
    }
};

class SigFnPtrParameter : public ISigParameter
{
public:
    SigFnPtrParameter(CSigType* pType) : ISigParameter(pType)
    {
    }
};