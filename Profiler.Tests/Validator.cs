using System.Collections.Generic;
using System.Linq;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    class Validator
    {
        public ThreadStack[] ThreadStacks { get; }

        public MethodInfo[] Methods { get; }

        public Validator(ThreadStack[] threadStacks, MethodInfo[] methods)
        {
            ThreadStacks = threadStacks;
            Methods = methods;
        }

        public void HasFrame(string name)
        {
            var frame = FindFrame(name);

            var info = frame.MethodInfo;

            Assert.AreEqual("DebugTools.TestHost.ProfilerType", info.TypeName);
            Assert.AreEqual("DebugTools.TestHost.exe", info.ModuleName);
        }

        internal MethodFrame FindFrame(string name)
        {
            var stack = new Stack<IFrame>();

            foreach (var thread in ThreadStacks)
                stack.Push(thread.Root);

            while (stack.Count > 0)
            {
                var item = stack.Pop();

                if (item is MethodFrame m)
                {
                    if (m.MethodInfo.MethodName == name)
                        return m;
                }

                foreach (var child in item.Children)
                    stack.Push(child);
            }

            throw new AssertFailedException($"Failed to find frame '{name}'");
        }
    }
}