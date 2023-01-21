using ClrDebug;

namespace DebugTools.SOS
{
    //This is a special frame type - a clr!Frame
    class SOSHelperStackFrame : SOSStackFrame
    {
        public override SOSStackFrameType Type => SOSStackFrameType.Helper;

        public string HelperName { get; }

        public SOSHelperStackFrame(CLRDATA_ADDRESS ip, DacpFrameData frameData, XCLRDataStackWalk stackWalk, SOSDacInterface sos, DataTarget dataTarget) : base(ip, frameData.frameAddr)
        {
            HelperName = GetHelperName(frameData.frameAddr, sos, dataTarget);

            string name = null;

            if (sos.TryGetMethodDescPtrFromFrame(frameData.frameAddr, out var methodDesc) == HRESULT.S_OK)
            {
                MethodDesc = methodDesc;

                if (sos.TryGetMethodDescData(methodDesc, ip, out var methodDescData) == HRESULT.S_OK)
                    MethodTable = methodDescData.data.MethodTablePtr;

                sos.TryGetMethodDescName(methodDesc, out name);
            }
            else
            {
                if (stackWalk.TryGetFrame(out var frame) == HRESULT.S_OK)
                {
                    if (frame.TryGetMethodInstance(out var method) == HRESULT.S_OK)
                        method.TryGetName(0, out name);
                }
            }

            if (name == null)
            {
                FullName = null;
                Name = FullName;
            }
            else
            {
                FullName = name;
                Name = GetShortName(name);
            }

            MethodName = GetMethodName(Name);
            Parameters = GetParameters(stackWalk);
        }

        private string GetHelperName(
            CLRDATA_ADDRESS frameAddr,
            SOSDacInterface sos,
            DataTarget dataTarget)
        {
            var ptr = dataTarget.ReadVirtual<CLRDATA_ADDRESS>(frameAddr);

            if (sos.TryGetFrameName(ptr, out var name) == HRESULT.S_OK)
                return name;

            return "Frame";
        }

        public override string ToString()
        {
            if (Name == null)
                return HelperName;

            return base.ToString();
        }
    }
}
