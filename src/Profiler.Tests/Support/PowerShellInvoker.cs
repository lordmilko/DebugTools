using System.Linq;
using System.Management.Automation;

namespace Profiler.Tests
{
    class PowerShellInvoker
    {
        private PowerShell powerShell;

        public PowerShellInvoker()
        {
            powerShell = PowerShell.Create();
        }

        public T[] Invoke<T>(string cmdlet, object param, object sessionParam, string inputCmdlet)
        {
            try
            {
                if (inputCmdlet != null)
                {
                    powerShell.AddCommand(inputCmdlet);
                    AddParameters(sessionParam);
                }

                powerShell.AddCommand(cmdlet);

                AddParameters(param);
                AddParameters(sessionParam);

                var result = powerShell.Invoke().Select(v => v.BaseObject).ToArray();

                var errors = powerShell.Streams.Error.ToArray();

                if (errors.Length > 0)
                    throw errors[0].Exception;

                return result.Cast<T>().ToArray();
            }
            finally
            {
                powerShell.Commands.Clear();
            }
        }

        private void AddParameters(object param)
        {
            if (param != null)
            {
                if (param.GetType().Name.Contains("AnonymousType"))
                {
                    var properties = param.GetType().GetProperties();

                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(param);

                        powerShell.AddParameter(prop.Name, value);
                    }
                }
                else
                {
                    //Positional
                    powerShell.AddArgument(param);
                }
            }
        }
    }
}
