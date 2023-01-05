#include "pch.h"
#include "CExceptionManager.h"
#include "CCorProfilerCallback.h"
#include "CValueTracer.h"
#include "DebugToolsProfiler.h"
#include "CExceptionInfo.h"

thread_local std::deque<CExceptionInfo*> g_ExceptionQueue;

thread_local long g_ExceptionSequence;

HRESULT CExceptionManager::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    if (g_ExceptionQueue.empty())
        return S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    _ASSERTE(reason == COR_PRF_TRANSITION_CALL || reason == COR_PRF_TRANSITION_RETURN);

    LogException(L"UnmanagedToManagedTransition %s (Reason: %s)\n", pExceptionInfo->m_pClassInfo->m_szName, reason == COR_PRF_TRANSITION_CALL ? L"Call" : L"Return");

    if (reason == COR_PRF_TRANSITION_CALL)
    {
        //Something unmanaged has called something managed. Maybe the catch/finally invoked something that caused
        //managed(1) -> unmanaged(1) -> managed(2). As far as our bookkeeping goes, this is not relevant to us
    }
    else //Return
    {
        /* If something called managed(1)->unmanaged(1), we're unmanaged (1) now returning back to managed(1).
         * Otherwise, we're unmanaged(0) which gracefully handled the exception caused by managed(1) and so the exception
         * has been completed */
        LeaveUnmanaged(pExceptionInfo);
    }

    return S_OK;
}

HRESULT CExceptionManager::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    if (g_ExceptionQueue.empty())
        return S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    _ASSERTE(reason == COR_PRF_TRANSITION_CALL || reason == COR_PRF_TRANSITION_RETURN);

    LogException(L"ManagedToUnmanagedTransition %s (Reason: %s)\n", pExceptionInfo->m_pClassInfo->m_szName, reason == COR_PRF_TRANSITION_CALL ? L"Call" : L"Return");

    if (reason == COR_PRF_TRANSITION_CALL)
    {
        /* Something managed has called something unmanaged.Maybe the catch / finally invoked something that caused
         * managed(1) -> unmanaged(1). We'll record this fact, so we can pair the inevitable UnmanagedToManaged(Return) up with this call event.
         * If a UnmanagedToManaged(Return) event occurs without an initial call, this means we've returned to unmanaged(0) which called managed(1)
         * and has gracefully handled the exception (we'll fly straight through the unmanaged code without transition notifications if the unmanaged code
         * doesn't handle the exception) */
        EnterUnmanaged(pExceptionInfo);
    }
    else //Return
    {
        //Maybe the catch/finally invoked something that caused managed(1) -> unmanaged(1) -> managed(2), and managed(2) is now returning
        //to unmanaged(1). As far as our bookkeeping goes, this is not relevant to us
    }

    return S_OK;
}

HRESULT CExceptionManager::ExceptionThrown(ObjectID thrownObjectId)
{
    HRESULT hr = S_OK;
    ClassID classId;
    CClassInfo* pClassInfo;
    CExceptionInfo* pExceptionInfo;

    _ASSERTE(thrownObjectId != NULL);

    IfFailGo(g_pProfiler->m_pInfo->GetClassFromObject(thrownObjectId, &classId));

    IfFailGo(g_pProfiler->GetClassInfo(classId, (IClassInfo**)&pClassInfo));

    g_ExceptionSequence++;

    pExceptionInfo = new CExceptionInfo(pClassInfo, g_ExceptionSequence);

    LogException(L"ExceptionThrown %s\n", pClassInfo->m_szName);

    g_ExceptionQueue.push_back(pExceptionInfo);

    ValidateETW(EventWriteExceptionEvent(g_ExceptionSequence, pClassInfo->m_szName));

ErrExit:
    return hr;
}

HRESULT CExceptionManager::UnwindFunctionEnter(FunctionID functionId)
{
    HRESULT hr = S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    pExceptionInfo->PushFrame(functionId);

    return hr;
}

HRESULT CExceptionManager::UnwindFunctionLeave()
{
    HRESULT hr = S_OK;
    BOOL unwindFrame = FALSE;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    if (g_ExceptionQueue.size() > 1)
    {
        while (true)
        {
            CExceptionInfo* pFirstException = g_ExceptionQueue.front();

            if (pFirstException->m_ExceptionState == ExceptionState::EnterCatch || pFirstException->m_ExceptionState == ExceptionState::EnterFinally)
            {
                if (pFirstException->m_ExceptionState == ExceptionState::EnterCatch)
                    LogException(L"UnwindFunctionLeave: Exception %s in queue is status EnterCatch and will never complete. Clearing exception\n", pFirstException->m_pClassInfo->m_szName);
                else
                    LogException(L"UnwindFunctionLeave: Exception %s in queue is status EnterFinally and will never complete. Clearing exception\n", pFirstException->m_pClassInfo->m_szName);

                ClearFirstException(pFirstException);
                pFirstException = nullptr;
            }
            else
                break;
        }
    }

    FunctionIDOrClientID functionId = pExceptionInfo->PopFrame();

    if (g_pProfiler->IsHookedFunction(functionId))
    {
        unwindFrame = TRUE;
        LogException(L"UnwindFunctionLeave %s: Unwinding shadow stack frame %llX\n", pExceptionInfo->m_pClassInfo->m_szName, functionId.functionID);

        //This increments g_Sequence so our profiler controller will explode if we don't also provide an ETW notification
        LEAVE_FUNCTION(functionId);
    }
    else
    {
        LogException(L"UnwindFunctionLeave %s: Not unwinding shadow stack frame %llX as it was not hooked\n", pExceptionInfo->m_pClassInfo->m_szName, functionId.functionID);
    }

ErrExit:
    if (unwindFrame)
        ValidateETW(EventWriteExceptionFrameUnwindEvent(functionId.functionID, g_Sequence, hr)); //todo: we should instead have an exceptionunwind event

    return hr;
}

HRESULT CExceptionManager::UnwindFinallyEnter(FunctionID functionId)
{
    CExceptionInfo* pExceptionInfo = GetCurrentException();

    _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::None);

    LogException(L"ExceptionUnwindFinallyEnter: %s None -> EnterFinally\n", pExceptionInfo->m_pClassInfo->m_szName);

    pExceptionInfo->m_ExceptionState = ExceptionState::EnterFinally;

    return S_OK;
}

HRESULT CExceptionManager::UnwindFinallyLeave()
{
    CExceptionInfo* pExceptionInfo = GetCurrentException();

    _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::EnterFinally);

    LogException(L"ExceptionUnwindFinallyLeave: %s EnterFinally -> None\n", pExceptionInfo->m_pClassInfo->m_szName);

    pExceptionInfo->m_ExceptionState = ExceptionState::None;

    return S_OK;
}

HRESULT CExceptionManager::CatcherEnter(FunctionID functionId, ObjectID objectId)
{
    CExceptionInfo* pExceptionInfo = GetCurrentException();

    _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::None);

    LogException(L"ExceptionCatcherEnter: %s None -> EnterCatch\n", pExceptionInfo->m_pClassInfo->m_szName);

    pExceptionInfo->m_ExceptionState = ExceptionState::EnterCatch;

    return S_OK;
}

HRESULT CExceptionManager::CatcherLeave()
{
    CExceptionInfo* pExceptionInfo = GetCurrentException();

    _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::EnterCatch);

    LogException(L"ExceptionCatcherLeave: %s EnterCatch -> None\n", pExceptionInfo->m_pClassInfo->m_szName);

    pExceptionInfo->m_ExceptionState = ExceptionState::None;

    ClearLastException(pExceptionInfo, ExceptionCompletedReason::Caught);

    return S_OK;
}

void CExceptionManager::EnterUnmanaged(CExceptionInfo* pExceptionInfo)
{
    pExceptionInfo->m_UnmanagedDepth++;
}

void CExceptionManager::LeaveUnmanaged(CExceptionInfo* pExceptionInfo)
{
    pExceptionInfo->m_UnmanagedDepth--;

    if (pExceptionInfo->m_UnmanagedDepth < 0)
    {
        LogException(L"Exception %s handled by unmanaged code. Clearing exception\n", pExceptionInfo->m_pClassInfo->m_szName);
        ClearLastException(pExceptionInfo, ExceptionCompletedReason::UnmanagedCaught);
    }
}

void CExceptionManager::ClearLastException(CExceptionInfo* pExceptionInfo, ExceptionCompletedReason reason)
{
    EventWriteExceptionCompletedEvent(pExceptionInfo->m_Sequence, (int) reason);

    LogException(L"Exception %s has been handled. Clearing exception\n", pExceptionInfo->m_pClassInfo->m_szName);

    _ASSERTE(g_ExceptionQueue.back() == pExceptionInfo);
    g_ExceptionQueue.pop_back();
    delete pExceptionInfo;
}

void CExceptionManager::ClearFirstException(CExceptionInfo* pExceptionInfo)
{
    EventWriteExceptionCompletedEvent(pExceptionInfo->m_Sequence, (int) ExceptionCompletedReason::Superseded);

    _ASSERTE(g_ExceptionQueue.front() == pExceptionInfo);
    g_ExceptionQueue.pop_front();
    delete pExceptionInfo;
}