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
        /* The CLR uses special code sharing techniques to represent generic types in certain scenarios. In order to resolve the meaning of an ELEMENT_TYPE_VAR, there are three
         * potential resolution mechanisms the CLR might use: Inspect the "this" parameter, inspect the InstantiatedMethodDesc or inspect the MethodTable (https://yizhang82.dev/dotnet-generics-typeof-t).
         * Pointers to these items are passed in CPU registers so that they may be accessed if need be, such as the thisArg and extraArg parameters of COR_PRF_FRAME_INFO_INTERNAL.
         * In certain scenarios however this can present a problem: consider the method Microsoft.VisualStudio.Utilities.dll!GlobalBrokeredServiceContainer+View.TryGetProfferingSourceAsync,
         * which returns a ValueTask<ValueTuple<>>.
         *
         * As part of the awaiting process, AsyncValueTaskMethodBuilder<T>.Task is accessed, which returns ValueTask<T>. Normally GetFunctionInfo2 would be able to resolve that
         * the T in ValueTask<T> is ValueTuple<>, however when we are in a leave callback, the CPU registers that thisArg and extraArg will probably have been overwritten as a result of
         * the method's normal course of action. As such, ProfilingGetFunctionLeave3Info() (called by GetFunctionLeave3Info()) sets thisArg and extraArg to null. When GetFunctionInfo2 executes,
         * MethodDesc::IsSharedByGenericInstantiations() will report TRUE, however GetExactInstantiationsOfMethodAndItsClassFromCallInformation() will return FALSE when AcquiresInstMethodTableFromThis() is TRUE
         * since our thisArg was null.
         *
         * In theory, we could inspect all our m_CanonicalGenericTypes to find one with a matching mdTypeDef and ModuleID, however this is not a good idea: the generic type args on this type will simply
         * be listed as System.__Canon, which is considered to be a reference type. As such, in the case of ValueTask<T>, it contains some field of type ValueTuple<> which we'll incorrectly treat as a reference
         * type and attempt to perform an indirection against the structure in an attempt to read the ObjectID. Furthermore, returning the canonical type can cause faulty information to be trickled down multiple levels.
         * If we failed to resolve the true implementation of AsyncValueTaskMethodBuilder<T>, we're going to pass T as being System.__Canon onto our field of type ValueTuple<T> which will in turn pass System.__Canon on to its
         * field of type T which is meant to be ValueTuple<> but is now being incorrectly treated as a reference type. */

        hr = PROFILER_E_NO_CLASSID;
        goto ErrExit;
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