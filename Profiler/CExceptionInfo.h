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

private:    
    std::stack<FunctionID> m_FrameStack;
};