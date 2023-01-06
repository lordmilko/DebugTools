using System;
using System.Linq;
using DebugTools.Profiler;
using DebugTools.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class ExceptionTests : BaseTest
    {
        [TestMethod]
        public void Exception_CaughtWithinMethod() =>
            Test(ExceptionTestType.CaughtWithinMethod, v =>
            {
                v.HasException("System.NotImplementedException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_UnwindOneFrame() =>
            Test(ExceptionTestType.UnwindOneFrame, v =>
            {
                v.HasException("System.NotImplementedException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_ThrownInCatchAndImmediatelyCaught() =>
            Test(ExceptionTestType.Nested_ThrownInCatchAndImmediatelyCaught, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_ThrownInCatchAndCaughtByOuterCatch() =>
            Test(ExceptionTestType.Nested_ThrownInCatchAndCaughtByOuterCatch, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch() =>
            Test(ExceptionTestType.Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_CaughtByOuterCatch() =>
            Test(ExceptionTestType.Nested_CaughtByOuterCatch, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_UnwindOneFrameFromThrowInCatch() =>
            Test(ExceptionTestType.Nested_UnwindOneFrameFromThrowInCatch, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_UnwindTwoFramesFromThrowInCatch() =>
            Test(ExceptionTestType.Nested_UnwindTwoFramesFromThrowInCatch, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_ThrownInFinallyAndImmediatelyCaught() =>
            Test(ExceptionTestType.Nested_ThrownInFinallyAndImmediatelyCaught, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_ThrownInFinallyAndUnwindOneFrame() =>
            Test(ExceptionTestType.Nested_ThrownInFinallyAndUnwindOneFrame, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_Nested_ThrownInFinallyAndUnwindTwoFrames() =>
            Test(ExceptionTestType.Nested_ThrownInFinallyAndUnwindTwoFrames, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_NoCatchThrowWithFinallyUnwindOneFrame() =>
            Test(ExceptionTestType.NoCatchThrowWithFinallyUnwindOneFrame, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_NoCatchThrowInFinallyUnwindOneFrame() =>
            Test(ExceptionTestType.NoCatchThrowInFinallyUnwindOneFrame, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_UncaughtInNative() =>
            Test(ExceptionTestType.UncaughtInNative, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_CaughtInNative() =>
            Test(ExceptionTestType.CaughtInNative, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.UnmanagedCaught);
            });

        [TestMethod]
        public void Exception_Rethrow() =>
            Test(ExceptionTestType.Rethrow, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded);
                v.HasException(1, "System.NotImplementedException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_CallFunctionInCatchAndThrow() =>
            Test(ExceptionTestType.CallFunctionInCatchAndThrow, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_ThrownInFilterAndCaught() =>
            Test(ExceptionTestType.ThrownInFilterAndCaught, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Caught);
                v.HasException(1, "System.InvalidOperationException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_ThrownInFilterAndNotCaught() =>
            Test(ExceptionTestType.ThrownInFilterAndNotCaught, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded); //Superseded by TimeoutException and NOT InvalidOperationException (whose filter failed)
                v.HasException(1, "System.ArgumentException", ExceptionStatus.UnhandledInFilter);
                v.HasException(2, "System.TimeoutException", ExceptionStatus.Caught);
            });

        [TestMethod]
        public void Exception_ThrownInFilterThatUnwindsOneFrameAndNotCaught() =>
            Test(ExceptionTestType.ThrownInFilterThatUnwindsOneFrameAndNotCaught, v =>
            {
                v.HasException(0, "System.NotImplementedException", ExceptionStatus.Superseded); //Superseded by TimeoutException and NOT InvalidOperationException (whose filter failed)
                v.HasException(1, "System.ArgumentException", ExceptionStatus.UnhandledInFilter);
                v.HasException(2, "System.TimeoutException", ExceptionStatus.Caught);
            });

        internal void Test(ExceptionTestType type, Action<ExceptionVerifier> validate, params ProfilerSetting[] settings)
        {
            TestInternal(TestType.Exception, type.ToString(), v => validate(new ExceptionVerifier(v.ThreadStacks.Single().Exceptions.Values.ToArray())), settings);
        }
    }
}
