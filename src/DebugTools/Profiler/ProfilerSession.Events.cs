using System;
using ClrDebug;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    partial class ProfilerSession
    {
        private void SetEventHandlers()
        {
            Reader.MethodInfo += Parser_MethodInfo;
            Reader.MethodInfoDetailed += Parser_MethodInfoDetailed;

            Reader.ModuleLoaded += Parser_ModuleLoaded;

            Reader.CallEnter += Parser_CallEnter;
            Reader.CallLeave += Parser_CallLeave;
            Reader.Tailcall += Parser_Tailcall;

            Reader.CallEnterDetailed += Parser_CallEnterDetailed;
            Reader.CallLeaveDetailed += Parser_CallLeaveDetailed;
            Reader.TailcallDetailed += Parser_TailcallDetailed;

            Reader.ManagedToUnmanaged += args => Parser_UnmanagedTransition(args, FrameKind.M2U);
            Reader.UnmanagedToManaged += args => Parser_UnmanagedTransition(args, FrameKind.U2M);

            Reader.Exception += Parser_Exception;
            Reader.ExceptionFrameUnwind += Parser_ExceptionFrameUnwind;
            Reader.ExceptionCompleted += Parser_ExceptionCompleted;

            Reader.StaticFieldValue += Parser_StaticFieldValue;

            Reader.ThreadCreate += Parser_ThreadCreate;
            Reader.ThreadDestroy += Parser_ThreadDestroy;
            Reader.ThreadName += Parser_ThreadName;

            Reader.Shutdown += v =>
            {
                //If we're monitoring sessions globally, we don't care if a given process exits, we want to keep
                //watching for the next process
                if (!global)
                {
                    //Calling Dispose() guarantees an immediate stop
                    Reader.Stop();

                    traceCTS?.Cancel();
                    userCTS?.Cancel();
                }
            };
        }

        #region Thread

        private void Parser_ThreadCreate(ThreadArgs v)
        {
            threadIdToSequenceMap[v.ThreadId] = v.ThreadSequence;
            threadSequenceToIdMap[v.ThreadSequence] = v.ThreadId;
        }

        private void Parser_ThreadDestroy(ThreadArgs v)
        {
            threadIdToSequenceMap.Remove(v.ThreadId);
        }

        private void Parser_ThreadName(ThreadNameArgs v)
        {
            threadNames[v.ThreadSequence] = v.ThreadName;

            if (threadSequenceToIdMap.TryGetValue(v.ThreadSequence, out var threadId) && ThreadCache.TryGetValue(threadId, out var stack))
                stack.Root.ThreadName = v.ThreadName;
        }

        #endregion
        #region MethodInfo

        private void Parser_MethodInfo(MethodInfoArgs v)
        {
            var wasUnknown = Methods.TryGetValue(v.FunctionID, out var existing) && existing is UnknownMethodInfo;

            //If the function was unknown previously (which can occur in weird double unmanaged to managed transitions, such as with
            //Visual Studio's VsAppDomainManager.OnStart()) we'll overwrite it, and be sure to check against the previous instance by looking at the function id,
            //not the object reference
            Methods[v.FunctionID] = new MethodInfo(new FunctionID(new IntPtr(v.FunctionID)), v.ModuleName, v.TypeName, v.MethodName)
            {
                WasUnknown = wasUnknown
            };
        }

        private void Parser_MethodInfoDetailed(MethodInfoDetailedArgs v)
        {
            var wasUnknown = Methods.TryGetValue(v.FunctionID, out var existing) && existing is UnknownMethodInfo;

            //If the function was unknown previously (which can occur in weird double unmanaged to managed transitions, such as with
            //Visual Studio's VsAppDomainManager.OnStart()) we'll overwrite it, and be sure to check against the previous instance by looking at the function id,
            //not the object reference
            Methods[v.FunctionID] = new MethodInfoDetailed(new FunctionID(new IntPtr(v.FunctionID)), v.ModuleName, v.TypeName, v.MethodName, v.Token)
            {
                WasUnknown = wasUnknown
            };
        }

        #endregion

        private void Parser_ModuleLoaded(ModuleLoadedArgs args)
        {
            Modules[args.UniqueModuleID] = new ModuleInfo(args.UniqueModuleID, args.Path);
        }

        #region CallArgs

        private void Parser_CallEnter(CallArgs args) =>
            CallEnterCommon(args, (t, v, m) => t.Enter(v, m));

        private void Parser_CallLeave(CallArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.Leave(v, m));

        private void Parser_Tailcall(CallArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.Tailcall(v, m));

        private void CallEnterCommon<T>(T args, Func<ThreadStack, T, IMethodInfoInternal, IFrame> addMethod, bool ignoreUnknown = false) where T : ICallArgs
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                Validate(args);

                bool setName = false;

                var method = GetMethodSafe(args.FunctionID);

                if (!ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                {
                    if (ignoreUnknown && method.WasUnknown && !includeUnknownTransitions)
                        return;

                    threadStack = new ThreadStack(includeUnknownTransitions, args.ThreadID);
                    ThreadCache[args.ThreadID] = threadStack;

                    setName = true;
                }

                var frame = addMethod(threadStack, args, method);

                if (frame != null)
                {
                    var local = watchQueue;
                    local?.Add(frame);
                }

                if (setName)
                {
                    if (threadIdToSequenceMap.TryGetValue(args.ThreadID, out var threadSequence) && threadNames.TryGetValue(threadSequence, out var name))
                        threadStack.Root.ThreadName = name;
                }
            }
        }

        private void CallLeaveCommon<T>(T args, Action<ThreadStack, T, IMethodInfoInternal> leaveMethod, bool validateHR = true) where T : ICallArgs
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                if (validateHR)
                    Validate(args);

                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                {
                    var method = GetMethodSafe(args.FunctionID);

                    leaveMethod(threadStack, args, method);
                }
            }
        }

        #endregion
        #region CallDetailedArgs

        private void Parser_CallEnterDetailed(CallDetailedArgs args) =>
            CallEnterCommon(args, (t, v, m) => t.EnterDetailed(v, m));

        private void Parser_CallLeaveDetailed(CallDetailedArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.LeaveDetailed(v, m));

        private void Parser_TailcallDetailed(CallDetailedArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.TailcallDetailed(v, m));

        private void Validate(ICallArgs args)
        {
            if ((uint)args.HRESULT == (uint)PROFILER_HRESULT.PROFILER_E_UNKNOWN_FRAME)
                throw new ProfilerException("Profiler encountered an unexpected function while processing a Leave/Tailcall. Current stack frame is unknown, profiler cannot continue.", (PROFILER_HRESULT)args.HRESULT);

            if (args.HRESULT.IsProfilerHRESULT())
            {
                switch ((PROFILER_HRESULT)args.HRESULT)
                {
                    case PROFILER_HRESULT.PROFILER_E_BUFFERFULL:
                    case PROFILER_HRESULT.PROFILER_E_GENERICCLASSID:
                    case PROFILER_HRESULT.PROFILER_E_UNKNOWN_GENERIC_ARRAY:
                    case PROFILER_HRESULT.PROFILER_E_NO_CLASSID:
                        break;

                    default:
                        throw new ProfilerException((PROFILER_HRESULT)(uint)args.HRESULT);
                }
            }
            else
            {
                switch (args.HRESULT)
                {
                    case HRESULT.CORPROF_E_CLASSID_IS_COMPOSITE:
                    case HRESULT.COR_E_TYPELOAD:
                        break;

                    default:
                        args.HRESULT.ThrowOnNotOK();
                        break;

                }
            }
        }

        #endregion
        #region Unmanaged

        private void Parser_UnmanagedTransition(UnmanagedTransitionArgs args, FrameKind kind)
        {
            if (args.Reason == COR_PRF_TRANSITION_REASON.COR_PRF_TRANSITION_CALL)
                CallEnterCommon(args, (t, v, m) => t.EnterUnmanagedTransition(v, m, kind), true);
            else
                CallLeaveCommon(args, (t, v, m) => t.LeaveUnmanagedTransition(v, m));
        }

        #endregion
        #region Exception

        private void Parser_Exception(ExceptionArgs args)
        {
            if (collectStackTrace)
            {
                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                    threadStack.Exception(args);
            }
        }

        private void Parser_ExceptionFrameUnwind(CallArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.ExceptionFrameUnwind(v, m), false);

        private void Parser_ExceptionCompleted(ExceptionCompletedArgs v)
        {
            if (collectStackTrace)
            {
                if (ThreadCache.TryGetValue(v.ThreadID, out var threadStack))
                    threadStack.ExceptionCompleted(v);
            }
        }

        #endregion

        public void Parser_StaticFieldValue(StaticFieldValueArgs args)
        {
            if (args.HRESULT == HRESULT.S_OK)
                staticFieldValue = ValueSerializer.FromReturnValue(args.Value);
            else
                staticFieldValue = args.HRESULT;

            staticFieldValueEvent.Set();
        }
    }
}
