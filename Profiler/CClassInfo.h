#pragma once

#include "CSigField.h"

enum class ClassInfoType
{
    Class,
    Array,
    StandardType
};

class IClassInfo : public CUnknown
{
public:
    IClassInfo(ClassInfoType infoType)
    {
        m_InfoType = infoType;
    }

    ClassInfoType m_InfoType;
};

class CClassInfo : public IClassInfo
{
public:
    CClassInfo(
        LPWSTR szName,
        ModuleID moduleId,
        mdTypeDef typeDef,
        ULONG numFields,
        CSigField** fields,
        COR_FIELD_OFFSET* fieldOffsets,
        ULONG32 numGenericTypeArgs,
        ClassID* genericTypeArgs) : IClassInfo(ClassInfoType::Class)
    {
        m_szName = _wcsdup(szName);
        m_ModuleID = moduleId;
        m_TypeDef = typeDef;
        m_NumFields = numFields;
        m_Fields = fields;
        m_FieldOffsets = fieldOffsets;
        m_NumGenericTypeArgs = numGenericTypeArgs;
        m_GenericTypeArgs = genericTypeArgs;
    }

    ~CClassInfo()
    {
        if (m_szName)
            free(m_szName);

        if (m_Fields)
        {
            for (ULONG i = 0; i < m_NumFields; i++)
                m_Fields[i]->Release();

            delete m_Fields;
        }

        if (m_FieldOffsets)
            delete m_FieldOffsets;

        if (m_GenericTypeArgs)
            delete m_GenericTypeArgs;
    }

    LPWSTR m_szName;
    ModuleID m_ModuleID;
    mdTypeDef m_TypeDef;
    ULONG m_NumFields;
    CSigField** m_Fields;
    COR_FIELD_OFFSET* m_FieldOffsets;
    ULONG32 m_NumGenericTypeArgs;
    ClassID* m_GenericTypeArgs;
};

class CArrayInfo : public IClassInfo
{
public:
    CArrayInfo(IClassInfo* pElementType, CorElementType elementType, ULONG rank) : IClassInfo(ClassInfoType::Array)
    {
        m_pElementType = pElementType;
        m_CorElementType = elementType;
        m_Rank = rank;
    }

    ~CArrayInfo()
    {
        if (m_pElementType)
            m_pElementType->Release();
    }

    IClassInfo* m_pElementType;
    CorElementType m_CorElementType;
    ULONG m_Rank;
};

class CStandardTypeInfo : public IClassInfo
{
public:
    CStandardTypeInfo(CorElementType elementType) : IClassInfo(ClassInfoType::StandardType)
    {
        m_ElementType = elementType;
    }

    CorElementType m_ElementType;
};