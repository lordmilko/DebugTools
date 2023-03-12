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
        private WildcardPattern[] calledFromWildcards;
        private WildcardPattern[] stringWildcards;
        private WildcardPattern[] classTypeWildcards;
        private WildcardPattern[] methodModuleNameWildcards;
        private WildcardPattern[] methodTypeNameWildcards;
        private WildcardPattern[] methodNameWildcards;
        private WildcardPattern[] parentMethodModuleNameWildcards;
        private WildcardPattern[] parentMethodTypeNameWildcards;
        private WildcardPattern[] parentMethodNameWildcards;

        private bool hasMethodFilter;
        private bool hasParentMethodFilter;

        private bool anyExcluded;

        private ConcurrentDictionary<IFrame, byte> includes;
        private ConcurrentDictionary<IFrame, byte> calledFromFrames = new ConcurrentDictionary<IFrame, byte>();

#if DEBUG
        private int? maxDegreesOfParallelism = null;
        //private int? maxDegreesOfParallelism = 1;
#endif

        public List<IFrame> SortedFrames => includes.Keys.OrderBy(f => f.Sequence).ToList();

        public List<IFrame> HighlightFrames { get; } = new List<IFrame>();

        public ConcurrentDictionary<object, byte> MatchedValues { get; }

        public FrameFilterer(FrameFilterOptions options)
        {
            this.options = options;

            includeWildcards = MakeWildcard(options.Include);
            excludeWildcards = MakeWildcard(options.Exclude);
            calledFromWildcards = MakeWildcard(options.CalledFrom);
            stringWildcards = MakeWildcard(options.StringValue);
            classTypeWildcards = MakeWildcard(options.ClassTypeName);
            methodModuleNameWildcards = MakeWildcard(options.MethodModuleName);
            methodTypeNameWildcards = MakeWildcard(options.MethodTypeName);
            methodNameWildcards = MakeWildcard(options.MethodName);
            parentMethodModuleNameWildcards = MakeWildcard(options.ParentMethodModuleName);
            parentMethodTypeNameWildcards = MakeWildcard(options.ParentMethodTypeName);
            parentMethodNameWildcards = MakeWildcard(options.ParentMethodName);

            hasParentMethodFilter =
                parentMethodModuleNameWildcards != null ||
                parentMethodTypeNameWildcards != null ||
                parentMethodNameWildcards != null;

            hasMethodFilter =
                methodModuleNameWildcards != null ||
                methodTypeNameWildcards != null ||
                methodNameWildcards != null ||
                hasParentMethodFilter;

            if (options.HasValueFilter)
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
                var query = DequeueAll(queue).AsParallel();

#if DEBUG
                if (maxDegreesOfParallelism != null)
                    query = query.WithDegreeOfParallelism(maxDegreesOfParallelism.Value);
#endif

                query.ForAll(item =>
                {
                    if (CheckFrame(item))
                        includes[item] = 0;
                    else
                    {
                        if (!anyExcluded && !(item is IRootFrame))
                            anyExcluded = true;
                    }

                    foreach (var child in item.Children)
                        queue.Enqueue(child);
                });
            }
        }

        private bool CheckFrame(IFrame item)
        {
            if (item is IRootFrame r)
            {
                if (r.ThreadName != null && ShouldInclude(r, (f, w) => w.IsMatch(f.ThreadName)) && !ShouldExclude(r) && !options.HasValueFilter)
                    return true;
            }
            else if (item is IMethodFrameDetailed d)
            {
                if (ShouldInclude(d, (f, w) => w.IsMatch(f.MethodInfo.MethodName) || w.IsMatch(f.MethodInfo.TypeName)) && HasValue(d) && !ShouldExclude(d))
                    return true;
            }
            else if (item is IMethodFrame m)
            {
                if (ShouldInclude(m, (f, w) => w.IsMatch(f.MethodInfo.MethodName) || w.IsMatch(f.MethodInfo.TypeName)) && !ShouldExclude(m))
                    return true;
            }

            return false;
        }

        #region Update Frames

        public List<IFrame> GetSortedFilteredFrames()
        {
            var newRoots = new List<IFrame>();

            var sortedFrames = SortedFrames;

            if (options.CalledFrom == null)
            {
                var knownOriginalFrames = new Dictionary<IFrame, IFrame>();

                foreach (var frame in sortedFrames)
                {
                    var originalStackTrace = GetOriginalStackTrace(frame);

                    var newRoot = GetNewFrames(originalStackTrace, sortedFrames, knownOriginalFrames);

                    if (newRoot != null)
                        newRoots.Add(newRoot);
                }

                //If you did -Unique -Include *, everything will be highlighted. No point highlighting everything
                //if everything is selected
                if (!anyExcluded)
                    HighlightFrames.Clear();
            }
            else
            {
                if (options.IsCalledFromOnly)
                {
                    newRoots.AddRange(calledFromFrames.Keys);
                }
                else
                {
                    var allParents = new HashSet<IFrame>();

                    foreach (var frame in sortedFrames)
                    {
                        var p = frame.Parent;

                        var stackParents = new List<IFrame>();

                        var topFrameIndex = -1;

                        while (!(p is IRootFrame))
                        {
                            stackParents.Add(p);

                            if (calledFromFrames.ContainsKey(p))
                                topFrameIndex = stackParents.Count - 1;

                            p = p.Parent;
                        }

                        for (var i = 0; i <= topFrameIndex; i++)
                            allParents.Add(stackParents[i]);
                    }

                    Dictionary<IFrame, IFrame> knownOriginalFrames;

                    if (options.Unique)
                        knownOriginalFrames = new Dictionary<IFrame, IFrame>(FrameEqualityComparer.Instance);
                    else
                        knownOriginalFrames = new Dictionary<IFrame, IFrame>();

                    var dict = new Dictionary<IRootFrame, IRootFrame>();

                    foreach (var frame in calledFromFrames.Keys)
                    {
                        var newFrame = GetNewFramesForCalledFrom((IMethodFrame)frame, null, allParents, sortedFrames, knownOriginalFrames);

                        if (newFrame != null)
                        {
                            var originalRoot = frame.GetRoot();

                            if (dict.TryGetValue(originalRoot, out var newRoot))
                                newRoot.Children.Add(newFrame);
                            else
                            {
                                newRoot = originalRoot.Clone();
                                newRoot.Children.Add(newFrame);
                                dict[originalRoot] = newRoot;
                            }
                        }
                    }

                    newRoots = dict.Values.Cast<IFrame>().ToList();
                }
            }

            SortFrames(newRoots);

            return newRoots;
        }

        private void SortFrames<T>(List<T> frames) where T : IFrame
        {
            frames.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

            foreach (var frame in frames)
                SortFrames(frame.Children);
        }

        public bool CheckFrameAndClear(IFrame frame)
        {
            if (includes.ContainsKey(frame))
                return false;

            var result = CheckFrame(frame);

            if (result)
                includes[frame] = 0;

            MatchedValues?.Clear();

            return result;
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
                        newRoot = r.Clone();

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

        private IMethodFrame GetNewFramesForCalledFrom(
            IMethodFrame frame,
            IMethodFrame newParent,
            HashSet<IFrame> parents,
            List<IFrame> originalSortedFrames,
            Dictionary<IFrame, IFrame> knownOriginalFrames)
        {
            if (!knownOriginalFrames.TryGetValue(frame, out var newItem))
            {
                newItem = frame.CloneWithNewParent(newParent);

                foreach (var child in frame.Children)
                {
                    if (parents.Contains(child) || originalSortedFrames.Contains(child))
                    {
                        var newChild = GetNewFramesForCalledFrom(child, (IMethodFrame)newItem, parents, originalSortedFrames, knownOriginalFrames);

                        if (newChild != null)
                            newItem.Children.Add(newChild);
                    }
                }

                knownOriginalFrames[frame] = newItem;

                if (originalSortedFrames.Contains(frame, FrameEqualityComparer.Instance))
                    HighlightFrames.Add(newItem);
            }
            else
                return null;

            return (IMethodFrame) newItem;
        }

        #endregion

        private bool ShouldInclude<T>(T frame, Func<T, WildcardPattern, bool> match) where T : IFrame
        {
            bool result = false;

            if (options.Unmanaged)
            {
                if (!(frame is IUnmanagedTransitionFrame))
                    return false;
            }

            if (hasMethodFilter && !TryFilterByMethod(frame))
                return false;

            IFrame calledFromFrame = null;

            if (calledFromWildcards != null)
            {
                if (frame is IRootFrame)
                    return false;

                var parent = frame.Parent;

                while (!(parent is IRootFrame))
                {
                    if (parent is IMethodFrame f && parent is T)
                    {
                        if (calledFromWildcards.Any(w => match((T)parent, w)))
                        {
                            calledFromFrame = parent;
                            goto checkInclude;
                        }
                    }

                    parent = parent.Parent;
                }

                //Either the frame isn't a method frame, or there wasn't a match against our parent
                return false;
            }

        checkInclude:
            if (includeWildcards == null)
            {
                result = true;
                goto end;
            }

            if (includeWildcards.Any(w => match(frame, w)))
            {
                result = true;
                goto end;
            }

        end:
            if (result && calledFromFrame != null)
                calledFromFrames[calledFromFrame] = 0;

            return result;
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

        private bool TryFilterByMethod(IFrame frame)
        {
            if (!(frame is IMethodFrame f))
                return false;

            var methodInfo = f.MethodInfo;

            if (methodModuleNameWildcards != null)
            {
                if (!methodModuleNameWildcards.Any(w => w.IsMatch(methodInfo.ModuleName)))
                    return false;
            }

            if (methodTypeNameWildcards != null)
            {
                if (!methodTypeNameWildcards.Any(w => w.IsMatch(methodInfo.TypeName)))
                    return false;
            }

            if (methodNameWildcards != null)
            {
                if (!methodNameWildcards.Any(w => w.IsMatch(methodInfo.MethodName)))
                    return false;
            }

            if (frame.Parent is IMethodFrame parentFrame)
            {
                var parentMethodInfo = parentFrame.MethodInfo;

                if (parentMethodModuleNameWildcards != null)
                {
                    if (!parentMethodModuleNameWildcards.Any(w => w.IsMatch(parentMethodInfo.ModuleName)))
                        return false;
                }

                if (parentMethodTypeNameWildcards != null)
                {
                    if (!parentMethodTypeNameWildcards.Any(w => w.IsMatch(parentMethodInfo.TypeName)))
                        return false;
                }

                if (parentMethodNameWildcards != null)
                {
                    if (!parentMethodNameWildcards.Any(w => w.IsMatch(parentMethodInfo.MethodName)))
                        return false;
                }
            }
            else
            {
                //We want to filter by parent method, but we don't have a parent method
                if (hasParentMethodFilter)
                    return false;
            }

            return true;
        }

        private bool HasValue(IMethodFrameDetailed d)
        {
            if (options.HasValueFilter)
            {
                var parameters = d.GetEnterParameters();

                void CacheParameters()
                {
                    if (!MethodFrameDetailed.ParameterCache.TryGetValue(d, out _))
                        MethodFrameDetailed.ParameterCache.Add(d, parameters);
                }

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
                    void CacheResult()
                    {
                        if (!MethodFrameDetailed.ReturnCache.TryGetValue(d, out _))
                            MethodFrameDetailed.ReturnCache.Add(d, returnValue);
                    }

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

        #region Match

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

            if (MatchPointer(d, value, methodComponent, onSuccess))
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
                if (classTypeWildcards != null)
                {
                    if (classTypeWildcards.Any(t => t.IsMatch(c.Name)))
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

        private bool MatchPointer(IMethodFrameDetailed d, object value, object methodComponent, Action onSuccess)
        {
            if (value is PtrValue ptr)
            {
                if (MatchAny(d, ptr.Value, methodComponent, onSuccess))
                {
                    AddMatchedValue(ptr.Value, methodComponent);
                    return true;
                }
            }

            return false;
        }

        #endregion

        public void Dispose()
        {
            MethodFrameDetailed.ClearCaches();
        }
    }
}
