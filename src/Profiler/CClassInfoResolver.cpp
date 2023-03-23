#include "pch.h"
#include "CClassInfoResolver.h"
#include "CCorProfilerCallback.h"
#include "CValueTracer.h"

HRESULT CClassInfoResolver::Resolve(
    _Out_ CClassInfo** ppClassInfo)
{
    if (m_pClassInfo != nullptr)
    {
        *ppClassInfo = (CClassInfo*)m_pClassInfo;
        return S_OK;
    }

    HRESULT hr = S_OK;

    IfFailGo(GetMethodTypeArgsAndContainingClass(&m_pClassInfo));

    *ppClassInfo = (CClassInfo*) m_pClassInfo;

ErrExit:
    return hr;
}

HRESULT CClassInfoResolver::GetMethodTypeArgsAndContainingClass(
    _Out_ IClassInfo** ppMethodClassInfo)
{
    HRESULT hr = S_OK;

    ClassID classId;
    ClassID* typeArgs = nullptr;
    IClassInfo* pMethodClassInfo = nullptr;
    ModuleID moduleId = 0;
    mdToken funcToken = 0;

    if (m_pMethod->m_NumGenericTypeArgNames)
    {
        typeArgs = new ClassID[m_pMethod->m_NumGenericTypeArgNames];
        ULONG32 cTypeArgs;

        IfFailGo(g_pProfiler->m_pInfo->GetFunctionInfo2(
            m_FunctionId.functionID,
            m_FrameInfo,
            &classId,
            &moduleId,
            &funcToken,
            m_pMethod->m_NumGenericTypeArgNames,
            &cTypeArgs,
            typeArgs
        ));

        m_pTracer->m_MethodGenericTypeArgs = new IClassInfo*[m_pMethod->m_NumGenericTypeArgNames];

        for (ULONG i = 0; i < m_pMethod->m_NumGenericTypeArgNames; i++)
        {
            IClassInfo* info;
            IfFailGo(g_pProfiler->GetClassInfoFromClassId(typeArgs[i], &info));

            m_pTracer->m_MethodGenericTypeArgs[i] = info;
        }
    }
    else
    {
        IfFailGo(g_pProfiler->m_pInfo->GetFunctionInfo2(
            m_FunctionId.functionID,
            m_FrameInfo,
            &classId,
            &moduleId,
            &funcToken,
            0,
            NULL,
            NULL
        ));
    }

    if (classId == 0)
    {
        /* GetFunctionInfo2() can set the classId to 0 if no class information is available. In this case, you'd think a good
         * strategy to try next might to be get the function's mdTypeDef and resolve the classId using a ModuleID + mdTypeDef.
         * Ostensibly we could give it a go trying to load the class using the ModuleID + mdTypeDef via GetClassFromToken,
         * however this scenario has been seen to occur with generic types, meaning we need to call GetClassFromTokenAndTypeArgs,
         * but we don't have a way to get the typearg class IDs. We could simply forgo getting an IClassInfo at all
         * (it only affects ELEMENT_TYPE_VAR) however this has been seen to affect a GenericInst with typeargs including ELEMENT_TYPE_VAR.
         *
         * In most scenarios we don't actually even need to know our parent type; a method that returns a T[] can get the value of T
         * by asking the array what its element type is. Other methods don't even appear to need this info at all. As such, we give it a go
         * attempting to get the canonical parent type (if possible) based on the ModuleID and mdTypeDef. */

        //Retrieving the canonical type of a method isn't cheap, so cache it if we've ever seen it. We can't make any assumptions
        //as to whether we'll have to fallback to using the canonical type on enter/leave or not, so we can only fallback after
        //we've confirmed we couldn't get the ClassID for a concrete type.
        if (m_pMethod->m_CanonicalType == nullptr)
        {
            IfFailGo(GetMethodCanonicalType(moduleId, funcToken, &pMethodClassInfo));

            m_pMethod->m_CanonicalType = pMethodClassInfo;
            pMethodClassInfo->AddRef();
        }
        else
        {
            pMethodClassInfo = m_pMethod->m_CanonicalType;
        }
    }
    else
    {
        //Get the class info of the class the method being invoked is defined in
        IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, &pMethodClassInfo));
    }

    *ppMethodClassInfo = pMethodClassInfo;

ErrExit:
    if (typeArgs)
        delete[] typeArgs;

    return hr;
}

HRESULT CClassInfoResolver::GetMethodCanonicalType(
    _In_ ModuleID moduleId,
    _In_ mdToken token,
    _Out_ IClassInfo** ppMethodClassInfo)
{
    if (moduleId == 0)
        return PROFILER_E_NO_CLASSID;

    CorTokenType type = (CorTokenType)TypeFromToken(token);

    if (type != mdtMethodDef)
        return PROFILER_E_NO_CLASSID;

    HRESULT hr = S_OK;
    CModuleInfo* pModuleInfo;
    mdTypeDef typeDef;

    IfFailGo(g_pProfiler->GetModuleInfo(moduleId, &pModuleInfo));

    IfFailGo(pModuleInfo->m_pMDI->GetMethodProps(token, &typeDef, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL));

    //Lock scope
    {
        CLock classLock(&g_pProfiler->m_ClassMutex);

        for (auto& item : g_pProfiler->m_CanonicalGenericTypes)
        {
            if (item->m_ModuleID == moduleId && item->m_TypeDef == typeDef)
            {
                //dprintf(L"Got canonical type %I64X %s\n", item->m_ClassID, item->m_szName);
                *ppMethodClassInfo = item;
                hr = S_OK;
                goto ErrExit;
            }
        }
    }

ErrExit:
    if (*ppMethodClassInfo == nullptr)
        return PROFILER_E_NO_CLASSID;

    return hr;
}