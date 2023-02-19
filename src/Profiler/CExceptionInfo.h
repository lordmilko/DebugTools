#pragma once

#include <stack>
#include "CClassInfo.h"

class CExceptionInfo;

enum class ExceptionState
{
    None,
    EnterCatch,
    EnterFinally
};

class CExceptionInfo
{
public:
    CExceptionInfo(CClassInfo* pClassInfo, long sequence)
    {
        pClassInfo->AddRef();
        m_pClassInfo = pClassInfo;
        m_Sequence = sequence;
        m_UnmanagedDepth = 0;
        m_FilterInvokerFunctionId = 0;
        m_UnhandledFilterExceptionComplete = FALSE;
        m_IsInFilter = FALSE;
    }

    ~CExceptionInfo()
    {
        if (m_pClassInfo)
            m_pClassInfo->Release();
    }

    void PushFrame(FunctionID functionId)
    {
        m_FrameStack.push(functionId);
    }

    FunctionIDOrClientID PopFrame()
    {
        FunctionID functionId = m_FrameStack.top();
        m_FrameStack.pop();

        FunctionIDOrClientID funcId{ functionId };
        return funcId;
    }

    long m_UnmanagedDepth;

    CClassInfo* m_pClassInfo;
    long m_Sequence;
    ExceptionState m_ExceptionState;

    /// <summary>
    /// Gets or sets the function that invokes an exception filter. This is the function with the candidate
    /// catch clause that is being evaluated.
    /// </summary>
    FunctionID m_FilterInvokerFunctionId;
    BOOL m_UnhandledFilterExceptionComplete;

    FunctionID m_CatcherFunctionId;
    size_t m_ClauseCallDepth;

    //todo: do we need to record these for the finally as well?
    UINT_PTR m_ClauseFramePointer;
    UINT_PTR m_ClauseProgramCounter;


    BOOL m_IsInFilter;

private:    
    std::stack<FunctionID> m_FrameStack;
};