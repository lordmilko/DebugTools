using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Find, "DbgProfilerStackFrame")]
    public class FindDbgProfilerStackFrame : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false)]
        public string StringValue { get; set; }

        private WildcardPattern[] includeWildcards;
        private WildcardPattern[] excludeWildcards;

        private ConcurrentDictionary<IFrame, byte> includes;

        public void Begin() => BeginProcessing();
        public void Process() => ProcessRecord();
        public List<IFrame> Frames => includes.Keys.ToList();

        public HashSet<object> MatchedValues { get; } = new HashSet<object>();

        protected override void BeginProcessing()
        {
            includeWildcards = Include.Select(i => new WildcardPattern(i, WildcardOptions.IgnoreCase)).ToArray();

            if (Exclude != null)
                excludeWildcards = Exclude.Select(e => new WildcardPattern(e, WildcardOptions.IgnoreCase)).ToArray();

            if (Unique)
                includes = new ConcurrentDictionary<IFrame, byte>(FrameEqualityComparer.Instance);
            else
                includes = new ConcurrentDictionary<IFrame, byte>();
        }

        protected override void DoProcessRecordEx()
        {
            CalculateIncludes(Frame);
        }

        protected override void EndProcessing()
        {
            foreach (var item in includes.Keys)
                WriteObject(item);
        }

        private void CalculateIncludes(IFrame frame)
        {
            var queue = new ConcurrentQueue<IFrame>();
            queue.Enqueue(frame);

            while (queue.Count > 0)
            {
                DequeueAll(queue).AsParallel().ForAll(item =>
                {
                    if (item is RootFrame r)
                    {
                        if (r.ThreadName != null && includeWildcards.Any(i => i.IsMatch(r.ThreadName)) && !ShouldExclude(r) && !HasValueFilter())
                            includes[item] = 0;
                    }
                    else if (item is MethodFrameDetailed d)
                    {
                        if (includeWildcards.Any(i => i.IsMatch(d.MethodInfo.MethodName) || i.IsMatch(d.MethodInfo.TypeName)) && HasValue(d) && !ShouldExclude(d))
                        {
                            includes[item] = 0;
                        }
                    }
                    else if (item is MethodFrame m)
                    {
                        if (includeWildcards.Any(i => i.IsMatch(m.MethodInfo.MethodName) || i.IsMatch(m.MethodInfo.TypeName)) && !ShouldExclude(m))
                        {
                            includes[item] = 0;
                        }
                    }

                    foreach (var child in item.Children)
                        queue.Enqueue(child);
                });
            }
        }

        private bool HasValueFilter()
        {
            return StringValue != null;
        }

        private bool HasValue(MethodFrameDetailed d)
        {
            if (StringValue != null)
            {
                var parameters = d.GetEnterParameters();

                foreach (var parameter in parameters)
                {
                    if (parameter is StringValue s)
                    {
                        if (s.Value == StringValue)
                        {
                            MatchedValues.Add(s);
                            MethodFrameDetailed.ParameterCache.Add(d, parameters);

                            return true;
                        }
                    }
                }

                var returnValue = d.GetExitResult();

                if (returnValue is StringValue sr)
                {
                    if (sr.Value == StringValue)
                    {
                        MatchedValues.Add(sr);
                        MethodFrameDetailed.ReturnCache.Add(d, returnValue);

                        return true;
                    }
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        private bool ShouldExclude(RootFrame root)
        {
            if (excludeWildcards == null)
                return false;

            return excludeWildcards.Any(e => e.IsMatch(root.ThreadName));
        }

        private bool ShouldExclude(MethodFrame frame)
        {
            if (excludeWildcards == null)
                return false;

            return excludeWildcards.Any(e => e.IsMatch(frame.MethodInfo.MethodName) || e.IsMatch(frame.MethodInfo.TypeName));
        }

        private IEnumerable<IFrame> DequeueAll(ConcurrentQueue<IFrame> queue)
        {
            while (queue.Count > 0)
            {
                var result = queue.TryDequeue(out var frame);

                if (result)
                    yield return frame;
                else
                    yield break;
            }
        }

        public void Dispose()
        {
            MethodFrameDetailed.ClearCaches();
        }
    }
}
