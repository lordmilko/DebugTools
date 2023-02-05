using DebugTools.Profiler;
using DebugTools.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    internal struct ExceptionVerifier
    {
        private ExceptionInfo[] exceptions;
        internal Validator Validator { get; }

        public ExceptionVerifier(ExceptionInfo[] exceptions, Validator validator)
        {
            this.exceptions = exceptions;
            Validator = validator;
        }

        public void HasException(int index, string type, ExceptionStatus completedReason)
        {
            if (index >= exceptions.Length)
                Assert.Fail($"Expected {index + 1} exceptions but found {exceptions.Length}");

            var exception = exceptions[index];

            Assert.AreEqual(type, exception.Type);
            Assert.AreEqual(completedReason, exception.Status);
        }

        public void HasException(string type, ExceptionStatus completedReason)
        {
            if (exceptions.Length != 1)
                Assert.Fail($"Expected a single exception but found {exceptions.Length}");

            HasException(0, type, completedReason);
        }
    }
}
