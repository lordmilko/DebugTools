#pragma once

#include "CSigType.h"
#include "CUnknown.h"
#include "ISigParameter.h"

class IClassInfo;

class CSigMethod : public CUnknown
{
public:
    CSigMethod(
        LPWSTR szName,
        CorCallingConvention callingConvention,
        CSigType* retType,
        ULONG numParameters,
        ISigParameter** parameters)
    {
        m_szName = _wcsdup(szName);

        m_CallingConv = callingConvention;
        m_pRetType = retType;
        m_NumParameters = numParameters;
        m_Parameters = parameters;
    }

    ~CSigMethod()
    {
        if (m_szName)
            free(m_szName);

        if (m_pRetType)
            m_pRetType->Release();

        if (m_Parameters)
        {
            for (ULONG i = 0; i < m_NumParameters; i++)
                m_Parameters[i]->Release();

            delete m_Parameters;
        }
    }

    LPWSTR m_szName;
    CorCallingConvention m_CallingConv;
    CSigType* m_pRetType;

    ULONG m_NumParameters;
    ISigParameter** m_Parameters;
};

class CSigMethodDef : public CSigMethod
{
public:
    CSigMethodDef(
        LPWSTR szName,
        CorCallingConvention callingConvention,
        CSigType* retType,
        ULONG numParameters,
        ISigParameter** parameters,
        ULONG numGenericArgNames,
        LPWSTR* genericTypeArgNames) : CSigMethod(szName, callingConvention, retType, numParameters, parameters),
        m_ModuleID(0)
    {
        m_NumGenericTypeArgNames = numGenericArgNames;
        m_GenericTypeArgNames = genericTypeArgNames;
    }

    ~CSigMethodDef();

    ULONG m_NumGenericTypeArgNames;
    LPWSTR* m_GenericTypeArgNames;

    ModuleID m_ModuleID;

    IClassInfo* m_CanonicalType;
};

class CSigMethodRef : public CSigMethod
{
public:
    CSigMethodRef(
        LPWSTR szName,
        CorCallingConvention callingConvention,
        CSigType* retType,
        ULONG numParameters,
        ISigParameter** parameters,
        ULONG numVarArgParameters,
        ISigParameter** varargParameters
    ) : CSigMethod(szName, callingConvention, retType, numParameters, parameters)
    {
        m_NumVarArgParameters = numVarArgParameters;
        m_VarArgParameters = varargParameters;
    }

    ~CSigMethodRef()
    {
        for (ULONG i = 0; i < m_NumVarArgParameters; i++)
            m_VarArgParameters[i]->Release();

        delete m_VarArgParameters;
    }

    ULONG m_NumVarArgParameters;
    ISigParameter** m_VarArgParameters;
};