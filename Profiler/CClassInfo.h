#pragma once

#include "CSigField.h"

class CClassInfo
{
public:
    CClassInfo(ULONG numFields, CSigField** fields, COR_FIELD_OFFSET* fieldOffsets)
    {
        m_NumFields = numFields;
        m_Fields = fields;
        m_FieldOffsets = fieldOffsets;
    }

    ~CClassInfo()
    {
        if (m_Fields)
        {
            for (ULONG i = 0; i < m_NumFields; i++)
                delete m_Fields[i];

            delete m_Fields;
        }

        if (m_FieldOffsets)
            delete m_FieldOffsets;
    }

    ULONG m_NumFields;
    CSigField** m_Fields;
    COR_FIELD_OFFSET* m_FieldOffsets;
};
