using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using DebugTools.PowerShell.Cmdlets;

namespace Profiler.Tests
{
    class PowerShellInvoker : IDisposable
    {
        protected PowerShell powerShell;
        protected InitialSessionState initialSessionState;

        public PowerShellInvoker(Func<InitialSessionState, Runspace> runspace = null)
        {
            var dll = typeof(GetDbgProfiler).Assembly.Location;
            var directory = Path.GetDirectoryName(dll);
            var module = Path.Combine(directory, "DebugTools");

            var initial = InitialSessionState.CreateDefault();
            initial.ImportPSModule(new[] { module });

            initialSessionState = initial;

            if (runspace == null)
                powerShell = PowerShell.Create(initial);
            else
            {
                powerShell = PowerShell.Create();
                powerShell.Runspace = runspace(initial);
                powerShell.Runspace.Open();
            }
        }

        public T[] Invoke<T>(string cmdlet, object param, object sessionParam = null, string inputCmdlet = null, (string outputCmdlet, object param)[] outputCmdlets = null)
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

                if (outputCmdlets != null)
                {
                    foreach (var outputCmdlet in outputCmdlets)
                    {
                        powerShell.AddCommand(outputCmdlet.outputCmdlet);
                        AddParameters(outputCmdlet.param);
                    }
                }

                var raw = powerShell.Invoke();

                object[] result;

                if (typeof(T) == typeof(PSObject))
                    result = raw.Cast<object>().ToArray();
                else
                    result = raw.Select(v => v.BaseObject).ToArray();

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

        public string[] GetWarnings() => powerShell.Streams.Warning.Select(v => v.Message).ToArray();

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

                        if (value != null)
                            powerShell.AddParameter(prop.Name, value);
                    }
                }
                else if (param is IDictionary d)
                {
                    foreach (DictionaryEntry kv in d)
                    {
                        if (kv.Value != null)
                            powerShell.AddParameter(kv.Key.ToString(), kv.Value);
                    }
                }
                else
                {
                    //Positional
                    powerShell.AddArgument(param);
                }
            }
        }

        public void Dispose()
        {
            powerShell?.Dispose();
        }
    }
}
