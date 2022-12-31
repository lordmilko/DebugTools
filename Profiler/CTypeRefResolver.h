#pragma once

class CAssemblyInfo;
class CModuleInfo;

class CTypeRefResolver
{
public:
    CTypeRefResolver(ModuleID moduleId, mdTypeRef typeRef)
    {
        m_ModuleID = moduleId;
        m_TypeRef = typeRef;
    }

    HRESULT Resolve(
        _Out_ ModuleID* moduleId,
        _Out_ mdTypeDef* typeDef
    );

    HRESULT ResolveAssemblyRef(
        _In_ mdAssemblyRef assemblyRef,
        _Out_ ModuleID* moduleId,
        _Out_ mdTypeDef* typeDef);

    HRESULT GetModuleIDAndTypeDefFromAssembly(
        _In_ CAssemblyInfo* pAssemblyInfo,
        _Out_ ModuleID* moduleId,
        _Out_ mdTypeDef* typeDef);

    HRESULT GetModuleIDAndTypeDefFromForwardedType(
        _In_ IMetaDataImport* pMDI,
        _In_ CModuleInfo* pModuleInfo,
        _Out_ ModuleID* moduleId,
        _Out_ mdTypeDef* typeDef);

private:
    ModuleID m_ModuleID;
    mdTypeRef m_TypeRef;

    CModuleInfo* m_pModuleInfo;
};