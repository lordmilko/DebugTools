using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class ValueSerializer : IDisposable
    {
        public BinaryReader Reader { get; }

        private byte[] data;

        public static List<object> FromParameters(byte[] data)
        {
            var serializer = new ValueSerializer(data);

            var numParameters = serializer.Reader.ReadUInt32();

            var parameters = new List<object>();

            for (var i = 0; i < numParameters; i++)
            {
                var value = serializer.ReadValue();

                parameters.Add(value);

                if (value == MaxTraceDepth.Instance)
                    break;
            }

            return parameters;
        }

        public static object FromReturnValue(byte[] data)
        {
            var serializer = new ValueSerializer(data);

            var value = serializer.ReadValue();

            return value;
        }

        public ValueSerializer(byte[] data)
        {
            this.data = data;

            Reader = new BinaryReader(new MemoryStream(data), Encoding.Unicode);
        }

        public object ReadValue()
        {
            var type = (CorElementType) Reader.ReadByte();

            switch (type)
            {
                case CorElementType.Void:
                    return VoidValue.Instance;

                case CorElementType.Boolean:
                    return new BoolValue(Reader);

                case CorElementType.Char:
                    return new CharValue(Reader);

                case CorElementType.I1:
                    return new SByteValue(Reader);

                case CorElementType.U1:
                    return new ByteValue(Reader);

                case CorElementType.I2:
                    return new Int16Value(Reader);

                case CorElementType.U2:
                    return new UInt16Value(Reader);

                case CorElementType.I4:
                    return new Int32Value(Reader);

                case CorElementType.U4:
                    return new UInt32Value(Reader);

                case CorElementType.I8:
                    return new Int64Value(Reader);

                case CorElementType.U8:
                    return new UInt64Value(Reader);

                case CorElementType.R4:
                    return new FloatValue(Reader);

                case CorElementType.R8:
                    return new DoubleValue(Reader);

                case CorElementType.I:
                    return new IntPtrValue(Reader);

                case CorElementType.U:
                    return new UIntPtrValue(Reader);

                case CorElementType.String:
                    return new StringValue(Reader);

                case CorElementType.Class:
                case CorElementType.Object:
                    return new ClassValue(Reader, this);

                case CorElementType.SZArray:
                    return new SZArrayValue(Reader, this);

                case CorElementType.Array:
                    return new ArrayValue(Reader, this);

                case CorElementType.ValueType:
                    return new StructValue(Reader, this);

                case CorElementType.Ptr:
                    return new PtrValue(Reader, this);

                case CorElementType.FnPtr:
                    return new FnPtrValue(Reader);

                //We reached the max trace depth. Any values after this are missing
                case CorElementType.End:
                {
                    var reason = (CorElementType)Reader.ReadByte();

                    switch (reason)
                    {
                        case CorElementType.End:
                            return MaxTraceDepth.Instance;

                        case CorElementType.Class:
                            return RecursionValue.ClassInstance;

                        case CorElementType.GenericInst:
                            return RecursionValue.GenericInstInstance;

                        case CorElementType.Array:
                            return RecursionValue.Array;

                        case CorElementType.SZArray:
                            return RecursionValue.SZArray;

                        default:
                            throw new NotImplementedException($"Don't know how to handle end reason of type '{reason}'.");
                    }
                }

                default:
                    throw new NotImplementedException($"Don't know how to handle element of type '{type}'.");
            }
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }
    }
}
