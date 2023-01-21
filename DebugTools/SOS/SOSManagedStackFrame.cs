using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    class SOSManagedStackFrame : SOSStackFrame
    {
        public override SOSStackFrameType Type => SOSStackFrameType.Managed;

        public SOSManagedStackFrame(CLRDATA_ADDRESS ip, CLRDATA_ADDRESS sp, XCLRDataStackWalk stackWalk, SOSDacInterface sos) : base(ip, sp)
        {
            string name = null;

            if (sos.TryGetMethodDescPtrFromIP(ip, out var methodDesc) == HRESULT.S_OK)
            {
                MethodDesc = methodDesc;

                sos.TryGetMethodDescName(methodDesc, out name);

                if (sos.TryGetMethodDescData(methodDesc, 0, out var methodData) == HRESULT.S_OK)
                {
                    MethodTable = methodData.data.MethodTablePtr;
                }
            }

            if (name == null)
            {
                FullName = "Unknown";
                Name = FullName;
            }
            else
            {
                FullName = name;
                Name = GetShortName(name);
            }

            Parameters = GetParameters(stackWalk);

            MethodName = GetMethodName(Name);
        }
    }
}
