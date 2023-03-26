using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Profiler.Tests")]

[assembly: DebuggerDisplay("Command = {DebugTools.PowerShell.PSCmdletDebuggerDisplay.GetValue(this),nq}", Target = typeof(PSCmdlet))]
