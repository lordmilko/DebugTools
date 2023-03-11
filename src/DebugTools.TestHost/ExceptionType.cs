using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace DebugTools.TestHost
{
    class ExceptionType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CaughtWithinMethod()
        {
            try
            {
                /* ExceptionThrown
                 * ExceptionUnwindFunctionEnter - CaughtWithinMethod
                 * ExceptionCatcherEnter
                 * ExceptionCatcherLeave
                 * Leave CaughtWithinMethod */
                throw new NotImplementedException("Error Message");
            }
            catch (Exception)
            {
            }
        }

        #region UnwindOneFrame

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnwindOneFrame1()
        {
            try
            {
                /* ExceptionThrown
                 * ExceptionUnwindFunctionEnter - UnwindOneFrame2
                 * ExceptionUnwindFunctionLeave - UnwindOneFrame2
                 * ExceptionUnwindFunctionEnter - UnwindOneFrame1
                 * ExceptionCatcherEnter
                 * ExceptionCatcherLeave
                 * Leave UnwindOneFrame1 */
                UnwindOneFrame2();
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UnwindOneFrame2()
        {
            throw new NotImplementedException("Error Message");
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_ThrownInCatchAndImmediatelyCaught()
        {
            try
            {
                /* ExceptionThrown                  - throw NotImplementedException
                 * ExceptionUnwindFunctionEnter     - consider whether to unwind
                 * ExceptionCatcherEnter            - catch (ImplementedException)
                 *     ExceptionThrown              - throw InvalidOperationException
                 *     ExceptionUnwindFunctionEnter - consider whether to unwind
                 *     ExceptionCatcherEnter        - catch (InvalidOperationException)
                 *     ExceptionCatcherLeave        - end inner catch
                 * ExceptionCatcherLeave            - end outer catch
                 * Leave Nested_ThrownInCatchAndImmediatelyCaught
                 */
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
                try
                {
                    throw new InvalidOperationException("Error Message 2");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_ThrownInCatchAndCaughtByOuterCatch()
        {
            try
            {
                try
                {
                    /* This sequence of events is the same as Nested_ThrownInCatchAndImmediatelyCaught except we never get a notification
                     * for the second ExceptionCatcherLeave, as it never runs. The CLR cleans up stale exceptions in ExceptionTracker::HandleNestedExceptionEscape(),
                     * however the EstablisherFrame (called MemoryStackFp) on the PEXCEPTION_ROUTINE (ProcessCLRException) specified to the DISPATCHER_CONTEXT is never
                     * passed to the profiler, so we can never determine whether the EstablisherFrame of the new exception supersedes the existing exception (it's not clear
                     * how these "frames" that are reported relate to actual stack frames). As such, on Leave/Tailcall we check whether there are any exceptions outstanding
                     * and complete them as superseded if we descend further than the frame depth their catch/finally was declared on. */

                    /* ExceptionThrown                  - throw NotImplementedException
                     * ExceptionUnwindFunctionEnter     - NotImplementedException
                     * ExceptionCatcherEnter            - NotImplementedException
                     *     ExceptionThrown              - throw InvalidOperationException
                     *     ExceptionUnwindFunctionEnter - InvalidOperationException
                     *     ExceptionCatcherEnter        - InvalidOperationException
                     *     ExceptionCatcherLeave        - InvalidOperationException
                     * Leave Nested_ThrownInCatchAndCaughtByOuterCatch
                     */
                    throw new NotImplementedException("Error Message 1");
                }
                catch (NotImplementedException)
                {
                    throw new InvalidOperationException("Error Message 2");
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        #region Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch1()
        {
            //Just because we're unwinding the frames of the inner exception, does not mean that the
            //outer exception has become unhandled.

            try
            {
                throw new NotImplementedException();
            }
            catch (NotImplementedException)
            {
                Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch2();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch2()
        {
            try
            {
                Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch3();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch3()
        {
            throw new InvalidOperationException();
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_CaughtByOuterCatch()
        {
            try
            {
                try
                {
                    /* ExceptionThrown
                     * ExceptionUnwindFunctionEnter
                     * ExceptionCatcherEnter         - NotImplementedException
                     * ExceptionCatcherLeave
                     * Leave Nested_CaughtByOuterCatch */
                    throw new NotImplementedException("Error Message");
                }
                catch (InvalidOperationException)
                {
                }
            }
            catch (NotImplementedException)
            {
            }
        }

        #region Nested_UnwindOneFrameFromThrowInCatch

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_UnwindOneFrameFromThrowInCatch1()
        {
            try
            {
                /* ExceptionThrown                  - throw NotImplementedException
                 * ExceptionUnwindFunctionEnter     - Nested_UnwindOneFrameFromThrowInCatch2
                 * ExceptionCatcherEnter            - catch (NotImplementedException)
                 *     ExceptionThrown              - throw InvalidOperationException
                 *     ExceptionUnwindFunctionEnter - Nested_UnwindOneFrameFromThrowInCatch2
                 *     ExceptionUnwindFunctionLeave - Nested_UnwindOneFrameFromThrowInCatch2
                 * ExceptionUnwindFunctionEnter     - Nested_UnwindOneFrameFromThrowInCatch1
                 * ExceptionCatcherEnter            - catch (InvalidOperationException)
                 * ExceptionCatcherLeave            - catch (InvalidOperationException)
                 * Leave Nested_UnwindOneFrameFromThrowInCatch1
                 */
                Nested_UnwindOneFrameFromThrowInCatch2();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_UnwindOneFrameFromThrowInCatch2()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
                throw new InvalidOperationException("Error Message 2");
            }
        }

        #endregion
        #region Nested_UnwindTwoFramesFromThrowInCatch

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_UnwindTwoFramesFromThrowInCatch1()
        {
            try
            {
                /* ExceptionThrown                  - throw NotImplementedException
                 * ExceptionUnwindFunctionEnter     - Nested_UnwindTwoFramesFromThrowInCatch3
                 * ExceptionCatcherEnter            - catch (NotImplementedException)
                 *     ExceptionThrown              - throw InvalidOperationException
                 *     ExceptionUnwindFunctionEnter - Nested_UnwindTwoFramesFromThrowInCatch3
                 *     ExceptionUnwindFunctionLeave - Nested_UnwindTwoFramesFromThrowInCatch3
                 * ExceptionUnwindFunctionEnter     - Nested_UnwindTwoFramesFromThrowInCatch2
                 * ExceptionUnwindFunctionLeave     - Nested_UnwindTwoFramesFromThrowInCatch2
                 * ExceptionUnwindFunctionEnter     - Nested_UnwindTwoFramesFromThrowInCatch1
                 * ExceptionCatcherEnter            - catch (InvalidOperationException)
                 * ExceptionCatcherLeave            - catch (InvalidOperationException)
                 * Leave Nested_UnwindTwoFramesFromThrowInCatch1
                 */
                Nested_UnwindTwoFramesFromThrowInCatch2();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_UnwindTwoFramesFromThrowInCatch2()
        {
            Nested_UnwindTwoFramesFromThrowInCatch3();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_UnwindTwoFramesFromThrowInCatch3()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
                throw new InvalidOperationException("Error Message 2");
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_ThrownInFinallyAndImmediatelyCaught()
        {
            try
            {
                //No notification is provided for finally even though it does execute

                /* ExceptionThrown                  - throw NotImplementedException
                 * ExceptionUnwindFunctionEnter     - Nested_ThrownInFinallyAndImmediatelyCaught
                 * ExceptionCatcherEnter            - catch (NotImplementedException)
                 * ExceptionCatcherLeave            - catch (NotImplementedException)
                 *
                 * <No Notification for Finally>
                 *
                 * ExceptionThrown                  - throw InvalidOperationException
                 * ExceptionUnwindFunctionEnter     - Nested_ThrownInFinallyAndImmediatelyCaught
                 * ExceptionCatcherEnter            - catch (InvalidOperationException)
                 * ExceptionCatcherLeave            - catch (InvalidOperationException)
                 * Leave Nested_ThrownInFinallyAndImmediatelyCaught
                 */
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
            }
            finally
            {
                //This generates unmanaged/managed transition events, so it should be commented out when verifying the events that occur
                Debug.WriteLine($"In {nameof(Nested_ThrownInFinallyAndImmediatelyCaught)} finally");

                try
                {
                    throw new InvalidOperationException("Error Message 2");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        #region Nested_ThrownInFinallyAndUnwindOneFrame

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_ThrownInFinallyAndUnwindOneFrame1()
        {
            try
            {
                //No notification is provided for finally even though it does execute

                /* ExceptionThrown                  - throw NotImplementedException
                 * ExceptionUnwindFunctionEnter     - Nested_ThrownInFinallyAndUnwindOneFrame2
                 * ExceptionCatcherEnter            - catch (NotImplementedException)
                 * ExceptionCatcherLeave            - catch (NotImplementedException)
                 *
                 * <No Notification for Finally>
                 *
                 * ExceptionThrown                  - throw InvalidOperationException
                 * ExceptionUnwindFunctionEnter     - Nested_ThrownInFinallyAndUnwindOneFrame2
                 * ExceptionUnwindFunctionLeave     - Nested_ThrownInFinallyAndUnwindOneFrame2
                 * ExceptionUnwindFunctionEnter     - Nested_ThrownInFinallyAndUnwindOneFrame1
                 * ExceptionCatcherEnter            - catch (InvalidOperationException)
                 * ExceptionCatcherLeave            - catch (InvalidOperationException)
                 * Leave Nested_ThrownInFinallyAndUnwindOneFrame1
                 */
                Nested_ThrownInFinallyAndUnwindOneFrame2();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_ThrownInFinallyAndUnwindOneFrame2()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
            }
            finally
            {
                //This generates unmanaged/managed transition events, so it should be commented out when verifying the events that occur
                Debug.WriteLine($"In {nameof(Nested_ThrownInFinallyAndUnwindOneFrame2)} finally");

                throw new InvalidOperationException("Error Message 2");
            }
        }

        #endregion
        #region Nested_ThrownInFinallyAndUnwindTwoFrames

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Nested_ThrownInFinallyAndUnwindTwoFrames1()
        {
            try
            {
                Nested_ThrownInFinallyAndUnwindTwoFrames2();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_ThrownInFinallyAndUnwindTwoFrames2()
        {
            Nested_ThrownInFinallyAndUnwindTwoFrames3();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Nested_ThrownInFinallyAndUnwindTwoFrames3()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
            }
            finally
            {
                //This generates unmanaged/managed transition events, so it should be commented out when verifying the events that occur
                Debug.WriteLine($"In {nameof(Nested_ThrownInFinallyAndUnwindOneFrame2)} finally");

                throw new InvalidOperationException("Error Message 2");
            }
        }

        #endregion
        #region NoCatchThrowWithFinallyUnwindOneFrame

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NoCatchThrowWithFinallyUnwindOneFrame1()
        {
            try
            {
                /* ExceptionThrown                  - NotImplementedException
                 * ExceptionUnwindFunctionEnter     - NoCatchThrowWithFinallyUnwindOneFrame2
                 * ExceptionUnwindFinallyEnter      - NoCatchThrowWithFinallyUnwindOneFrame2
                 * ExceptionUnwindFinallyLeave      - NoCatchThrowWithFinallyUnwindOneFrame2
                 * ExceptionUnwindFunctionLeave     - NoCatchThrowWithFinallyUnwindOneFrame2
                 * ExceptionUnwindFunctionEnter     - NoCatchThrowWithFinallyUnwindOneFrame1
                 * ExceptionCatcherEnter            - catch (NotImplementedException)
                 * ExceptionCatcherLeave            - catch (NotImplementedException)
                 * Leave NoCatchThrowInFinallyUnwindOneFrame1
                 */
                NoCatchThrowWithFinallyUnwindOneFrame2();
            }
            catch (NotImplementedException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void NoCatchThrowWithFinallyUnwindOneFrame2()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            finally
            {
                //This generates unmanaged/managed transition events, so it should be commented out when verifying the events that occur
                Debug.WriteLine($"In {nameof(NoCatchThrowInFinallyUnwindOneFrame2)} Finally");
            }
        }

        #endregion
        #region NoCatchThrowInFinallyUnwindOneFrame

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NoCatchThrowInFinallyUnwindOneFrame1()
        {
            try
            {
                /* ExceptionThrown                  - NotImplementedException
                 * ExceptionUnwindFunctionEnter     - NoCatchThrowInFinallyUnwindOneFrame2
                 * ExceptionUnwindFinallyEnter      - NoCatchThrowInFinallyUnwindOneFrame2
                 *     ExceptionThrown              - InvalidOperationException
                 *     ExceptionUnwindFunctionEnter - NoCatchThrowInFinallyUnwindOneFrame2
                 *     ExceptionUnwindFunctionLeave - NoCatchThrowInFinallyUnwindOneFrame2
                 * ExceptionUnwindFunctionEnter     - NoCatchThrowInFinallyUnwindOneFrame1
                 * ExceptionCatcherEnter            - catch (InvalidOperationException)
                 * ExceptionCatcherLeave            - catch (InvalidOperationException)
                 * Leave NoCatchThrowInFinallyUnwindOneFrame1
                 */
                NoCatchThrowInFinallyUnwindOneFrame2();
            }
            catch (NotImplementedException)
            {
                //Because exception handling occurs in two phases (search then unwind), even though we'll always throw
                //in the finally, if there isn't a suitable handler capable of handling the NotImplementedException, the finally
                //will never be run
            }
            catch (InvalidOperationException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void NoCatchThrowInFinallyUnwindOneFrame2()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            finally
            {
                throw new InvalidOperationException("Error Message 2");
            }
        }

        #endregion
        #region UncaughtInNative

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UncaughtInNative()
        {
            try
            {
                //When the exception occurs, we don't get any transition events

                /* Enter UncaughtInNative
                 * ManagedToUnmanaged (Call)        - Call EnumWindows
                 * UnmanagedToManaged (Call)        - Call delegate
                 * Enter <UncaughtInNative>b__18_0  - delegate
                 *
                 * ExceptionThrown                  - throw NotImplementedException
                 * ExceptionUnwindFunctionEnter     - delegate
                 * ExceptionUnwindFunctionLeave     - delegate
                 * ExceptionUnwindFunctionEnter     - UncaughtInNative
                 * ExceptionCatcherEnter            - catch (NotImplementedException)
                 * ExceptionCatcherLeave            - catch (NotImplementedException)
                 * Leave UncaughtInNative
                 */
                NativeMethods.EnumWindows((hWnd, lParam) => throw new NotImplementedException("Error Message"), IntPtr.Zero);
            }
            catch (NotImplementedException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UncaughtInNative_DoubleCallback()
        {
            try
            {
                NativeMethods.EnumWindows((hWnd_1, lParam_1) =>
                {
                    return NativeMethods.EnumWindows((hWnd_2, lParam_2) => throw new NotImplementedException("Error Message"), IntPtr.Zero);
                }, IntPtr.Zero);
            }
            catch (NotImplementedException)
            {
            }
        }

        #endregion
        #region CaughtInNative

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CaughtInNative()
        {
            var sos = GetSOSDacInterface();
            var appDomains = new ulong[1];

            Marshal.ThrowExceptionForHR(sos.GetAppDomainList(1, appDomains, out _));
            Marshal.ThrowExceptionForHR(sos.GetAssemblyList(appDomains[0], 0, null, out var numAssemblies));

            var assemblies = new ulong[numAssemblies];
            Marshal.ThrowExceptionForHR(sos.GetAssemblyList(appDomains[0], numAssemblies, assemblies, out numAssemblies));
            Marshal.ThrowExceptionForHR(sos.GetAssemblyModuleList(assemblies[0], 0, null, out var numModules));

            Debug.WriteLine("Checking modules");
            var modules = new ulong[numModules];
            Marshal.ThrowExceptionForHR(sos.GetAssemblyModuleList(assemblies[0], numModules, modules, out numModules));

            /* UnmanagedToManaged (Call)        - TraverseModuleMap internally invoke our callback
             * Enter TypeDefToModuleCallback    - our callback invoked
             *
             * ExceptionThrown                  - throw NotImplementedException
             * ExceptionUnwindFunctionEnter     - TypeDefToModuleCallback
             * ExceptionUnwindFunctionLeave     - TypeDefToModuleCallback
             * UnmanagedToManaged (Return)      - gracefully return HRESULT
             */
            Debug.WriteLine("Calling!");
            var hr = sos.TraverseModuleMap(ModuleMapType.TypeDefToMethodTable, modules[0], TypeDefToModuleCallback, IntPtr.Zero);

            Debug.WriteLine($"Completed with {hr:X}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TypeDefToModuleCallback(int index, ulong methodtable, IntPtr token)
        {
            //This generates unmanaged/managed transition events, so it should be commented out when verifying the events that occur
            Debug.WriteLine("Throwing in callback");
            throw new NotImplementedException("Error Message 1");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ISOSDacInterface GetSOSDacInterface()
        {
            var dataTarget = new DataTarget(Process.GetCurrentProcess());

            var clrDataCreateInstance = GetCLRDataCreateInstance();

            clrDataCreateInstance(typeof(ISOSDacInterface).GUID, dataTarget, out var iface);

            return (ISOSDacInterface)iface;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CLRDataCreateInstanceDelegate GetCLRDataCreateInstance()
        {
            /* We need to create a ClrDataAccess object (IXCLRDataProcess/ISOSDacInterface) from mscordacwks.dll.
             * This can be done by calling the mscordacwks!CLRDataCreateInstance method. We can't simply P/Invoke
             * this method however, as P/Invoke will search the global PATH for any and all mscordacwks.dll assemblies,
             * which is wrong. We need to load the specific mscordacwks.dll from the exact runtime version/bitness
             * we're executing under. */
            var mscordacwksPath = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscordacwks.dll");

            var mscordacwks = NativeMethods.LoadLibrary(mscordacwksPath);

            if (mscordacwks == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load library '{mscordacwksPath}': {Marshal.GetHRForLastWin32Error()}");

            var clrDataCreateInstancePtr = NativeMethods.GetProcAddress(mscordacwks, "CLRDataCreateInstance");

            if (clrDataCreateInstancePtr == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to find function 'CLRDataCreateInstance': {Marshal.GetHRForLastWin32Error()}");

            var clrDataCreateInstance = Marshal.GetDelegateForFunctionPointer<CLRDataCreateInstanceDelegate>(clrDataCreateInstancePtr);

            return clrDataCreateInstance;
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Rethrow()
        {
            try
            {
                try
                {
                    throw new NotImplementedException("Error Message 1");
                }
                catch (NotImplementedException)
                {
                    throw;
                }
            }
            catch (NotImplementedException)
            {
            }
        }

        #region CallFunctionInCatchAndThrow

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CallFunctionInCatchAndThrow1()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException)
            {
                try
                {
                    CallFunctionInCatchAndThrow2();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CallFunctionInCatchAndThrow2()
        {
            throw new InvalidOperationException("Error Message 2");
        }

        #endregion

        #region ThrownInFilterAndCaught

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThrownInFilterAndCaught()
        {
            try
            {
                throw new NotImplementedException("Error Message 1");
            }
            catch (NotImplementedException) when (FilterThatCatches())
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool FilterThatCatches()
        {
            try
            {
                throw new InvalidOperationException("Error Message 2");
            }
            catch (InvalidOperationException)
            {
            }

            return true;
        }

        #endregion
        #region ThrownInFilterAndNotCaught

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThrownInFilterAndNotCaught()
        {
            //We should NOT have an InvalidOperationException. Instead, we should have a TimeoutException

            try
            {
                try
                {
                    try
                    {
                        throw new NotImplementedException("Error Message 1");
                    }
                    catch (NotImplementedException) when (FilterThatThrows())
                    {
                        throw new InvalidOperationException("Error Message 2");
                    }
                }
                catch (NotImplementedException)
                {
                    Debug.WriteLine("Throwing TimeoutException");
                    throw new TimeoutException();
                }
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("Catch TimeoutException");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool FilterThatThrows()
        {
            throw new ArgumentException();
        }

        #endregion
        #region ThrownInFilterThatUnwindsOneFrameAndNotCaught

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThrownInFilterThatUnwindsOneFrameAndNotCaught()
        {
            //We should NOT have an InvalidOperationException. Instead, we should have a TimeoutException

            try
            {
                try
                {
                    try
                    {
                        throw new NotImplementedException("Error Message 1");
                    }
                    catch (NotImplementedException) when (FilterThatThrowsAndUnwinds1())
                    {
                        throw new InvalidOperationException("Error Message 2");
                    }
                }
                catch (NotImplementedException)
                {
                    Debug.WriteLine("Throwing TimeoutException");
                    throw new TimeoutException();
                }
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("Catch TimeoutException");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UntracedThread()
        {
            try
            {
                var doc = new XDocument();

                throw new NotImplementedException("Error Message " + doc);
            }
            catch (NotImplementedException)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool FilterThatThrowsAndUnwinds1()
        {
            return FilterThatThrowsAndUnwinds2();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool FilterThatThrowsAndUnwinds2()
        {
            throw new ArgumentException();
        }

        #endregion
    }
}
