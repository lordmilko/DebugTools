using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace DebugTools.PowerShell
{
    class PSCmdletDebuggerDisplay
    {
        public static string GetValue(PSCmdlet cmdlet)
        {
            var builder = new StringBuilder();

            builder.Append(cmdlet.MyInvocation.MyCommand);

            foreach (var kv in cmdlet.MyInvocation.BoundParameters)
            {
                var parameterInfo = cmdlet.MyInvocation.MyCommand.Parameters[kv.Key];

                if (parameterInfo.ParameterType == typeof(SwitchParameter))
                {
                    if ((SwitchParameter)kv.Value)
                        builder.Append($" -{kv.Key}");
                    else
                        builder.Append($" -{kv.Key}:$false");
                }
                else
                {
                    var value = kv.Value;

                    if (IsIEnumerable(kv.Value))
                        value = string.Join(", ", ToIEnumerable(value));

                    builder.Append($" -{kv.Key} {value}");
                }
            }

            return builder.ToString();
        }

        public static bool IsIEnumerable(object obj)
        {
            return obj is IEnumerable && !(obj is string);
        }

        public static IEnumerable<object> ToIEnumerable(object obj)
        {
            if (obj is IEnumerable<object>)
                return (IEnumerable<object>)obj;

            return ((IEnumerable)obj).Cast<object>();
        }
    }
}
