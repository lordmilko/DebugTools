﻿using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerMethod")]
    public class GetDbgProfilerMethod : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        [Parameter(Mandatory = false)]
        public string[] ModuleName { get; set; }

        [Parameter(Mandatory = false)]
        public string[] TypeName { get; set; }

        [Parameter(Mandatory = false)]
        public string[] MethodName { get; set; }

        [Parameter(Mandatory = false)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter LastTrace { get; set; }

        protected override void ProcessRecordEx()
        {
            IEnumerable<IMethodInfo> methods;

            if (LastTrace)
                methods = GetLastTraceMethods();
            else
                methods = Session.Methods.Values;

            if (Name != null)
            {
                var wildcards = Name.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                methods = methods.Where(m => wildcards.All(w =>
                {
                    if (w.IsMatch(m.MethodName))
                        return true;

                    if (w.IsMatch(m.TypeName))
                        return true;

                    if (w.IsMatch(m.ModuleName))
                        return true;

                    return false;
                }));
            }

            if (ModuleName != null)
                methods = FilterByWildcardArray(ModuleName, methods, m => m.ModuleName);

            if (TypeName != null)
                methods = FilterByWildcardArray(TypeName, methods, m => m.TypeName);

            if (MethodName != null)
                methods = FilterByWildcardArray(MethodName, methods, m => m.MethodName);

            if (Exclude != null)
            {
                var wildcards = Exclude.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                methods = methods.Where(m => wildcards.All(w =>
                {
                    if (w.IsMatch(m.MethodName))
                        return false;

                    if (w.IsMatch(m.TypeName))
                        return false;

                    if (w.IsMatch(m.ModuleName))
                        return false;

                    return true;
                }));
            }

            WriteObject(methods, true);
        }

        private IEnumerable<IMethodInfo> GetLastTraceMethods()
        {
            var unique = new HashSet<IMethodInfo>();

            if (Session.LastTrace == null)
                return Enumerable.Empty<IMethodInfo>();

            var stack = new Stack<IFrame>();

            var stacks = Session.LastTrace;

            foreach (var trace in stacks)
                stack.Push(trace.Root);

            while (stack.Count > 0)
            {
                var item = stack.Pop();

                if (item is IMethodFrame m)
                    unique.Add(m.MethodInfo);

                foreach (var child in item.Children)
                    stack.Push(child);
            }

            return unique;
        }
    }
}
