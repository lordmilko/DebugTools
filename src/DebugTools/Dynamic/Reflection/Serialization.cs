using System;
using System.Collections.Generic;
using System.Reflection;

namespace DebugTools.Dynamic
{
    class Serialization
    {
        public static bool IsSerializable(object value) => IsSerializable(value, new HashSet<Type>());

        private static bool IsSerializable(object value, HashSet<Type> seenTypes)
        {
            if (value == null)
                return true;

            var type = value.GetType();

            //If we've already analyzed it, we'll go with whatever the result of that analysis is. If it's false, we're going to return false in the end
            if (!seenTypes.Add(type))
                return true;

            if (Type.GetTypeCode(type) != TypeCode.Object)
                return true;

            if (!type.IsSerializable)
                return false;

            if (type.IsArray)
            {
                var arr = (object[])value;

                foreach (var item in arr)
                    return IsSerializable(item, seenTypes);
            }

            //type.GetElementType() doesn't work for System.Array. Don't know how to get the underlying type,
            //so we must assume false
            if (typeof(Array).IsAssignableFrom(type))
                return false;

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if ((field.Attributes & FieldAttributes.NotSerialized) == FieldAttributes.NotSerialized)
                    continue;

                if (!IsSerializable(field.GetValue(value), seenTypes))
                    return false;
            }

            return true;
        }
    }
}
