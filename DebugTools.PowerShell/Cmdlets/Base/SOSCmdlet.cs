using ClrDebug;
using DebugTools.SOS;
using static ClrDebug.Extensions;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class SOSCmdlet : ProfilerSessionCmdlet
    {
        protected SOSDacInterface SOS
        {
            get
            {
                if (Session.SOS == null)
                    Session.SOS = CLRDataCreateInstance(new DataTarget(Session.Process)).SOSDacInterface;

                return Session.SOS;
            }
        }
    }
}
