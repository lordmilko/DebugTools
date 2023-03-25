#include "pch.h"
#include "CAssemblyName.h"
#include "CCorProfilerCallback.h"

BOOL SafeCompareString(
    _In_ LPWSTR first,
    _In_ LPWSTR second
)
{
    if (first == nullptr)
    {
        //We ARE nullptr. If other is not, that's a fail
        if (second != nullptr)
            return FALSE;
    }
    else
    {
        //We are NOT nullptr. If other is, that's a fail

        if (second == nullptr)
            return FALSE;
        else
        {
            //Both of us have values. Compare

            if (wcscmp(first, second) != 0)
                return FALSE;
        }
    }

    //No issues detected, all good!
    return TRUE;
}

CAssemblyName::CAssemblyName(
    _In_ ULONG chName,
    _In_ ASSEMBLYMETADATA& asmMetaData,
    _In_ const BYTE* pbPublicKeyOrToken,
    _In_ ULONG cbPublicKeyOrToken)
{
    m_szShortName = _wcsdup(g_szAssemblyName);;

    m_Major = asmMetaData.usMajorVersion;
    m_Minor = asmMetaData.usMinorVersion;
    m_Build = asmMetaData.usBuildNumber;
    m_Revision = asmMetaData.usRevisionNumber;

    m_szLocale = _wcsdup(asmMetaData.szLocale);
    m_szPublicKey = GetPublicKey(pbPublicKeyOrToken, cbPublicKeyOrToken);

    m_szName = GetName(chName);
}

LPWSTR CAssemblyName::GetPublicKey(
    _In_ const BYTE* pbPublicKeyOrToken,
    _In_ ULONG cbPublicKeyOrToken) const
{
    if (!cbPublicKeyOrToken)
        return nullptr;

    //Each byte expands to 2 characters, and then we need 2 bytes per character
    //plus a null terminator
    ULONG bufferLength = (cbPublicKeyOrToken * 2) + 1;
    LPWSTR buffer = (LPWSTR) malloc(bufferLength * sizeof(wchar_t));

    ULONG pos = 0;

    for (ULONG i = 0; i < cbPublicKeyOrToken; i++)
    {
        pos += swprintf_s(
            buffer + pos,
            bufferLength - pos,
            L"%2.2x",
            pbPublicKeyOrToken[i]
        );
    }

    return buffer;
}

LPWSTR CAssemblyName::GetName(
    _In_ ULONG chName) const
{
    chName--; //Ignore the null terminator

    chName += swprintf_s(
        g_szAssemblyName + chName,
        NAME_BUFFER_SIZE - chName,
        L", Version=%d.%d.%d.%d, Culture=",
        m_Major,
        m_Minor,
        m_Build,
        m_Revision
    );

    chName += swprintf_s(
        g_szAssemblyName + chName,
        NAME_BUFFER_SIZE - chName,
        L"%s",
        m_szLocale == nullptr ? L"neutral" : m_szLocale
    );

    chName += swprintf_s(
        g_szAssemblyName + chName,
        NAME_BUFFER_SIZE - chName,
        L", PublicKeyToken="
    );

    if (m_szPublicKey)
    {
        chName += swprintf_s(
            g_szAssemblyName + chName,
            NAME_BUFFER_SIZE - chName,
            L"%s",
            m_szPublicKey
        );
    }
    else
    {
        chName += swprintf_s(
            g_szAssemblyName + chName,
            NAME_BUFFER_SIZE - chName,
            L"null"
        );
    }

    return _wcsdup(g_szAssemblyName);
}

BOOL CAssemblyName::IsMatch(CAssemblyName* pOther, BOOL nameOnly)
{
    if (wcscmp(m_szShortName, pOther->m_szShortName) != 0)
        return FALSE;

    if (nameOnly)
        return TRUE;

    if (!SafeCompareString(m_szLocale, pOther->m_szLocale))
        return FALSE;

    if (!SafeCompareString(m_szPublicKey, pOther->m_szPublicKey))
        return FALSE;

    int version = CompareVersion(pOther);

    //If the comparison returns -1, then other was higher. If the comparison returns 1,
    //we're higher, which means it's not a match
    if (version > 0)
        return FALSE;

    return TRUE;
}

int CAssemblyName::CompareVersion(CAssemblyName* pOther) const
{
    if (m_Major != pOther->m_Major)
        return m_Major > pOther->m_Major ? 1 : -1;

    if (m_Minor != pOther->m_Minor)
        return m_Minor > pOther->m_Minor ? 1 : -1;

    if (m_Build != pOther->m_Build)
        return m_Build > pOther->m_Build ? 1 : -1;

    if (m_Revision != pOther->m_Revision)
        return m_Revision > pOther->m_Revision ? 1 : -1;

    return 0;
}
