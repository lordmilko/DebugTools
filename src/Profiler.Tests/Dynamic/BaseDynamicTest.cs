using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DebugTools.Dynamic;

namespace Profiler.Tests
{
    public class BaseDynamicTest
    {
        protected void Test<T>(Action<dynamic> action, [CallerMemberName] string callerMemberName = null)
        {
            var appDomain = AppDomain.CreateDomain(callerMemberName);

            try
            {
                var factory = (DummyFactory)appDomain.CreateInstanceFromAndUnwrap(
                    typeof(DummyFactory).Assembly.Location,
                    typeof(DummyFactory).FullName,
                    false,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new object[0],
                    null,
                    null
                );

                var remoteStub = factory.Create(typeof(T).Assembly.Location, typeof(T).FullName);

                var localStub = LocalProxyStub.New(remoteStub);

                action(localStub);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        protected void Test<T1, T2>(Action<dynamic, dynamic> action, [CallerMemberName] string callerMemberName = null)
        {
            var appDomain = AppDomain.CreateDomain(callerMemberName);

            try
            {
                var factory = (DummyFactory)appDomain.CreateInstanceFromAndUnwrap(
                    typeof(DummyFactory).Assembly.Location,
                    typeof(DummyFactory).FullName,
                    false,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new object[0],
                    null,
                    null
                );

                var remoteStub1 = factory.Create(typeof(T1).Assembly.Location, typeof(T1).FullName);
                var localStub1 = LocalProxyStub.New(remoteStub1);

                var remoteStub2 = factory.Create(typeof(T2).Assembly.Location, typeof(T2).FullName);
                var localStub2 = LocalProxyStub.New(remoteStub2);

                action(localStub1, localStub2);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }
    }
}
