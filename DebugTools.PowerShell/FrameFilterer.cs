using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    public class FrameFilterer : IDisposable
    {
        private FrameFilterOptions options;

        private WildcardPattern[] includeWildcards;
        private WildcardPattern[] excludeWildcards;
        private WildcardPattern[] stringWildcards;
        private WildcardPattern[] typeWildcards;

        private ConcurrentDictionary<IFrame, byte> includes;

        public List<IFrame> SortedFrames => includes.Keys.OrderBy(f => f.Sequence).ToList();

        public List<IFrame> HighlightFrames { get; } = new List<IFrame>();

        public ConcurrentDictionary<object, byte> MatchedValues { get; }

        public FrameFilterer(FrameFilterOptions options)
        {
            this.options = options;

            includeWildcards = MakeWildcard(options.Include);
            excludeWildcards = MakeWildcard(options.Exclude);
            stringWildcards = MakeWildcard(options.StringValue);
            typeWildcards = MakeWildcard(options.TypeName);

            if (options.HasFilterValue)
                MatchedValues = new ConcurrentDictionary<object, byte>();

            if (options.Unique)
                includes = new ConcurrentDictionary<IFrame, byte>(FrameEqualityComparer.Instance);
            else
                includes = new ConcurrentDictionary<IFrame, byte>();
        }

        private WildcardPattern[] MakeWildcard(string[] arr)
        {
            if (arr == null)
                return null;

            return arr.Select(i => new WildcardPattern(i, WildcardOptions.IgnoreCase)).ToArray();
        }

        public void ProcessFrame(IFrame frame)
        {
            var queue = new ConcurrentQueue<IFrame>();
            queue.Enqueue(frame);

            while (queue.Count > 0)
            {
                DequeueAll(queue).AsParallel().ForAll(item =>
                {
                    if (item is IRootFrame r)
                    {
                        if (r.ThreadName != null && ShouldInclude(i => i.IsMatch(r.ThreadName)) && !ShouldExclude(r) && !options.HasFilterValue)
                            includes[item] = 0;
                    }
                    else if (item is IMethodFrameDetailed d)
                    {
                        if (ShouldInclude(i => i.IsMatch(d.MethodInfo.MethodName) || i.IsMatch(d.MethodInfo.TypeName)) && HasValue(d) && !ShouldExclude(d))
                        {
                            includes[item] = 0;
                        }
                    }
                    else if (item is IMethodFrame m)
                    {
                        if (ShouldInclude(i => i.IsMatch(m.MethodInfo.MethodName) || i.IsMatch(m.MethodInfo.TypeName)) && !ShouldExclude(m))
                        {
                            includes[item] = 0;
                        }
                    }

                    foreach (var child in item.Children)
                        queue.Enqueue(child);
                });
            }
        }

        #region Update Frames

        public List<IFrame> GetSortedMaybeValueFilteredFrames()
        {
            if (options.HasFilterValue)
            {
                var knownOriginalFrames = new Dictionary<IFrame, IFrame>();

                var newRoots = new List<IFrame>();

                var sortedFrames = SortedFrames;

                foreach (var frame in sortedFrames)
                {
                    var originalStackTrace = GetOriginalStackTrace(frame);

                    var newRoot = GetNewFrames(originalStackTrace, sortedFrames, knownOriginalFrames);

                    if (newRoot != null)
                        newRoots.Add(newRoot);
                }

                return newRoots;
            }
            else
                return SortedFrames;
        }

        private List<IFrame> GetOriginalStackTrace(IFrame frame)
        {
            var list = new List<IFrame> { frame };

            var parent = frame;

            while (!(parent is IRootFrame))
            {
                parent = parent.Parent;
                list.Add(parent);
            }

            list.Reverse();

            return list;
        }

        private IRootFrame GetNewFrames(
            List<IFrame> originalStackTrace,
            List<IFrame> originalSortedFrames,
            Dictionary<IFrame, IFrame> knownOriginalFrames)
        {
            IFrame newParent = null;
            IRootFrame newRoot = null;

            foreach (var item in originalStackTrace)
            {
                if (!knownOriginalFrames.TryGetValue(item, out var newItem))
                {
                    if (item is IRootFrame r)
                    {
                        newRoot = new RootFrame
                        {
                            ThreadId = r.ThreadId,
                            ThreadName = r.ThreadName
                        };

                        newItem = newRoot;
                        newParent = newRoot;
                    }
                    else if (item is IMethodFrameDetailed d)
                    {
                        newItem = d.CloneWithNewParent(newParent);
                        newParent.Children.Add((IMethodFrame)newItem);

                        if (MethodFrameDetailed.ParameterCache.TryGetValue(d, out var parameters))
                            MethodFrameDetailed.ParameterCache.Add((IMethodFrameDetailed)newItem, parameters);

                        if (MethodFrameDetailed.ReturnCache.TryGetValue(d, out var returnValue))
                            MethodFrameDetailed.ReturnCache.Add((IMethodFrameDetailed)newItem, returnValue);

                        newParent = newItem;
                    }
                    else if (item is IMethodFrame m)
                    {
                        newItem = m.CloneWithNewParent(newParent);
                        newParent.Children.Add((IMethodFrame)newItem);
                        newParent = newItem;
                    }

                    knownOriginalFrames[item] = newItem;
                }
                else
                    newParent = newItem;

                if (originalSortedFrames.Contains(item, FrameEqualityComparer.Instance))
                    HighlightFrames.Add(newItem);
            }

            return newRoot;
        }

        #endregion

        private bool ShouldInclude(Func<WildcardPattern, bool> match)
        {
            if (includeWildcards == null)
                return true;

            return includeWildcards.Any(match);
        }

        private bool HasValue(IMethodFrameDetailed d)
        {
            if (options.HasFilterValue)
            {
                var parameters = d.GetEnterParameters();

                void CacheParameters() => MethodFrameDetailed.ParameterCache.Add(d, parameters);

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        if (MatchAny(d, parameter, parameter, CacheParameters))
                            return true;
                    }
                }

                var returnValue = d.GetExitResult();

                if (returnValue != null)
                {
                    void CacheResult() => MethodFrameDetailed.ReturnCache.Add(d, returnValue);

                    if (MatchAny(d, returnValue, returnValue, CacheResult))
                        return true;
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        private void AddMatchedValue(object value, object methodComponent)
        {
            if (ReferenceEquals(value, methodComponent))
                MatchedValues[methodComponent] = 0;
        }

        private bool MatchAny(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (MatchVoid(d, value, methodComponent, onSuccess))
                return true;

            #region Primitive

            if (MatchBool(d, value, methodComponent, onSuccess))
                return true;

            if (MatchChar(d, value, methodComponent, onSuccess))
                return true;

            if (MatchSByte(d, value, methodComponent, onSuccess))
                return true;

            if (MatchByte(d, value, methodComponent, onSuccess))
                return true;

            if (MatchInt16(d, value, methodComponent, onSuccess))
                return true;

            if (MatchUInt16(d, value, methodComponent, onSuccess))
                return true;

            if (MatchInt32(d, value, methodComponent, onSuccess))
                return true;

            if (MatchUInt32(d, value, methodComponent, onSuccess))
                return true;

            if (MatchInt64(d, value, methodComponent, onSuccess))
                return true;

            if (MatchUInt64(d, value, methodComponent, onSuccess))
                return true;

            if (MatchFloat(d, value, methodComponent, onSuccess))
                return true;

            if (MatchDouble(d, value, methodComponent, onSuccess))
                return true;

            if (MatchIntPtr(d, value, methodComponent, onSuccess))
                return true;

            if (MatchUIntPtr(d, value, methodComponent, onSuccess))
                return true;

            #endregion

            if (MatchString(d, value, methodComponent, onSuccess))
                return true;

            if (MatchClassOrStructParameter(d, value, methodComponent, onSuccess))
            {
                AddMatchedValue(value, methodComponent);
                return true;
            }

            if (MatchArray(d, value, methodComponent, onSuccess))
            {
                AddMatchedValue(value, methodComponent);
                return true;
            }

            return false;
        }

        private bool MatchVoid(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is VoidValue && options.VoidValue)
            {
                AddMatchedValue(value, methodComponent);
                onSuccess();

                return true;
            }

            return false;
        }

        #region Primitive

        private bool MatchBool(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is BoolValue v && options.BoolValue != null)
            {
                if (options.BoolValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchChar(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is CharValue v && options.CharValue != null)
            {
                if (options.CharValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchSByte(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is SByteValue v && options.SByteValue != null)
            {
                if (options.SByteValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchByte(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is ByteValue v && options.ByteValue != null)
            {
                if (options.ByteValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchInt16(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is Int16Value v && options.Int16Value != null)
            {
                if (options.Int16Value.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchUInt16(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is UInt16Value v && options.UInt16Value != null)
            {
                if (options.UInt16Value.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchInt32(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is Int32Value v && options.Int32Value != null)
            {
                if (options.Int32Value.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchUInt32(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is UInt32Value v && options.UInt32Value != null)
            {
                if (options.UInt32Value.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchInt64(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is Int64Value v && options.Int64Value != null)
            {
                if (options.Int64Value.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchUInt64(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is UInt64Value v && options.UInt64Value != null)
            {
                if (options.UInt64Value.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchFloat(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is FloatValue v && options.FloatValue != null)
            {
                if (options.FloatValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchDouble(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is DoubleValue v && options.DoubleValue != null)
            {
                if (options.DoubleValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchIntPtr(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is IntPtrValue v && options.IntPtrValue != null)
            {
                if (options.IntPtrValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchUIntPtr(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is UIntPtrValue v && options.UIntPtrValue != null)
            {
                if (options.UIntPtrValue.Contains(v.Value))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        #endregion

        private bool MatchString(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is StringValue s && stringWildcards != null)
            {
                if (stringWildcards.Any(w => w.IsMatch(s.Value)))
                {
                    AddMatchedValue(value, methodComponent);
                    onSuccess();

                    return true;
                }
            }

            return false;
        }

        private bool MatchClassOrStructParameter(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is ComplexTypeValue c && c.Value != null)
            {
                if (typeWildcards != null)
                {
                    if (typeWildcards.Any(t => t.IsMatch(c.Name)))
                    {
                        AddMatchedValue(value, methodComponent);
                        return true;
                    }
                }

                if (c.FieldValues != null)
                {
                    foreach (var field in c.FieldValues)
                    {
                        if (MatchAny(d, field, methodComponent, onSuccess))
                        {
                            AddMatchedValue(field, methodComponent);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool MatchArray(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is SZArrayValue sz && sz.Value != null)
            {
                foreach (var item in sz.Value)
                {
                    if (MatchAny(d, item, methodComponent, onSuccess))
                    {
                        AddMatchedValue(item, methodComponent);
                        return true;
                    }
                }
            }

            if (value is ArrayValue arr && arr.Value != null)
            {
                foreach (var item in arr.Value)
                {
                    if (MatchAny(d, item, methodComponent, onSuccess))
                    {
                        AddMatchedValue(item, methodComponent);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ShouldExclude(IRootFrame root)
        {
            if (excludeWildcards == null)
                return false;

            return excludeWildcards.Any(e => e.IsMatch(root.ThreadName));
        }

        private bool ShouldExclude(IMethodFrame frame)
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
