using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    public static class AssertEx
    {
        public static void Throws<T>(Action action, string message, bool checkMessage = true) where T : Exception
        {
            try
            {
                action();

                Assert.Fail($"Expected an assertion of type {typeof(T)} to be thrown, however no exception occurred");
            }
            catch (T ex)
            {
                if (checkMessage)
                    Assert.IsTrue(ex.Message.Contains(message), $"Exception message '{ex.Message}' did not contain string '{message}'");
            }
            catch (Exception ex) when (!(ex is AssertFailedException))
            {
                throw;
            }
        }
    }
}
