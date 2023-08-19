using System;
using System.Linq;
using System.Reflection;

namespace DebugTools.Ui
{
    class WindowMessageStructReader
    {
        private static MethodInfo readStruct;
        private static MethodInfo readString;
        private MethodInfo[] readStructs;

        static WindowMessageStructReader()
        {
            readStruct = typeof(MMAReader).GetMethod(nameof(MMAReader.ReadStruct));
            readString = typeof(MMAReader).GetMethod(nameof(MMAReader.ReadString));
        }

        public WindowMessageStructReader(Type[] pointerTypes)
        {
            readStructs = pointerTypes.Select(v =>
            {
                if (v == typeof(string))
                    return readString;

                if (v.IsEnum)
                    throw new ArgumentException($"Cannot use enum type '{v.Name}' as a pointer type");

                return readStruct.MakeGenericMethod(v);
            }).ToArray();
        }

        public object[] ReadStructs(MMAReader reader) =>
            readStructs.Select(m => m.Invoke(reader, new object[0])).ToArray();
    }
}
