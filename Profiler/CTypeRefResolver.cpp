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
    CLock moduleMutex(&g_pProfiler->m_ModuleMutex);

    CModuleInfo* pModuleInfo = g_pProfiler->m_ModuleInfoMap[m_ModuleID];

    if (!pModuleInfo)
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    m_pModuleInfo = pModuleInfo;

    IfFailGo(pModuleInfo->m_pMDI->GetTypeRefProps(m_TypeRef, &tkResolutionScope, g_szTypeName, NAME_BUFFER_SIZE, NULL));

    resolutionScopeType = (CorTokenType) TypeFromToken(tkResolutionScope);

    if (resolutionScopeType == mdtAssemblyRef)
    {
        IfFailGo(ResolveAssemblyRef(tkResolutionScope, moduleId, typeDef));
    }

ErrExit:
    return hr;
}

HRESULT CTypeRefResolver::ResolveAssemblyRef(
    _In_ mdAssemblyRef assemblyRef,
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
     * must be followed.
     *
     * The following demonstrates how the public key token can be computed for the Standard Public Key found
     * in all assemblies in the Standard Library using PowerShell (II.6.2.1.3)
     *
     *     $sha = New-Object System.Security.Cryptography.SHA1CryptoServiceProvider
     *     $publicKey = 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x04,0x00,0x00,0x00,0x00,0x00,0x00,0x00
     *     $tokenBytes = $sha.ComputeHash($publicKey)|foreach { $_.tostring("X") }
     *     [array]::reverse($tokenBytes)
     *     ($tokenBytes|select -first 8) -join " "  */

    HRESULT hr = S_OK;

    IMetaDataAssemblyImport* pMDAI = nullptr;
    const BYTE* pbPublicKeyOrToken;
    ULONG cbPublicKeyOrToken;
    ULONG chName = 0;
    ASSEMBLYMETADATA asmMetaData;
    ZeroMemory(&asmMetaData, sizeof(ASSEMBLYMETADATA));
    CorAssemblyFlags asmFlags;

    CAssemblyInfo* pAssemblyInfo;
    LPWSTR shortAsmName = nullptr;
    BOOL added = FALSE;

    //Lock scope
    {
        CLock asmRefLock(&m_pModuleInfo->m_AsmRefMutex);

        CModuleIDAndTypeDef* match = m_pModuleInfo->m_AsmRefMap[assemblyRef];

        if (match)
        {
            if (match->m_Failed)
                return E_FAIL;

            *moduleId = match->m_ModuleID;
            *typeDef = match->m_TypeDef;
            return S_OK;
        }
    }

    LPWSTR assemblyName = nullptr;
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

    shortAsmName = _wcsdup(g_szAssemblyName);

    IfFailGo(g_pProfiler->GetAssemblyName(
        chName,
        asmMetaData,
        pbPublicKeyOrToken,
        cbPublicKeyOrToken,
        asmFlags & afPublicKey,
        &assemblyName
    ));

    //Lock scope
    {
        CLock assemblyLock(&g_pProfiler->m_AssemblyMutex);

        pAssemblyInfo = g_pProfiler->m_AssemblyNameMap[assemblyName];

        if (pAssemblyInfo == nullptr)
            pAssemblyInfo = g_pProfiler->m_AssemblyShortNameMap[shortAsmName];

        if (!pAssemblyInfo)
        {
            hr = E_NOTIMPL;
            goto ErrExit;
        }

        //Maintain lock on pAssemblyInfo as long as we are still using it
        IfFailGo(GetModuleIDAndTypeDefFromAssembly(pAssemblyInfo, moduleId, typeDef));
    }

    //Lock scope
    {
        CLock asmRefLock(&m_pModuleInfo->m_AsmRefMutex);

        m_pModuleInfo->m_AsmRefMap[assemblyRef] = new CModuleIDAndTypeDef(*moduleId, *typeDef, FALSE);
        added = TRUE;
    }

ErrExit:
    if (!added)
    {
        CLock asmRefLock(&m_pModuleInfo->m_AsmRefMutex);

        m_pModuleInfo->m_AsmRefMap[assemblyRef] = new CModuleIDAndTypeDef(0, 0, TRUE);
    }

    if (assemblyName)
        free(assemblyName);

    if (shortAsmName)
        free(shortAsmName);

    if (pMDAI)
        pMDAI->Release();

    return hr;
}

HRESULT CTypeRefResolver::GetModuleIDAndTypeDefFromAssembly(
    _In_ CAssemblyInfo* pAssemblyInfo,
    _Out_ ModuleID* moduleId,
    _Out_ mdTypeDef* typeDef)
{
    HRESULT hr = S_OK;
    mdToken ptd = 0;

    for(ULONG i = 0; i < pAssemblyInfo->m_NumModules; i++)
    {
        IMetaDataImport2* pMDI = pAssemblyInfo->m_Modules[i]->m_pMDI;

        hr = pMDI->FindTypeDefByName(g_szTypeName, mdTokenNil, &ptd);

        if (hr == S_OK)
        {
            *moduleId = pAssemblyInfo->m_Modules[i]->m_ModuleID;
            *typeDef = ptd;
            break;
        }
    }

    return hr;
}