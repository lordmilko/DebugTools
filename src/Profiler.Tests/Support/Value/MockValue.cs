using System;
using System.IO;
using ClrDebug;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockValue<TOuterValue, TInnerValue> : IMockValue<TInnerValue> where TOuterValue : IValue<TInnerValue>
    {
        public CorElementType ElementType { get; }

        public Stream Stream { get; }

        public TOuterValue OuterValue { get; }

        public TInnerValue RawValue => OuterValue.Value;

        public byte[] RawBytes { get; }

        public MockValue(CorElementType elementType, Stream stream, Func<BinaryReader, ValueSerializer, TOuterValue> makeValue)
        {
            ElementType = elementType;
            Stream = stream;
            RawBytes = ((MemoryStream) stream).ToArray();

            var serializer = new ValueSerializer(RawBytes);

            OuterValue = makeValue(serializer.Reader, serializer);
        }

        object IMockValue.OuterValue => OuterValue;
        object IMockValue.RawValue => RawValue;
    }
}
