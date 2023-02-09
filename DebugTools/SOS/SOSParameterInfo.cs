using System;
using ClrDebug;

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

            if (frame.TryGetArgumentByIndex(i, out var argInfo) != HRESULT.S_OK)
                return;

            Name = argInfo.name;

            // At times we cannot print the value of a parameter (most
            // common case being a non-primitive value type).  In these
            // cases we need to print the location of the parameter,
            // so that we can later examine it (e.g. using !dumpvc)
            var result = argInfo.arg.TryGetNumLocations(out var numLocs) == HRESULT.S_OK && numLocs == 1;

            if (result)
            {
                result = argInfo.arg.TryGetLocationByIndex(0, out var locInfo) == HRESULT.S_OK;

                if (result)
                {
                    if (locInfo.flags == ClrDataValueLocationFlag.CLRDATA_VLOC_REGISTER)
                        Location = "CLRRegister";
                    else
                        Location = locInfo.arg.ToString();
                }
            }

            if (argInfo.arg.Raw.GetBytes(0, out var dataSize, out var buffer) == HRESULT.ERROR_BUFFER_OVERFLOW)
            {
                var hr = argInfo.arg.TryGetBytes(dataSize + 1, out var byteInfo);

                if (hr == HRESULT.S_OK)
                    Value = byteInfo.buffer.ToInt64();
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
