using System.IO;
using System.Text;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class PtrValue : IValue<object>
    {
        public CorElementType ElementType { get; }

        public object Value { get; }

        public PtrValue(BinaryReader reader, ValueSerializer serializer)
        {
            ElementType = (CorElementType) reader.ReadByte();

            switch (ElementType)
            {
                case CorElementType.Char:
                    reader.ReadByte(); //ELEMENT_TYPE_CHAR again
                    Value = new StringValue(reader);
                    break;

                default:
                    Value = serializer.ReadValue();
                    break;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("*");

            var value = Value;
            CorElementType elmType = ElementType;

            while (value is PtrValue v)
            {
                builder.Append("*");
                elmType = v.ElementType;
                value = v.Value;
            }

            switch (elmType)
            {
                case CorElementType.Class:
                case CorElementType.ValueType:
                    builder.Insert(0, GetTypeName(((ComplexTypeValue) value).Name));
                    return builder.ToString();
            }

            builder.Insert(0, GetPtrType(elmType));
            builder.Append(" (").Append(value).Append(")");

            return builder.ToString();
        }

        private static string GetPtrType(CorElementType elmType)
        {
            switch (elmType)
            {
                case CorElementType.Boolean:
                    return "bool";
                case CorElementType.Char:
                    return "char";
                case CorElementType.I1:
                    return "sbyte";
                case CorElementType.U1:
                    return "byte";
                case CorElementType.I2:
                    return "short";
                case CorElementType.U2:
                    return "ushort";
                case CorElementType.I4:
                    return "int";
                case CorElementType.U4:
                    return "uint";
                case CorElementType.I8:
                    return "long";
                case CorElementType.U8:
                    return "ulong";
                case CorElementType.R4:
                    return "float";
                case CorElementType.R8:
                    return "double";
                case CorElementType.I:
                    return "IntPtr";
                case CorElementType.U:
                    return "UIntPtr";

                case CorElementType.Void:
                    return "void";

                default:
                    return "?";
            }
        }

        private string GetTypeName(string name)
        {
            var dot = name.LastIndexOf('.');

            if (dot != -1)
                return name.Substring(dot + 1);

            return name;
        }
    }
}
