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
                {
                    try
                    {
                        Session.DataTarget = new DataTarget(Session.Process);
                        Session.SOS = CLRDataCreateInstance(Session.DataTarget).SOSDacInterface;

                        var xclrProcess = new XCLRDataProcess((IXCLRDataProcess) Session.SOS.Raw);

                        Session.DataTarget.SetFlushCallback(() => xclrProcess.Flush());
                    }
                    catch
                    {
                        Session.DataTarget = null;
                        Session.SOS = null;

                        throw;
                    }
                }

                return Session.SOS;
            }
        }
    }
}
