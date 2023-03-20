#pragma once

class CTypeIdentifier
{
public:
    CTypeIdentifier(ModuleID moduleId, mdTypeDef typeDef, ULONG numGenericArgs, ClassID* typeArgs, BOOL ownsArray)
    {
        m_ModuleID = moduleId;
        m_TypeDef = typeDef;
        m_NumGenericArgs = numGenericArgs;

        if (ownsArray)
        {
            m_TypeArgs = new ClassID[m_NumGenericArgs];

            for (ULONG i = 0; i < m_NumGenericArgs; i++)
                m_TypeArgs[i] = typeArgs[i];
        }
        else
            m_TypeArgs = typeArgs;

        m_OwnsArray = ownsArray;
        m_Failed = FALSE;
    }

    //Move constructor
    CTypeIdentifier(CTypeIdentifier&& other) noexcept
    {
        dprintf(L"Move!\n");
        m_ModuleID = other.m_ModuleID;
        m_TypeDef = other.m_TypeDef;
        m_NumGenericArgs = other.m_NumGenericArgs;
        m_TypeArgs = other.m_TypeArgs;
        m_OwnsArray = other.m_OwnsArray;
        m_Failed = other.m_Failed;

        other.m_TypeArgs = nullptr;
        other.m_OwnsArray = FALSE;
    }

    //Copy constructor
    CTypeIdentifier(const CTypeIdentifier& other)
    {
        m_ModuleID = other.m_ModuleID;
        m_TypeDef = other.m_TypeDef;
        m_NumGenericArgs = other.m_NumGenericArgs;

        m_TypeArgs = new ClassID[m_NumGenericArgs];

        for (ULONG i = 0; i < m_NumGenericArgs; i++)
            m_TypeArgs[i] = other.m_TypeArgs[i];

        m_OwnsArray = TRUE;
        m_Failed = other.m_Failed;
    }

    ~CTypeIdentifier()
    {
        if (m_TypeArgs && m_OwnsArray)
            delete[] m_TypeArgs;
    }

    ModuleID m_ModuleID;
    mdTypeDef m_TypeDef;
    ULONG m_NumGenericArgs;
    ClassID* m_TypeArgs;
    BOOL m_Failed;

private:
    BOOL m_OwnsArray;
};

inline bool operator==(const CTypeIdentifier& lhs, const CTypeIdentifier& rhs) noexcept
{
    if (lhs.m_ModuleID != rhs.m_ModuleID)
        return false;

    if (lhs.m_TypeDef != rhs.m_TypeDef)
        return false;

    if (lhs.m_NumGenericArgs != rhs.m_NumGenericArgs)
        return false;

    for(ULONG i = 0; i < lhs.m_NumGenericArgs; i++)
    {
        if (lhs.m_TypeArgs[i] != rhs.m_TypeArgs[i])
            return false;
    }

    return true;
}

template <class T>
inline void hash_combine(std::size_t& seed, const T& v)
{
    std::hash<T> hasher;
    seed ^= hasher(v) + 0x9e3779b9 + (seed << 6) + (seed >> 2);
}

namespace std
{
    template <>
    struct hash<CTypeIdentifier>
    {
        FORCEINLINE size_t operator()(const CTypeIdentifier& x) const noexcept
        {
            size_t seed = std::hash<UINT_PTR>{}(x.m_ModuleID);

            hash_combine(seed, x.m_TypeDef);

            for (ULONG i = 0; i < x.m_NumGenericArgs; i++)
                hash_combine(seed, x.m_TypeArgs[i]);

            return seed;
        }
    };
}