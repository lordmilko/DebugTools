#pragma once

class CAssemblyName
{
public:
    CAssemblyName(
        _In_ ULONG chName,
        _In_ ASSEMBLYMETADATA& asmMetaData,
        _In_ const BYTE* pbPublicKeyOrToken,
        _In_ ULONG cbPublicKeyOrToken);

    ~CAssemblyName()
    {
        if (m_szName)
            free(m_szName);

        if (m_szShortName)
            free(m_szShortName);

        if (m_szLocale)
            free(m_szLocale);

        if (m_szPublicKey)
            free(m_szPublicKey);
    }

    BOOL IsMatch(CAssemblyName* pOther, BOOL nameOnly);

    LPWSTR m_szName;

private:
    LPWSTR GetName(
        _In_ ULONG chName) const;

    LPWSTR GetPublicKey(
        _In_ const BYTE* pbPublicKeyOrToken,
        _In_ ULONG cbPublicKeyOrToken) const;

    int CompareVersion(CAssemblyName* pOther) const;

    LPWSTR m_szShortName;
    USHORT m_Major;
    USHORT m_Minor;
    USHORT m_Build;
    USHORT m_Revision;

    LPWSTR m_szLocale;

    LPWSTR m_szPublicKey;
};