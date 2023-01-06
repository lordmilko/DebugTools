#pragma once

#include <deque>

class CExceptionInfo;

extern thread_local std::deque<CExceptionInfo*> g_ExceptionQueue;

enum class ExceptionCompletedReason
{
    Caught = 1,
    UnmanagedCaught,
    Superseded,
    UnhandledInFilter
};

class CExceptionManager
{
public:
    HRESULT UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason);
    HRESULT ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason);

    HRESULT ExceptionThrown(ObjectID thrownObjectId);

    HRESULT SearchFilterEnter(FunctionID functionId);
    HRESULT SearchFilterLeave();

    HRESULT UnwindFunctionEnter(FunctionID functionId);
    HRESULT UnwindFunctionLeave();

    HRESULT UnwindFinallyEnter(FunctionID functionId);
    HRESULT UnwindFinallyLeave();

    HRESULT CatcherEnter(FunctionID functionId, ObjectID objectId);
    HRESULT CatcherLeave();

    static void ClearStaleExceptions();

private:
    static void ClearStaleException(CExceptionInfo* pExceptionInfo);
    
    void EnterUnmanaged(CExceptionInfo* pExceptionInfo);
    void LeaveUnmanaged(CExceptionInfo* pExceptionInfo);

    void ClearLastException(CExceptionInfo* pExceptionInfo, ExceptionCompletedReason reason);

    CExceptionInfo* GetCurrentException()
    {
        return g_ExceptionQueue.back();
    }
};