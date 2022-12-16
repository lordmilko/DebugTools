using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Find, "DbgProfilerStackFrame")]
    public class FindDbgProfilerStackFrame : StackFrameCmdlet
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public string[] Include { get; set; }

        private WildcardPattern[] include;
        private ConcurrentDictionary<IFrame, byte> includes;

        public void Begin() => BeginProcessing();
        public void Process() => ProcessRecord();
        public List<IFrame> Frames => includes.Keys.ToList();

        protected override void BeginProcessing()
        {
            include = Include.Select(i => new WildcardPattern(i, WildcardOptions.IgnoreCase)).ToArray();

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
                        if (r.ThreadName != null && include.Any(i => i.IsMatch(r.ThreadName)))
                            includes[item] = 0;
                    }
                    else if (item is MethodFrame m)
                    {
                        if (include.Any(i => i.IsMatch(m.MethodInfo.MethodName) || i.IsMatch(m.MethodInfo.TypeName)))
                        {
                            includes[item] = 0;
                        }
                    }

                    foreach (var child in item.Children)
                        queue.Enqueue(child);
                });
            }
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
    }
}