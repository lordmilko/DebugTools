#pragma once

#include "CSigType.h"

class ISigParameter : public CUnknown
{
public:
    CSigType* m_pType;

protected:
    ISigParameter(CSigType* pType)
    {
        m_pType = pType;
    }

    virtual ~ISigParameter()
    {
        if (m_pType)
            m_pType->Release();
    }
};

class SigParameter : public ISigParameter
{
public:
    SigParameter(CSigType* pType) : ISigParameter(pType)
    {
    }
};

class SigArgListParameter : public ISigParameter
{
public:
    SigArgListParameter() : ISigParameter(nullptr)
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

class SigVarArgParameter : public ISigParameter
{
public:
    SigVarArgParameter(CSigType* pType) : ISigParameter(pType)
    {
    }
};