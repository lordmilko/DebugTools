using System;
using ClrDebug;
using static ClrDebug.HRESULT;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSParameterInfo
    {
        //The SOSStackFrame holds a reference to the SOSParameterInfo which holds a reference to the SOSStackFrame.
        //The serializer will see there is a recursive reference automatically. The deserialized SOSFrame will have
        //reference equality with the SOSStackFrame that contains this SOSParameterInfo.
        public SOSStackFrame SOSFrame { get; }

        public string Name { get; }

        public string Location { get; }

        public CLRDATA_ADDRESS? Value { get; }

        public SOSParameterInfo(SOSStackFrame sosFrame, XCLRDataFrame frame, int i)
        {
            SOSFrame = sosFrame;

            if (frame.TryGetArgumentByIndex(i, out var argInfo) != S_OK)
                return;

            Name = argInfo.name;

            // At times we cannot print the value of a parameter (most
            // common case being a non-primitive value type).  In these
            // cases we need to print the location of the parameter,
            // so that we can later examine it (e.g. using !dumpvc)
            var result = argInfo.arg.TryGetNumLocations(out var numLocs) == S_OK && numLocs == 1;

            if (result)
            {
                result = argInfo.arg.TryGetLocationByIndex(0, out var locInfo) == S_OK;

                if (result)
                {
                    if (locInfo.flags == ClrDataValueLocationFlag.CLRDATA_VLOC_REGISTER)
                        Location = "CLRRegister";
                    else
                        Location = locInfo.arg.ToString();
                }
            }

            if (argInfo.arg.Raw.GetBytes(0, out var dataSize, null) == ERROR_BUFFER_OVERFLOW)
            {
                var hr = argInfo.arg.TryGetBytes(dataSize + 1, out var byteInfo);

                if (hr == S_OK)
                {
                    var buffer = byteInfo.buffer;

                    switch (dataSize)
                    {
                        case 1:
                            Value = (ulong)buffer[0];
                            break;

                        case 2:
                            Value = (ulong)BitConverter.ToUInt16(buffer, 0);
                            break;

                        case 4:
                            Value = (ulong) BitConverter.ToUInt32(buffer, 0);
                            break;

                        case 8:
                            Value = BitConverter.ToUInt64(buffer, 0);
                            break;
                    }
                }
            }
        }

        public override string ToString()
        {
            if (Location == null)
                return $"{Name} = {(object)Value ?? "?"}";

            return $"{Name} ({Location}) = {(object)Value ?? "?"}";
        }
    }
}
