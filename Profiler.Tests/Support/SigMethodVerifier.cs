using System;
using DebugTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    internal struct SigMethodVerifier
    {
        private SigMethod method;

        public SigMethodVerifier(SigMethod method)
        {
            this.method = method;
        }

        public SigMethodVerifier ReturnsVoid()
        {
            Assert.AreEqual("void", method.RetType.ToString());
            return this;
        }

        public SigMethodVerifier HasNoParams()
        {
            Assert.AreEqual(0, method.Parameters.Length);
            return this;
        }

        public SigMethodVerifier HasParam<T>(int i, string paramName = "a")
        {
            Assert.AreEqual(GetTypeName(typeof(T)) + $" {paramName}", method.Parameters[i].ToString());
            return this;
        }

        private string GetTypeName(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return "bool";
                case TypeCode.Byte:
                    return "byte";
                case TypeCode.Char:
                    return "char";
                case TypeCode.Decimal:
                    return "decimal";
                case TypeCode.Double:
                    return "double";
                case TypeCode.Int16:
                    return "short";
                case TypeCode.Int32:
                    return "int";
                case TypeCode.Int64:
                    return "long";
                case TypeCode.SByte:
                    return "sbyte";
                case TypeCode.Single:
                    return "float";
                case TypeCode.String:
                    return "string";
                case TypeCode.UInt16:
                    return "ushort";
                case TypeCode.UInt32:
                    return "uint";
                case TypeCode.UInt64:
                    return "ulong";
                default:
                    return type.Name;
            }
        }
    }
}