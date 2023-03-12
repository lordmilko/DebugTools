using System.Collections.Generic;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    class ThreadVerifier
    {
        private ThreadStack thread;

        public ThreadVerifier(ThreadStack thread)
        {
            this.thread = thread;
        }

        public void HasFrame(string name, string typeName = "DebugTools.TestHost.ProfilerType")
        {
            var frame = FindFrame(name, typeName);

            var info = frame.MethodInfo;

            Assert.AreEqual("DebugTools.TestHost.exe", info.ModuleName);
        }

        internal IMethodFrame FindFrame(string methodName, string typeName = null)
        {
            var stack = new Stack<IFrame>();
            stack.Push(thread.Root);

            while (stack.Count > 0)
            {
                var item = stack.Pop();

                if (item is IMethodFrame m)
                {
                    if (m.MethodInfo.MethodName == methodName && (typeName == null || m.MethodInfo.TypeName == typeName))
                        return m;
                }

                foreach (var child in item.Children)
                    stack.Push(child);
            }

            throw new AssertFailedException($"Failed to find frame '{methodName}'");
        }
    }
}