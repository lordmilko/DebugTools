#include "pch.h"
#include "CClassInfoResolver.h"
#include "CCorProfilerCallback.h"
#include "CValueTracer.h"

HRESULT CClassInfoResolver::Resolve(
    _In_ CClassInfo** ppClassInfo)
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

        m_pTracer->m_GenericTypeArgs = new IClassInfo * [m_pMethod->m_NumGenericTypeArgNames];

        for (ULONG i = 0; i < m_pMethod->m_NumGenericTypeArgNames; i++)
        {
            IClassInfo* info;
            IfFailGo(m_pTracer->GetClassInfoFromClassId(typeArgs[i], &info));

            m_pTracer->m_GenericTypeArgs[i] = info;
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
        //GetFunctionInfo2() can set the classId to 0 if no class information is available. In this case, lets get the function's mdTypeDef
        //and resolve the classId using a ModuleID + mdTypeDef. Ostensibly we could give it a go trying to load the class
        //using the ModuleID + mdTypeDef via GetClassFromToken, however this scenario has been seen to occur with generic types,
        //meaning we need to call GetClassFromTokenAndTypeArgs, but we don't have a way to get the typearg class IDs. We could
        //simply forgo getting an IClassInfo at all (it only affects ELEMENT_TYPE_VAR) however this has been seen to affect
        //a GenericInst with typeargs including ELEMENT_TYPE_VAR

        hr = PROFILER_E_NO_CLASSID;
        goto ErrExit;
    }
    else
    {
        //Get the class info of the class the method being invoked is defined in
        IfFailGo(m_pTracer->GetClassInfoFromClassId(classId, &pMethodClassInfo));
    }

    *ppMethodClassInfo = pMethodClassInfo;

ErrExit:
    if (typeArgs)
        delete typeArgs;

    return hr;
}