using System;
using System.IO;
using System.Linq;
using System.Text;
using ClrDebug;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class ValueFactory
    {
        #region Primitive

        public static IMockValue<bool> Boolean(bool value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<BoolValue, bool>(
                CorElementType.Boolean,
                stream, (r, s) => new BoolValue(r)
            );
        }

        public static IMockValue<char> Char(char value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<CharValue, char>(
                CorElementType.Char,
                stream, (r, s) => new CharValue(r)
            );
        }

        public static IMockValue<sbyte> SByte(sbyte value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<SByteValue, sbyte>(
                CorElementType.I1,
                stream, (r, s) => new SByteValue(r)
            );
        }

        public static IMockValue<byte> Byte(byte value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<ByteValue, byte>(
                CorElementType.U1,
                stream, (r, s) => new ByteValue(r)
            );
        }

        public static IMockValue<short> Int16(short value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<Int16Value, short>(
                CorElementType.I2,
                stream, (r, s) => new Int16Value(r)
            );
        }

        public static IMockValue<ushort> UInt16(ushort value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<UInt16Value, ushort>(
                CorElementType.U2,
                stream, (r, s) => new UInt16Value(r)
            );
        }

        public static IMockValue<int> Int32(int value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<Int32Value, int>(
                CorElementType.I4,
                stream, (r, s) => new Int32Value(r)
            );
        }

        public static IMockValue<uint> UInt32(uint value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<UInt32Value, uint>(
                CorElementType.U4,
                stream, (r, s) => new UInt32Value(r)
            );
        }

        public static IMockValue<long> Int64(long value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<Int64Value, long>(
                CorElementType.I8,
                stream, (r, s) => new Int64Value(r)
            );
        }

        public static IMockValue<ulong> UInt64(ulong value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<UInt64Value, ulong>(
                CorElementType.U8,
                stream, (r, s) => new UInt64Value(r)
            );
        }

        public static IMockValue<float> Float(float value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<FloatValue, float>(
                CorElementType.R4,
                stream, (r, s) => new FloatValue(r)
            );
        }

        public static IMockValue<double> Double(double value)
        {
            var stream = MakeStream(writer => writer.Write(value));

            return new MockValue<DoubleValue, double>(
                CorElementType.R8,
                stream, (r, s) => new DoubleValue(r)
            );
        }

        public static IMockValue<IntPtr> IntPtr(IntPtr value)
        {
            var stream = MakeStream(writer => writer.Write(value.ToInt64()));

            return new MockValue<IntPtrValue, IntPtr>(
                CorElementType.I,
                stream, (r, s) => new IntPtrValue(r)
            );
        }

        public static IMockValue<UIntPtr> UIntPtr(UIntPtr value)
        {
            var stream = MakeStream(writer => writer.Write(value.ToUInt64()));

            return new MockValue<UIntPtrValue, UIntPtr>(
                CorElementType.U,
                stream, (r, s) => new UIntPtrValue(r)
            );
        }

        #endregion

        public static IMockValue<string> String(string value)
        {
            var stream = MakeStream(writer =>
            {
                if (value == null)
                    writer.Write(0);
                else
                {
                    writer.Write(value.Length + 1);
                    writer.Write(value.ToCharArray(), 0, value.Length);
                    writer.Write('\0');
                }
            });

            return new MockValue<StringValue, string>(
                CorElementType.String,
                stream,(r, s) => new StringValue(r)
            );
        }

        public static IMockValue<object> Class(string type, params IMockValue[] fieldValues)
        {
            var stream = MakeStream(writer =>
            {
                if (type == null)
                    writer.Write(0);
                else
                {
                    writer.Write(type.Length + 1);
                    writer.Write(type.ToCharArray(), 0, type.Length);
                    writer.Write('\0');

                    if (fieldValues == null)
                        writer.Write(0);
                    else
                    {
                        writer.Write(fieldValues.Length);

                        foreach (var field in fieldValues)
                        {
                            writer.Write((byte) field.ElementType);
                            field.Stream.Seek(0, SeekOrigin.Begin);
                            field.Stream.CopyTo(writer.BaseStream);
                        }
                    }
                }
            });

            return new MockValue<ClassValue, object>(
                CorElementType.Class,
                stream,
                (r, s) => new ClassValue(r, s)
            );
        }

        public static IMockValue<object[]> SZArray(CorElementType elementType, params IMockValue[] elements)
        {
            var stream = MakeStream(writer =>
            {
                writer.Write((byte) elementType);

                if (elementType != CorElementType.End)
                {
                    writer.Write(elements.Length);

                    foreach (var elm in elements)
                    {
                        writer.Write((byte) elm.ElementType);
                        elm.Stream.Seek(0, SeekOrigin.Begin);
                        elm.Stream.CopyTo(writer.BaseStream);
                    }
                }
            });

            return new MockValue<SZArrayValue, object[]>(
                CorElementType.SZArray,
                stream,
                (r, s) => new SZArrayValue(r, s)
            );
        }

        public static IMockValue<object[]> SZArrayNull() => SZArray(CorElementType.End);

        public static IMockValue<Array> Array(CorElementType elementType, Array elements)
        {
            var stream = MakeStream(writer =>
            {
                if (elementType == CorElementType.End)
                {
                    writer.Write(0);
                    writer.Write((byte) elementType);
                }
                else
                {
                    writer.Write(elements.Rank);
                    writer.Write((byte) elementType);

                    for (var i = 0; i < elements.Rank; i++)
                    {
                        var length = elements.GetLength(i);

                        writer.Write(length);

                        for (var j = 0; j < length; j++)
                        {
                            var elm = (IMockValue) elements.GetValue(i, j);

                            writer.Write((byte)elm.ElementType);
                            elm.Stream.Seek(0, SeekOrigin.Begin);
                            elm.Stream.CopyTo(writer.BaseStream);
                        }
                    }
                }
            });

            return new MockValue<ArrayValue, Array>(
                CorElementType.Array,
                stream,
                (r, s) => new ArrayValue(r, s)
            );
        }

        public static IMockValue<Array> ArrayNull() => Array(CorElementType.End, null);

        public static IMockValue<object> ValueType(string type, params IMockValue[] fieldValues)
        {
            var stream = MakeStream(writer =>
            {
                writer.Write(type.Length + 1);
                writer.Write(type.ToCharArray(), 0, type.Length);
                writer.Write('\0');

                if (fieldValues == null)
                    writer.Write(0);
                else
                {
                    writer.Write(fieldValues.Length);

                    foreach (var field in fieldValues)
                    {
                        writer.Write((byte)field.ElementType);
                        field.Stream.Seek(0, SeekOrigin.Begin);
                        field.Stream.CopyTo(writer.BaseStream);
                    }
                }
            });

            return new MockValue<StructValue, object>(
                CorElementType.ValueType,
                stream,
                (r, s) => new StructValue(r, s)
            );
        }

        public static IMockValue FromRaw(object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                    return Boolean((bool) value);
                case TypeCode.Char:
                    return Char((char) value);
                case TypeCode.SByte:
                    return SByte((sbyte) value);
                case TypeCode.Byte:
                    return Byte((byte) value);
                case TypeCode.Int16:
                    return Int16((short) value);
                case TypeCode.UInt16:
                    return UInt16((ushort) value);
                case TypeCode.Int32:
                    return Int32((int) value);
                case TypeCode.UInt32:
                    return UInt32((uint) value);
                case TypeCode.Int64:
                    return Int64((long) value);
                case TypeCode.UInt64:
                    return UInt64((ulong) value);
                case TypeCode.Single:
                    return Float((float) value);
                case TypeCode.Double:
                    return Double((double) value);
                case TypeCode.String:
                    return String((string) value);
                default:
                    return Class(value.GetType().Name, value.GetType().GetFields().Select(f => FromRaw(f.GetValue(value))).ToArray());
            }
        }

        private static Stream MakeStream(Action<BinaryWriter> write)
        {
            var stream = new MemoryStream();

            using (var writer = new BinaryWriter(stream, Encoding.Unicode, true))
            {
                write(writer);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
