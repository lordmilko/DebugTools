#pragma once

#include <functional>

class CSigReader;
class CSigMethod;

class CSigType
{
public:
    static HRESULT New(_In_ CSigReader& reader, _Out_ CSigType** ppType);

    virtual ~CSigType() = default;

    CSigType(CorElementType type, BOOL isByRef)
    {
        m_Type = type;
        m_IsByRef = isByRef;
    }

    virtual HRESULT Initialize(CSigReader& reader);

    HRESULT GetName(
        _In_ mdToken token,
        _In_ IMetaDataImport2* pMDI,
        _Out_ LPWSTR* szName);

    HRESULT GetMethodGenericArgName(
        _In_ ULONG index,
        _In_ CSigReader& reader,
        _Out_ LPWSTR* szName);

    CorElementType m_Type;
    BOOL m_IsByRef;
};

class CSigArrayType final : public CSigType
{
public:
    CSigArrayType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_pElementType(nullptr),
        m_Rank(0),
        m_NumSizes(0),
        m_Sizes(nullptr),
        m_NumLowerBounds(0),
        m_LowerBounds(nullptr)
    {
    }

    ~CSigArrayType()
    {
        if (m_pElementType)
            delete m_pElementType;

        if (m_Sizes)
            delete[] m_Sizes;

        if (m_LowerBounds)
            delete[] m_LowerBounds;
    }

    HRESULT Initialize(CSigReader& reader) override;

    CSigType* m_pElementType;
    ULONG m_Rank;

    ULONG m_NumSizes;
    ULONG* m_Sizes;

    ULONG m_NumLowerBounds;
    int* m_LowerBounds;
};

class CSigClassType final : public CSigType
{
public:
    CSigClassType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_Token(0),
        m_szName(nullptr)
    {
    }

    ~CSigClassType()
    {
        if (m_szName)
            free(m_szName);
    }

    HRESULT Initialize(CSigReader& reader) override;

    mdToken m_Token;
    LPWSTR m_szName;
};

class CSigValueType final : public CSigType
{
public:
    CSigValueType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_Token(0),
        m_szName(nullptr)
    {
    }

    ~CSigValueType()
    {
        if (m_szName)
            free(m_szName);
    }

    HRESULT Initialize(CSigReader& reader) override;

    mdToken m_Token;
    LPWSTR m_szName;
};

class CSigFnPtrType final : public CSigType
{
public:
    CSigFnPtrType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_pMethod(nullptr)
    {
    }

    ~CSigFnPtrType();

    HRESULT Initialize(CSigReader& reader) override;

    CSigMethod* m_pMethod;
};

class CSigGenericType final : public CSigType
{
public:
    CSigGenericType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_GenericTypeDefinitionToken(0),
        m_szGenericTypeDefinitionName(nullptr),
        m_NumGenericArgs(0),
        m_GenericArgs(nullptr)
    {
    }

    ~CSigGenericType()
    {
        if (m_szGenericTypeDefinitionName)
            free(m_szGenericTypeDefinitionName);

        if (m_GenericArgs)
        {
            for (ULONG i = 0; i < m_NumGenericArgs; i++)
                delete m_GenericArgs[i];

            delete m_GenericArgs;
        }
    }

    HRESULT Initialize(CSigReader& reader) override;

    mdToken m_GenericTypeDefinitionToken;
    LPWSTR m_szGenericTypeDefinitionName;

    ULONG m_NumGenericArgs;
    CSigType** m_GenericArgs;
};

class CSigMethodGenericArgType final : public CSigType
{
public:
    CSigMethodGenericArgType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_Index(0),
        m_szName(nullptr)
    {
    }

    ~CSigMethodGenericArgType()
    {
        if (m_szName)
            free(m_szName);
    }

    HRESULT Initialize(CSigReader& reader) override;

    ULONG m_Index;
    LPWSTR m_szName;
};

class CSigTypeGenericArgType final : public CSigType
{
public:
    CSigTypeGenericArgType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_Index(0),
        m_szName(nullptr)
    {
    }

    ~CSigTypeGenericArgType()
    {
        if (m_szName)
            free(m_szName);
    }

    HRESULT Initialize(CSigReader& reader) override;

    ULONG m_Index;
    LPWSTR m_szName;
};

class CSigPtrType final : public CSigType
{
public:
    CSigPtrType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_pPtrType(nullptr)
    {
    }

    ~CSigPtrType()
    {
        if (m_pPtrType)
            delete m_pPtrType;
    }

    HRESULT Initialize(CSigReader& reader) override;

    CSigType* m_pPtrType;
};

class CSigSZArrayType final : public CSigType
{
public:
    CSigSZArrayType(CorElementType type, BOOL isByRef) : CSigType(type, isByRef),
        m_pElementType(nullptr)
    {
    }

    ~CSigSZArrayType()
    {
        if (m_pElementType)
            delete m_pElementType;
    }

    HRESULT Initialize(CSigReader& reader) override;

    CSigType* m_pElementType;
};