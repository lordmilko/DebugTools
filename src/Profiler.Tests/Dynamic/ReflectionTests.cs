using System;
using System.Linq;
using System.Reflection;
using DebugTools.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    class ReflectionProxyType
    {
        void NoArgs()
        {
        }

        void SingleArg_Int(int value)
        {
        }

        void SingleArg_String(string value)
        {
        }

        void SingleArg_Enum(DayOfWeek value)
        {
        }

        #region MultipleOverloads_IntString

        void MultipleOverloads_IntString(int value)
        {
        }

        void MultipleOverloads_IntString(string value)
        {
        }

        #endregion
        #region MultipleOverloads_StringEnum

        void MultipleOverloads_StringEnum(string value)
        {
        }

        void MultipleOverloads_StringEnum(DayOfWeek value)
        {
        }

        #endregion
        #region MultipleOverloads_IntEnum

        void MultipleOverloads_IntEnum(int value)
        {
        }

        void MultipleOverloads_IntEnum(DayOfWeek value)
        {
        }

        #endregion
    }

    [TestClass]
    public class ReflectionTests
    {
        private static MethodInfo NoArgs;
        private static MethodInfo SingleArg_Int;
        private static MethodInfo SingleArg_String;
        private static MethodInfo SingleArg_Enum;

        private static MethodInfo MultipleOverloads_IntString_PassInt;
        private static MethodInfo MultipleOverloads_IntString_PassString;

        private static MethodInfo MultipleOverloads_StringEnum_PassString;
        private static MethodInfo MultipleOverloads_StringEnum_PassEnum;

        private static MethodInfo MultipleOverloads_IntEnum_PassInt;
        private static MethodInfo MultipleOverloads_IntEnum_PassEnum;

        private static ReflectionCache Cache;

        static ReflectionTests()
        {
            MethodInfo GetMethod(string name, params Type[] args)
            {
                var method = typeof(ReflectionProxyType).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);

                if (method == null)
                    throw new NotImplementedException($"Failed to get method '{name}'");

                return method;
            }

            NoArgs = GetMethod("NoArgs");
            SingleArg_Int = GetMethod("SingleArg_Int", typeof(int));
            SingleArg_String = GetMethod("SingleArg_String", typeof(string));
            SingleArg_Enum = GetMethod("SingleArg_Enum", typeof(DayOfWeek));

            MultipleOverloads_IntString_PassInt = GetMethod("MultipleOverloads_IntString", typeof(int));
            MultipleOverloads_IntString_PassString = GetMethod("MultipleOverloads_IntString", typeof(string));

            MultipleOverloads_StringEnum_PassString = GetMethod("MultipleOverloads_StringEnum", typeof(string));
            MultipleOverloads_StringEnum_PassEnum = GetMethod("MultipleOverloads_StringEnum", typeof(DayOfWeek));

            MultipleOverloads_IntEnum_PassInt = GetMethod("MultipleOverloads_IntEnum", typeof(int));
            MultipleOverloads_IntEnum_PassEnum = GetMethod("MultipleOverloads_IntEnum", typeof(DayOfWeek));

            Cache = new ReflectionCache(typeof(ReflectionProxyType));
        }

        [TestMethod]
        public void Reflection_NoArgs() => Test(NoArgs);

        [TestMethod]
        public void Reflection_SingleArg_Int() => Test(SingleArg_Int, typeof(int));

        [TestMethod]
        public void Reflection_SingleArg_String() => Test(SingleArg_String, typeof(string));

        [TestMethod]
        public void Reflection_SingleArg_Enum() => Test(SingleArg_Enum, typeof(DayOfWeek));

        [TestMethod]
        public void Reflection_SingleArg_Int_PassString() => Test(SingleArg_Int, typeof(string));

        [TestMethod]
        public void Reflection_SingleArg_String_PassNonString() => Test(SingleArg_String, typeof(int));

        #region MultipleOverloads_IntString

        [TestMethod]
        public void Reflection_MultipleOverloads_IntString_PassInt() =>
            Test(MultipleOverloads_IntString_PassInt, typeof(int));

        [TestMethod]
        public void Reflection_MultipleOverloads_IntString_PassString() =>
            Test(MultipleOverloads_IntString_PassString, typeof(string));

        #endregion
        #region MultipleOverloads_StringEnum

        [TestMethod]
        public void Reflection_MultipleOverloads_StringEnum_PassString() =>
            Test(MultipleOverloads_StringEnum_PassString, typeof(string));

        [TestMethod]
        public void Reflection_MultipleOverloads_StringEnum_PassEnum() =>
            Test(MultipleOverloads_StringEnum_PassEnum, typeof(DayOfWeek));

        #endregion
        #region MultipleOverloads_IntEnum

        [TestMethod]
        public void Reflection_MultipleOverloads_IntEnum_PassInt() =>
            Test(MultipleOverloads_IntEnum_PassInt, typeof(int));

        [TestMethod]
        public void Reflection_MultipleOverloads_IntEnum_PassEnum() =>
            Test(MultipleOverloads_IntEnum_PassEnum, typeof(DayOfWeek));

        [TestMethod]
        public void Reflection_MultipleOverloads_IntEnum_PassString() =>
            Test(MultipleOverloads_IntEnum_PassEnum, typeof(string));

        #endregion

        private void Test(MethodInfo expectedMethod, params Type[] parameterTypes)
        {
            var candidates = Cache.Methods[expectedMethod.Name];

            var result = ReflectionProvider.FindBestMethod(candidates, parameterTypes);

            Assert.IsNotNull(result);

            Assert.AreEqual(expectedMethod, result.Value.Method);

            var instance = new ReflectionProxyType();

            var proxy = new ReflectionProxy(instance);

            var arguments = GetArguments(parameterTypes);

            proxy.CallAnyVoid(expectedMethod.Name, parameterTypes, arguments);
        }

        private object[] GetArguments(Type[] parameterTypes)
        {
            return parameterTypes.Select(t =>
            {
                if (t == typeof(int))
                    return 5;

                if (t == typeof(string))
                    return (object) "1";

                if (t == typeof(DayOfWeek))
                    return DayOfWeek.Wednesday;

                throw new NotImplementedException($"Don't know how to handle type '{t.Name}'.");
            }).ToArray();
        }
    }
}
