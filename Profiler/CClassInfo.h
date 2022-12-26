#pragma once

#include "CSigField.h"

class IClassInfo
{
public:
    IClassInfo(BOOL isArray)
    {
        m_IsArray = isArray;
        m_IsKnownType = FALSE;
    }

    BOOL m_IsArray;
    BOOL m_IsKnownType;
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
        COR_FIELD_OFFSET* fieldOffsets) : IClassInfo(FALSE)
    {
        m_szName = _wcsdup(szName);
        m_ModuleID = moduleId;
        m_TypeDef = typeDef;
        m_NumFields = numFields;
        m_Fields = fields;
        m_FieldOffsets = fieldOffsets;
    }

    ~CClassInfo()
    {
        if (m_szName)
            free(m_szName);

        if (m_Fields)
        {
            for (ULONG i = 0; i < m_NumFields; i++)
                delete m_Fields[i];

            delete m_Fields;
        }

        if (m_FieldOffsets)
            delete m_FieldOffsets;
    }

    LPWSTR m_szName;
    ModuleID m_ModuleID;
    mdTypeDef m_TypeDef;
    ULONG m_NumFields;
    CSigField** m_Fields;
    COR_FIELD_OFFSET* m_FieldOffsets;
};

class CArrayInfo : public IClassInfo
{
public:
    CArrayInfo(IClassInfo* pElementType, CorElementType elementType, ULONG rank) : IClassInfo(TRUE)
    {
        m_pElementType = pElementType;
        m_CorElementType = elementType;
        m_Rank = rank;
    }

    ~CArrayInfo()
    {
        if (m_pElementType)
            delete m_pElementType;
    }

    IClassInfo* m_pElementType;
    CorElementType m_CorElementType;
    ULONG m_Rank;
};

class CKnownTypeInfo : public IClassInfo
{
public:
    CKnownTypeInfo(CorElementType elementType) : IClassInfo(FALSE)
    {
        m_ElementType = elementType;

        m_IsKnownType = TRUE;
    }

    CorElementType m_ElementType;
};