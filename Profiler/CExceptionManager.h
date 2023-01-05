#pragma once

#include <deque>

class CExceptionInfo;

extern thread_local std::deque<CExceptionInfo*> g_ExceptionQueue;

enum class ExceptionCompletedReason
{
    Caught = 1,
    UnmanagedCaught,
    Superseded
};

class CExceptionManager
{
public:
    HRESULT UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason);
    HRESULT ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason);

    HRESULT ExceptionThrown(ObjectID thrownObjectId);

    HRESULT UnwindFunctionEnter(FunctionID functionId);
    HRESULT UnwindFunctionLeave();

    HRESULT UnwindFinallyEnter(FunctionID functionId);
    HRESULT UnwindFinallyLeave();

    HRESULT CatcherEnter(FunctionID functionId, ObjectID objectId);
    HRESULT CatcherLeave();

private:
    void EnterUnmanaged(CExceptionInfo* pExceptionInfo);
    void LeaveUnmanaged(CExceptionInfo* pExceptionInfo);
    void ClearLastException(CExceptionInfo* pExceptionInfo, ExceptionCompletedReason reason);
    void ClearFirstException(CExceptionInfo* pExceptionInfo);

    CExceptionInfo* GetCurrentException()
    {
        return g_ExceptionQueue.back();
    }
};