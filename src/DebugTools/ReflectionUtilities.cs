using System;
using System.Reflection;

namespace DebugTools
{
    static class ReflectionUtilities
    {
        public static FieldInfo GetInternalFieldInfo(this Type type, string name)
        {
            var fieldInfo = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
                throw new MissingMemberException(type.Name, name);

            return fieldInfo;
        }

        public static object GetInternalField(this object obj, string name)
        {
            var fieldInfo = GetInternalFieldInfo(obj.GetType(), name);

            return fieldInfo.GetValue(obj);
        }

        public static PropertyInfo GetInternalPropertyInfo(this object obj, string name) =>
            GetInternalPropertyInfo(obj.GetType(), name);

        public static PropertyInfo GetInternalPropertyInfo(this Type type, string name)
        {
            var propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null)
                throw new MissingMemberException(type.Name, name);

            return propertyInfo;
        }

        public static object GetInternalProperty(this object obj, string name)
        {
            var propertyInfo = GetInternalPropertyInfo(obj, name);

            return propertyInfo.GetValue(obj);
        }

        public static MethodInfo GetInternalMethod(this object obj, string name) => GetInternalMethod(obj.GetType(), name);

        public static MethodInfo GetInternalMethod(this Type type, string name)
        {
            var methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
                throw new MissingMemberException(type.Name, name);

            return methodInfo;
        }

        public static MethodInfo GetStaticInternalMethod(this Type type, string name)
        {
            var methodInfo = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo == null)
                throw new MissingMemberException(type.Name, name);

            return methodInfo;
        }
    }
}