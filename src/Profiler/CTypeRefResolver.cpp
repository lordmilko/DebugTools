#include "pch.h"
#include "CTypeRefResolver.h"
#include "CCorProfilerCallback.h"

HRESULT CTypeRefResolver::Resolve(
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    HRESULT hr = S_OK;
    mdToken tkResolutionScope;
    CorTokenType resolutionScopeType;
    WCHAR typeName[NAME_BUFFER_SIZE];

    auto match = g_pProfiler->m_ModuleInfoMap.find(m_ModuleID);

    if (match == g_pProfiler->m_ModuleInfoMap.end())
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    m_pModuleInfo = match->second;

    if (m_TypeRef != mdTokenNil)
    {
        CLock typeRefLock(&m_pModuleInfo->m_TypeRefMutex);

        auto refMatch = m_pModuleInfo->m_TypeRefMap.find(m_TypeRef);

        if (refMatch != m_pModuleInfo->m_TypeRefMap.end())
        {
            CModuleIDAndTypeDef* item = refMatch->second;

            if (item->m_Failed)
                return E_FAIL;

            *moduleId = item->m_ModuleID;
            *typeDef = item->m_TypeDef;
            goto ErrExit;
        }
    }

    IfFailGo(m_pModuleInfo->m_pMDI->GetTypeRefProps(m_TypeRef, &tkResolutionScope, typeName, NAME_BUFFER_SIZE, NULL));

    resolutionScopeType = (CorTokenType) TypeFromToken(tkResolutionScope);

    if (resolutionScopeType == mdtAssemblyRef)
    {
        IfFailGo(ResolveAssemblyRef(tkResolutionScope, typeName, moduleId, typeDef));
    }
    else if (resolutionScopeType == mdtTypeRef)
    {
       IfFailGo(ResolveNestedType(tkResolutionScope, typeName, moduleId, typeDef));
    }
    else
        hr = PROFILER_E_UNKNOWN_RESOLUTION_SCOPE;

ErrExit:
    return hr;
}

HRESULT CTypeRefResolver::ResolveAssemblyRef(
    _In_ mdAssemblyRef assemblyRef,
    _In_ LPWSTR szTypeName,
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    /* Resolving a mdTypeRef from an mdAssemblyRef basically involves implementing the entire logic of the
     * .NET Framework assembly loader system. dnlib appears to do this, and goes to great lengths to resolve
     * an mdTypeRef, even going so far as to look in the GAC.
     *
     * An mdAssemblyRef is a record in Assembly B's AssemblyRef table that describes Assembly A that it needs to
     * link to in order to satisfy types referenced from A in B. Assembly B's AssemblyRef record contains the following
     * pieces of information that can be used to help locate Assembly A
     * - A's name
     * - A's version (see ASSEMBLYMETADATA)
     * - A's locale (see ASSEMBLYMETADATA)
     * - The full public key of A, or the low 8 bytes of the SHA-1 hash of A's public key (ECMA 335 section II.6.3, PDF page 144)
     *
     * Combining all of these values into one will get you the name you would typically see by inspecting System.Assembly.FullName
     *
     * When searching for assemblies, assemblies may have a slightly higher version than the one initially hardcoded in the AssemblyRef.
     * This should be allowed if an exact version match cannot be found. Once a type has been found, it may be forwarded, so any forwards
     * must be followed. We do not attempt to build a bullet proof type loader; we are happy with a system that is "good enough",
     * and will add to it as required.
     *
     * The following demonstrates how the public key token can be computed for the Standard Public Key found
     * in all assemblies in the Standard Library using PowerShell (II.6.2.1.3)
     *
     *     $sha = New-Object System.Security.Cryptography.SHA1CryptoServiceProvider
     *     $publicKey = 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x04,0x00,0x00,0x00,0x00,0x00,0x00,0x00
     *     $tokenBytes = $sha.ComputeHash($publicKey)|foreach { $_.tostring("X") }
     *     [array]::reverse($tokenBytes)
     *     ($tokenBytes|select -first 8) -join " "
     */

    HRESULT hr = S_OK;

    IMetaDataAssemblyImport* pMDAI = nullptr;
    const BYTE* pbPublicKeyOrToken;
    ULONG cbPublicKeyOrToken;
    ULONG chName = 0;
    ASSEMBLYMETADATA asmMetaData;
    ZeroMemory(&asmMetaData, sizeof(ASSEMBLYMETADATA));
    CorAssemblyFlags asmFlags;

    CAssemblyInfo* pAssemblyInfo = nullptr;
    BOOL added = FALSE;

    CAssemblyName* pAssemblyName = nullptr;
    IfFailGo(m_pModuleInfo->m_pMDI->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&pMDAI));

    IfFailGo(pMDAI->GetAssemblyRefProps(
        assemblyRef,
        (const void**)&pbPublicKeyOrToken,
        &cbPublicKeyOrToken,
        g_szAssemblyName,
        NAME_BUFFER_SIZE,
        &chName,
        &asmMetaData,
        NULL,
        NULL,
        (DWORD*)&asmFlags
    ));

    IfFailGo(g_pProfiler->GetAssemblyName(
        chName,
        asmMetaData,
        pbPublicKeyOrToken,
        cbPublicKeyOrToken,
        asmFlags & afPublicKey,
        &pAssemblyName
    ));

    //Lock scope
    {
        CLock assemblyLock(&g_pProfiler->m_AssemblyMutex);

        auto nameMatch = g_pProfiler->m_AssemblyNameMap.find(pAssemblyName->m_szName);

        if (nameMatch != g_pProfiler->m_AssemblyNameMap.end())
            pAssemblyInfo = nameMatch->second;
        else
        {
            /* Tryand get the best possible match.First try for an assembly with a higher version,
             * otherwise fallback to one with a simple name match. Multiple versions of the same assembly could be loaded into
             * the process (e.g. StreamJsonRpc 1.5 and 2.8). If we were looking for 2.7, it's better to go for 2.8 */

            for (auto& kv : g_pProfiler->m_AssemblyInfoMap)
            {
                if (pAssemblyName->IsMatch(kv.second->m_pAssemblyName, FALSE))
                {
                    pAssemblyInfo = kv.second;
                    break;
                }
            }

            if (pAssemblyInfo == nullptr)
            {
                for (auto& kv : g_pProfiler->m_AssemblyInfoMap)
                {
                    if (pAssemblyName->IsMatch(kv.second->m_pAssemblyName, TRUE))
                    {
                        pAssemblyInfo = kv.second;
                        break;
                    }
                }
            }

            if (pAssemblyInfo == nullptr)
            {
                hr = E_NOTIMPL;
                goto ErrExit;
            }
        }

        //Maintain lock on pAssemblyInfo as long as we are still using it
        IfFailGo(GetModuleIDAndTypeDefFromAssembly(pAssemblyInfo, szTypeName, moduleId, typeDef));
    }

    if (m_TypeRef != mdTokenNil)
    {
        CLock typeRefLock(&m_pModuleInfo->m_TypeRefMutex, true);

        m_pModuleInfo->m_TypeRefMap[m_TypeRef] = new CModuleIDAndTypeDef(*moduleId, *typeDef, FALSE);
        added = TRUE;
    }

ErrExit:
    if (!added && m_TypeRef != mdTokenNil)
    {
        CLock typeRefLock(&m_pModuleInfo->m_TypeRefMutex, true);

        m_pModuleInfo->m_TypeRefMap[m_TypeRef] = new CModuleIDAndTypeDef(0, 0, TRUE);
    }

    if (pAssemblyName)
        delete pAssemblyName;

    if (pMDAI)
        pMDAI->Release();

    return hr;
}

HRESULT CTypeRefResolver::ResolveNestedType(
    _In_ mdTypeRef parentTypeRef,
    _In_ LPWSTR szNestedType,
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    HRESULT hr = S_OK;
    ModuleID innerModuleId;
    mdTypeDef innerTypeDef;

    CTypeRefResolver resolver(m_ModuleID, parentTypeRef);

    IfFailGo(resolver.Resolve(&innerModuleId, &innerTypeDef));

    CModuleInfo* pInnerModule;
    IfFailGo(g_pProfiler->GetModuleInfo(innerModuleId, &pInnerModule));

    IfFailGo(pInnerModule->m_pMDI->FindTypeDefByName(szNestedType, innerTypeDef, typeDef));
    *moduleId = innerModuleId;

    //Lock scope
    {
        CLock typeRefLock(&m_pModuleInfo->m_TypeRefMutex, true);
        m_pModuleInfo->m_TypeRefMap[m_TypeRef] = new CModuleIDAndTypeDef(*moduleId, *typeDef, FALSE);
    }

ErrExit:
    return hr;
}

HRESULT CTypeRefResolver::GetModuleIDAndTypeDefFromAssembly(
    _In_ CAssemblyInfo* pAssemblyInfo,
    _In_ LPWSTR szTypeName,
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    HRESULT hr = S_OK;
    mdToken ptd = 0;

    for(ULONG i = 0; i < pAssemblyInfo->m_NumModules; i++)
    {
        IMetaDataImport2* pMDI = pAssemblyInfo->m_Modules[i]->m_pMDI;

        hr = pMDI->FindTypeDefByName(szTypeName, mdTokenNil, &ptd);

        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            //Maybe it's actually a forwarded type
            if (GetModuleIDAndTypeDefFromForwardedType(pAssemblyInfo->m_Modules[i], szTypeName, moduleId, typeDef) == S_OK)
            {
                hr = S_OK;
                break;
            }
        }
        else if (hr == S_OK)
        {
            *moduleId = pAssemblyInfo->m_Modules[i]->m_ModuleID;
            *typeDef = ptd;
            break;
        }
    }

    return hr;
}

HRESULT CTypeRefResolver::GetModuleIDAndTypeDefFromForwardedType(
    _In_ CModuleInfo* pModuleInfo,
    _In_ LPWSTR szTypeName,
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    HRESULT hr = S_OK;
    IMetaDataAssemblyImport* pMDAI = nullptr;
    mdExportedType tkExportedType;
    mdToken tkImplementation;
    CorTokenType implType;

    IMetaDataImport2* pMDI = pModuleInfo->m_pMDI;

    IfFailGo(pMDI->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&pMDAI));

    IfFailGo(pMDAI->FindExportedTypeByName(szTypeName, mdExportedTypeNil, &tkExportedType));

    IfFailGo(pMDAI->GetExportedTypeProps(
        tkExportedType,
        szTypeName,
        NAME_BUFFER_SIZE,
        NULL,
        &tkImplementation,
        NULL,
        NULL
    ));

    implType = (CorTokenType)TypeFromToken(tkImplementation);

    if (implType == mdtAssemblyRef)
    {
        CTypeRefResolver resolver(pModuleInfo->m_ModuleID, mdTokenNil);
        resolver.m_pModuleInfo = pModuleInfo;

        IfFailGo(resolver.ResolveAssemblyRef(tkImplementation, szTypeName, moduleId, typeDef));
    }
    else
        hr = E_FAIL;

ErrExit:
    return hr;
}