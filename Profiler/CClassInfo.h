#pragma once

#include "CSigField.h"

class IClassInfo
{
public:
    IClassInfo(BOOL isArray)
    {
        m_IsArray = isArray;
        m_IsString = FALSE;
    }

    BOOL m_IsArray;
    BOOL m_IsString;
};

class CClassInfo : public IClassInfo
{
public:
    CClassInfo(LPWSTR szName, ULONG numFields, CSigField** fields, COR_FIELD_OFFSET* fieldOffsets) : IClassInfo(FALSE)
    {
        m_szName = _wcsdup(szName);
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

class StringClassInfo : public IClassInfo
{
public:
    StringClassInfo() : IClassInfo(FALSE)
    {
        m_IsString = TRUE;
    }
};