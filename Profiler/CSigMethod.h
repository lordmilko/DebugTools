#pragma once

#include "CSigType.h"
#include "ISigParameter.h"

class CSigMethod
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
            delete m_pRetType;

        if (m_Parameters)
        {
            for (ULONG i = 0; i < m_NumParameters; i++)
                delete m_Parameters[i];

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
    //todo: will this call the base ctor?
    CSigMethodDef(
        LPWSTR szName,
        CorCallingConvention callingConvention,
        CSigType* retType,
        ULONG numParameters,
        ISigParameter** parameters,
        ULONG numGenericArgNames,
        LPWSTR* genericTypeArgNames) : CSigMethod(szName, callingConvention, retType, numParameters, parameters)
    {
        m_NumGenericTypeArgs = numGenericArgNames;
        m_GenericTypeArgNames = genericTypeArgNames;
    }

    ~CSigMethodDef()
    {
        if (m_GenericTypeArgNames)
        {
            for (ULONG i = 0; i < m_NumGenericTypeArgs; i++)
                free(m_GenericTypeArgNames[i]);

            free(m_GenericTypeArgNames);
        }
    }

    ULONG m_NumGenericTypeArgs;
    LPWSTR* m_GenericTypeArgNames;
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
    }

    ~CSigMethodRef()
    {
        for (ULONG i = 0; i < m_NumVarArgParameters; i++)
            delete m_VarArgParameters[i];

        delete m_VarArgParameters;
    }

    ULONG m_NumVarArgParameters;
    ISigParameter** m_VarArgParameters;
};