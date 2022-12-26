using System;
using System.Collections.Generic;
using System.IO;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class ValueSerializer : IDisposable
    {
        private BinaryReader reader;

        public uint NumParameters { get; }

        public List<object> Parameters { get; }

        private byte[] data;

        public ValueSerializer(byte[] data)
        {
            this.data = data;

            reader = new BinaryReader(new MemoryStream(data));

            NumParameters = reader.ReadUInt32();

            var parameters = new List<object>();

            for (var i = 0; i < NumParameters; i++)
                parameters.Add(ReadValue());

            Parameters = parameters;
        }

        internal object ReadValue()
        {
            var type = (CorElementType) reader.ReadByte();

            switch (type)
            {
                case CorElementType.Boolean:
                    return new BoolValue(reader);

                case CorElementType.Char:
                    return new CharValue(reader);

                case CorElementType.I1:
                    return new SByteValue(reader);

                case CorElementType.U1:
                    return new ByteValue(reader);

                case CorElementType.I2:
                    return new Int16Value(reader);

                case CorElementType.U2:
                    return new UInt16Value(reader);

                case CorElementType.I4:
                    return new Int32Value(reader);

                case CorElementType.U4:
                    return new UInt32Value(reader);

                case CorElementType.I8:
                    return new Int64Value(reader);

                case CorElementType.U8:
                    return new UInt64Value(reader);

                case CorElementType.R4:
                    return new FloatValue(reader);

                case CorElementType.R8:
                    return new DoubleValue(reader);

                case CorElementType.I:
                    return new IntPtrValue(reader);

                case CorElementType.U:
                    return new UIntPtrValue(reader);

                case CorElementType.String:
                    return new StringValue(reader);

                case CorElementType.Class:
                case CorElementType.Object:
                    return new ClassValue(reader, this);

                case CorElementType.SZArray:
                    return new SZArrayValue(reader, this);

                case CorElementType.ValueType:
                    return new ValueType(reader, this);

                default:
                    throw new NotImplementedException($"Don't know how to handle element of type '{type}'.");
            }
        }

        public void Dispose()
        {
            reader?.Dispose();
        }
    }
}
