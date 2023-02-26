using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using ClrDebug;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class ValueFactory
    {
        private static int nextModule;

        public static IDictionary<int, ModuleInfo> KnownModules => knownModules.ToDictionary(kv => kv.Value.UniqueModuleID, kv => kv.Value);

        private static ConcurrentDictionary<Module, ModuleInfo> knownModules = new ConcurrentDictionary<Module, ModuleInfo>();

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

        public static IMockValue<object> Class(string type, params IMockValue[] fieldValues) =>
            Class(type, fieldValues, 0, 0);

        public static IMockValue<object> Class(string type, IMockValue[] fieldValues, mdTypeDef typeDef, int uniqueModuleID)
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
                        writer.Write(typeDef);
                        writer.Write(uniqueModuleID);

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

                    var dimensionSizes = new int[elements.Rank];

                    for (var i = 0; i < elements.Rank; i++)
                    {
                        var dimensionLength = elements.GetLength(i);
                        writer.Write(dimensionLength);
                        dimensionSizes[i] = dimensionLength;
                    }

                    var totalLength = elements.Length;

                    var indices = new int[elements.Rank];

                    var currentDimension = elements.Rank - 1;

                    for (var i = 0; i < totalLength; i++)
                    {
                        var current = (IMockValue) elements.GetValue(indices);

                        writer.Write((byte) current.ElementType);
                        current.Stream.Seek(0, SeekOrigin.Begin);
                        current.Stream.CopyTo(writer.BaseStream);

                        ArrayValue.UpdateArrayIndices(indices, ref currentDimension, dimensionSizes, elements.Rank);

                        currentDimension = elements.Rank - 1;
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

        public static IMockValue<object> Struct(string type, params IMockValue[] fieldValues) =>
            Struct(type, fieldValues, 0, 0);

        public static IMockValue<object> Struct(string type, IMockValue[] fieldValues, mdTypeDef typeDef, int uniqueModuleID)
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
                    writer.Write(typeDef);
                    writer.Write(uniqueModuleID);

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

        public static IMockValue<object> Ptr(IMockValue value)
        {
            var stream = MakeStream(writer =>
            {
                if (value.ElementType == CorElementType.String)
                {
                    //Write the type twice
                    writer.Write((byte) CorElementType.Char);
                    writer.Write((byte)CorElementType.Char);
                }
                else
                {
                    //Write the type twice
                    writer.Write((byte)value.ElementType);
                    writer.Write((byte)value.ElementType);
                }

                value.Stream.Seek(0, SeekOrigin.Begin);
                value.Stream.CopyTo(writer.BaseStream);
            });

            return new MockValue<PtrValue, object>(
                CorElementType.Ptr,
                stream,
                (r, s) => new PtrValue(r, s)
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
                    Type type = value.GetType();

                    if (type.IsArray)
                    {
                        if (type.GetArrayRank() == 1)
                        {
                            return SZArray(
                                GetElementType(type.GetElementType()),
                                ((object[])value).Select(FromRaw).ToArray()
                            );
                        }
                        else
                        {
                            var newArray = BuildNewArray((Array) value);

                            return Array(
                                GetElementType(type.GetElementType()),
                                newArray
                            );
                        }
                    }

                    if (type.IsValueType)
                    {
                        return Struct(
                            type.Name,
                            type.GetFields().Select(f => FromRaw(f.GetValue(value))).ToArray(),
                            type.MetadataToken,
                            GetModule(type.Module)
                        );
                    }
                    else
                    {
                        return Class(
                            type.Name,
                            type.GetFields().Select(f => FromRaw(f.GetValue(value))).ToArray(),
                            type.MetadataToken,
                            GetModule(type.Module)
                        );
                    }
            }
        }

        private static Array BuildNewArray(Array original)
        {
            var dimensionSizes = new int[original.Rank];

            for (var i = 0; i < original.Rank; i++)
            {
                var dimensionLength = original.GetLength(i);
                dimensionSizes[i] = dimensionLength;
            }

            var newArray = System.Array.CreateInstance(typeof(object), dimensionSizes);

            var totalLength = original.Length;

            var indices = new int[original.Rank];

            var currentDimension = original.Rank - 1;

            for (var i = 0; i < totalLength; i++)
            {
                var current = original.GetValue(indices);

                var val = FromRaw(current);

                newArray.SetValue(val, indices);

                ArrayValue.UpdateArrayIndices(indices, ref currentDimension, dimensionSizes, original.Rank);

                currentDimension = original.Rank - 1;
            }

            return newArray;
        }

        private static CorElementType GetElementType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return CorElementType.Boolean;
                case TypeCode.Char:
                    return CorElementType.Char;
                case TypeCode.SByte:
                    return CorElementType.I1;
                case TypeCode.Byte:
                    return CorElementType.U1;
                case TypeCode.Int16:
                    return CorElementType.I2;
                case TypeCode.UInt16:
                    return CorElementType.U2;
                case TypeCode.Int32:
                    return CorElementType.I4;
                case TypeCode.UInt32:
                    return CorElementType.U4;
                case TypeCode.Int64:
                    return CorElementType.I8;
                case TypeCode.UInt64:
                    return CorElementType.U8;
                case TypeCode.Single:
                    return CorElementType.R4;
                case TypeCode.Double:
                    return CorElementType.R8;
                case TypeCode.String:
                    return CorElementType.String;
                default:
                    if (type == typeof(object))
                        return CorElementType.Object;

                    if (type.IsClass)
                        return CorElementType.Class;

                    if (type.IsValueType)
                        return CorElementType.ValueType;

                    throw new NotImplementedException($"Don't know how to handle value of type '{type.Name}'.");
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

        internal static int GetModule(Module module)
        {
            return knownModules.GetOrAdd(module, m =>
            {
                var uniqueModuleID = Interlocked.Increment(ref nextModule);

                return new ModuleInfo(uniqueModuleID, module.FullyQualifiedName);
            }).UniqueModuleID;
        }
    }
}
