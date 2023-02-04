#include "pch.h"
#include "CExceptionManager.h"
#include "CCorProfilerCallback.h"
#include "CValueTracer.h"
#include "DebugToolsProfiler.h"
#include "CExceptionInfo.h"

thread_local std::deque<CExceptionInfo*> g_ExceptionQueue;

thread_local long g_ExceptionSequence;

thread_local ULONG g_FilterCallDepth = 0;

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

    IfFailGo(g_pProfiler->GetClassInfoFromClassId(classId, (IClassInfo**)&pClassInfo));

    g_ExceptionSequence++;

    pExceptionInfo = new CExceptionInfo(pClassInfo, g_ExceptionSequence);

    if (g_FilterCallDepth > 0)
        pExceptionInfo->m_IsInFilter = TRUE;

    LogException(L"ExceptionThrown %s\n", pClassInfo->m_szName);

    g_ExceptionQueue.push_back(pExceptionInfo);

    ValidateETW(EventWriteExceptionEvent(g_ExceptionSequence, pClassInfo->m_szName));

ErrExit:
    return hr;
}

HRESULT CExceptionManager::SearchFilterEnter(FunctionID functionId)
{
    CExceptionInfo* pExceptionInfo = GetCurrentException();

    //Set the function that will call the filter. This is NOT the FunctionID of the filter itself
    pExceptionInfo->m_FilterInvokerFunctionId = functionId;

    LogException(L"FilterEnter %s " FORMAT_PTR "\n", pExceptionInfo->m_pClassInfo->m_szName, functionId);

    g_FilterCallDepth++;

    return S_OK;
}

HRESULT CExceptionManager::SearchFilterLeave()
{
    CExceptionInfo* pExceptionInfo = GetCurrentException();

    LogException(L"FilterLeave %s\n", pExceptionInfo->m_pClassInfo->m_szName);
    g_FilterCallDepth--;

    return S_OK;
}

HRESULT CExceptionManager::UnwindFunctionEnter(FunctionID functionId)
{
    HRESULT hr = S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    if (pExceptionInfo->m_IsInFilter && g_ExceptionQueue.size() > 1)
    {
        /* When an UnwindFunctionLeave event occurs, this indicates an unwind of an "exception handler scope".
         * If an unhandled exception occurs in an exception filter, the exception will be swallowed and the filter
         * will be considered to have returned false. However, an exception that occurs directly within a filter
         * function will generate TWO pairs of UnwindFunctionEnter/Leave pairs. Once for the filter itself exiting,
         * and another for the "exception handler scope" of "when (Filter())" ending. We do not want to treat this
         * second unwind event as an unwind of an actual method frame - there was no Enter event associated with it
         * that should be unwound; as such, we record the FunctionID of the function that invoked the filter initially.
         * If an unhandled exception has occurred in a filter and we've rewound to the method that initially invoked
         * that filter, the filter is over, and we can know not to actually unwind any additional frames.
         * An issue that may be related is described at https://github.com/dotnet/runtime/issues/10871 */

        //A queue's indexer is relative to the start like a normal list
        CExceptionInfo* pPreviousException = g_ExceptionQueue[g_ExceptionQueue.size() - 2];

        if (pPreviousException->m_FilterInvokerFunctionId == functionId)
        {
            //An exception occurred in a filter and the filter has now unwound to the "exception handler scope"
            //of "when (Filter())". Flag the exception as being an unhandled filter exception so that it is properly
            //disposed (and does not actually unwind any frames) in UnwindFrameLeave(). Another UnwindFunctionEnter
            //event for the m_FilterInvokerFunctionId will occur after SearchFilterLeave
            pExceptionInfo->m_UnhandledFilterExceptionComplete = TRUE;
        }
    }

    LogException(L"UnwindFunctionEnter %s " FORMAT_PTR "\n", pExceptionInfo->m_pClassInfo->m_szName, functionId);

    pExceptionInfo->PushFrame(functionId);

    return hr;
}

HRESULT CExceptionManager::UnwindFunctionLeave()
{
    HRESULT hr = S_OK;
    BOOL unwindFrame = FALSE;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    FunctionIDOrClientID functionId = pExceptionInfo->PopFrame();

    if (pExceptionInfo->m_UnhandledFilterExceptionComplete)
    {
        LogException(L"UnwindFunctionLeave: deleting unhandled filter exception %s", pExceptionInfo->m_pClassInfo->m_szName);

        ClearLastException(pExceptionInfo, ExceptionCompletedReason::UnhandledInFilter);
    }
    else
    {
        if (g_pProfiler->IsHookedFunction(functionId))
        {
            unwindFrame = TRUE;
            LogException(L"UnwindFunctionLeave %s: Unwinding shadow stack frame " FORMAT_PTR "\n", pExceptionInfo->m_pClassInfo->m_szName, functionId.functionID);

            //This increments g_Sequence so our profiler controller will explode if we don't also provide an ETW notification
            LEAVE_FUNCTION(functionId);
        }
        else
        {
            LogException(L"UnwindFunctionLeave %s: Not unwinding shadow stack frame " FORMAT_PTR " as it was not hooked\n", pExceptionInfo->m_pClassInfo->m_szName, functionId.functionID);
        }
    }

    ClearStaleExceptions();

ErrExit:
    if (unwindFrame)
        ValidateETW(EventWriteExceptionFrameUnwindEvent(functionId.functionID, g_Sequence, hr)); //todo: we should instead have an exceptionunwind event

    return hr;
}

HRESULT CExceptionManager::UnwindFinallyEnter(FunctionID functionId)
{
    HRESULT hr = S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    COR_PRF_EX_CLAUSE_INFO clauseInfo;
    IfFailGo(g_pProfiler->m_pInfo->GetNotifiedExceptionClauseInfo(&clauseInfo));

    _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::None);

    LogException(L"ExceptionUnwindFinallyEnter: %s None -> EnterFinally\n", pExceptionInfo->m_pClassInfo->m_szName);

    pExceptionInfo->m_ExceptionState = ExceptionState::EnterFinally;
    pExceptionInfo->m_ClauseCallDepth = g_CallStack.size();
    pExceptionInfo->m_ClauseFramePointer = clauseInfo.framePointer;
    pExceptionInfo->m_ClauseProgramCounter = clauseInfo.programCounter;

ErrExit:
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
    HRESULT hr = S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    COR_PRF_EX_CLAUSE_INFO clauseInfo;
    IfFailGo(g_pProfiler->m_pInfo->GetNotifiedExceptionClauseInfo(&clauseInfo));

    _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::None);

    LogException(
        L"ExceptionCatcherEnter: %s None -> EnterCatch (EIP " FORMAT_PTR ", EBP " FORMAT_PTR ")\n",
        pExceptionInfo->m_pClassInfo->m_szName,
        clauseInfo.programCounter,
        clauseInfo.framePointer
    );

    pExceptionInfo->m_ExceptionState = ExceptionState::EnterCatch;
    pExceptionInfo->m_ClauseCallDepth = g_CallStack.size();
    pExceptionInfo->m_ClauseFramePointer = clauseInfo.framePointer;
    pExceptionInfo->m_ClauseProgramCounter = clauseInfo.programCounter;

ErrExit:
    return hr;
}

HRESULT CExceptionManager::CatcherLeave()
{
    HRESULT hr = S_OK;

    CExceptionInfo* pExceptionInfo = GetCurrentException();

    COR_PRF_EX_CLAUSE_INFO clauseInfo;
    IfFailGo(g_pProfiler->m_pInfo->GetNotifiedExceptionClauseInfo(&clauseInfo));

    /* Ostensibly, any time an exception is thrown, it should hit CatcherEnter before CatcherLeave. However, a case was observed in Visual Studio during startup wherein msenv!VsCoCreateAggregatedManagedObject tries to create a COM object,
     * Activator.CreateInstance() is called to create it, a FileNotFoundException is thrown, and yet CatcherEnter never occurred. It appears this may have something to do with the fact that the exception is processed on a helper frame, with
     * UnwindAndContinueRethrowHelperAfterCatch() being called to dispatch the exception. This function is used in the INSTALL_UNWIND_AND_CONTINUE_HANDLER_FOR_HMF macro, which in turn appears to exclusively be used in a variety of HELPER_METHOD_FRAME_BEGIN_* fcall macros.
     * After unwinding has finished, execution jumps straight to the catch block in a DomainNeutralILStubClass.IL_STUB_COMtoCLR, which calls JIT_EndCatch with no "start catch" event
     * 
     * Observe that the address KERNELBASE!RaiseException jumps is to is *22b, which is the same address as the start of the EHHandler Handler (catch) clause. Setting a breakpoint at the start of the try clause (bp 049dc15a) and rewinding, we can see that AppDomain.CreateInstance() was called
     * for some component of ReSharper.
     * 
     *     0:048> r eip
     *     eip=049dc22b
     * 
     *     0:048> !EHInfo 0x49dc22b
     *     Method Name:  DomainNeutralILStubClass.IL_STUB_COMtoCLR(IntPtr, IntPtr, IntPtr)
     *
     *     EHHandler 0: FINALLY 
     *     Clause:  [049dc15f, 049dc1ed] [17, a5]
     *     Handler: [049dc1ed, 049dc222] [a5, da]
     *
     *     EHHandler 1: TYPED catch(System.Threading.ExecutionContext.Run(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object)) 
     *     Clause:  [049dc15a, 049dc22b] [12, e3]
     *     Handler: [049dc22b, 049dc23a] [e3, f2]
     */
    if (pExceptionInfo->m_ExceptionState == ExceptionState::None)
    {
        LogException(L"ExceptionCatcherLeave: CatcherLeave called without previous CatcherEnter. Possible direct JIT_EndCatch in IL_STUB_COMtoCLR\n");
    }
    else
    {
        _ASSERTE(pExceptionInfo->m_ExceptionState == ExceptionState::EnterCatch);

        LogException(
            L"ExceptionCatcherLeave: %s EnterCatch -> None (EIP " FORMAT_PTR ", EBP " FORMAT_PTR ")\n",
            pExceptionInfo->m_pClassInfo->m_szName,
            clauseInfo.programCounter,
            clauseInfo.framePointer
        );
    }

    pExceptionInfo->m_ExceptionState = ExceptionState::None;

    ClearLastException(pExceptionInfo, ExceptionCompletedReason::Caught);

    ClearStaleExceptions();

ErrExit:
    return hr;
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

        //We've just gracefully stepped out of unmanaged code. Any exceptions that were present have been handled
        ClearStaleExceptions();
    }
}

void CExceptionManager::ClearStaleExceptions()
{
    while (!g_ExceptionQueue.empty())
    {
        CExceptionInfo* pFirstException = g_ExceptionQueue.back();

        if (pFirstException->m_ExceptionState == ExceptionState::EnterCatch || pFirstException->m_ExceptionState == ExceptionState::EnterFinally)
        {
            //If we've returned at least 1 frame from this previous exception, its EnterCatch or EnterFinally will never complete. The exception must have been
            //interrupted by an exception that was thrown in its catch/finally clause.
            if (pFirstException->m_ClauseCallDepth > g_CallStack.size())
            {
                ClearStaleException(pFirstException);
                pFirstException = nullptr;
            }
            else
                break;
        }
        else
            break;
    }
}

void CExceptionManager::ClearLastException(CExceptionInfo* pExceptionInfo, ExceptionCompletedReason reason)
{
    EventWriteExceptionCompletedEvent(pExceptionInfo->m_Sequence, (int) reason);

    if (reason != ExceptionCompletedReason::UnhandledInFilter)
        LogException(L"Exception %s has been handled. Clearing exception\n", pExceptionInfo->m_pClassInfo->m_szName);

    _ASSERTE(g_ExceptionQueue.back() == pExceptionInfo);
    g_ExceptionQueue.pop_back();
    delete pExceptionInfo;

    LogException(L"Exceptions remaining: %d\n", g_ExceptionQueue.size());
}

void CExceptionManager::ClearStaleException(CExceptionInfo* pExceptionInfo)
{
    if (pExceptionInfo->m_ExceptionState == ExceptionState::EnterCatch)
        LogException(L"UnwindFunctionLeave: Exception %s in queue is status EnterCatch and will never complete. Clearing exception\n", pExceptionInfo->m_pClassInfo->m_szName);
    else
        LogException(L"UnwindFunctionLeave: Exception %s in queue is status EnterFinally and will never complete. Clearing exception\n", pExceptionInfo->m_pClassInfo->m_szName);

    EventWriteExceptionCompletedEvent(pExceptionInfo->m_Sequence, (int) ExceptionCompletedReason::Superseded);

    _ASSERTE(g_ExceptionQueue.back() == pExceptionInfo);
    g_ExceptionQueue.pop_back();
    delete pExceptionInfo;

    LogException(L"Exceptions remaining: %d\n", g_ExceptionQueue.size());
}