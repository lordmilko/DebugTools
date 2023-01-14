#pragma once

#include <Shlwapi.h>

enum class MatchKind
{
    All = 1,
    Contains,
    ModuleName,
    StartsWith,
    EndsWith,
    Literal
};

class CMatchItem
{
public:
    CMatchItem() :
        m_MatchKind((MatchKind)0),
        m_szValue(nullptr)
    {
    }

    CMatchItem(MatchKind matchKind, LPWSTR szValue)
    {
        m_MatchKind = matchKind;
        m_szValue = szValue;
    }

    CMatchItem(const CMatchItem& existing)
    {
        m_MatchKind = existing.m_MatchKind;
        m_szValue = _wcsdup(existing.m_szValue);
    }

    CMatchItem(CMatchItem&& existing) noexcept : //Move constructor
        m_MatchKind(existing.m_MatchKind),
        m_szValue(std::move(existing.m_szValue))
    {
        existing.m_szValue = nullptr;
    }

    ~CMatchItem()
    {
        if (m_szValue)
            free(m_szValue);
    }

    bool IsMatch(LPWSTR str)
    {
        switch (m_MatchKind)
        {
        case MatchKind::All:
            return TRUE;

        case MatchKind::ModuleName:
            return lstrcmpiW(m_szValue, str) == 0;

        case MatchKind::Contains:
            return StrStrI(str, m_szValue) != NULL;
        default:
            break;
        }

        return true;
    }

    MatchKind m_MatchKind;
    LPWSTR m_szValue;
};