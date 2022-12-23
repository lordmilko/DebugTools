#pragma once

#include "CSigType.h"

class ISigParameter
{
public:
    CSigType* m_pType;

protected:
    ISigParameter(CSigType* pType)
    {
        m_pType = pType;
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