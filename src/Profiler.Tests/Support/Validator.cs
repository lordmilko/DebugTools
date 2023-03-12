using System;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    class Validator
    {
        public ThreadStack[] ThreadStacks { get; }

        public IMethodInfo[] Methods { get; }

        public Validator(ThreadStack[] threadStacks, IMethodInfo[] methods)
        {
            ThreadStacks = threadStacks;
            Methods = methods;
        }

        public void HasFrame(string name, string typeName = "DebugTools.TestHost.ProfilerType")
        {
            var frame = FindFrame(name, typeName);

            var info = frame.MethodInfo;

            Assert.AreEqual("DebugTools.TestHost.exe", info.ModuleName);
        }

        internal IMethodFrame[] FindFrames(Func<IMethodFrame, bool> predicate)
        {
            var stack = new Stack<IFrame>();

            foreach (var thread in ThreadStacks)
                stack.Push(thread.Root);

            var results = new List<IMethodFrame>();

            while (stack.Count > 0)
            {
                var item = stack.Pop();

                if (item is IMethodFrame m)
                {
                    if (predicate(m))
                        results.Add(m);
                }

                foreach (var child in item.Children)
                    stack.Push(child);
            }

            return results.ToArray();
        }

        internal IMethodFrame FindFrame(string methodName, string typeName = null)
        {
            var stack = new Stack<IFrame>();

            foreach (var thread in ThreadStacks)
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

        internal ThreadStack FindThread(string name)
        {
            var match = ThreadStacks.SingleOrDefault(t => t.Root.ThreadName == name);

            if (match == null)
                Assert.Fail($"Thread '{name}' could not be found");

            return match;
        }
    }
}
