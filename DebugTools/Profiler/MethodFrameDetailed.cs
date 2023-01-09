using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClrDebug;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    public class MethodFrameDetailed : MethodFrame, IMethodFrameDetailed
    {
        //Conditional weak table is thread safe
        internal static ConditionalWeakTable<IMethodFrameDetailed, List<object>> ParameterCache = new ConditionalWeakTable<IMethodFrameDetailed, List<object>>();
        internal static ConditionalWeakTable<IMethodFrameDetailed, object> ReturnCache = new ConditionalWeakTable<IMethodFrameDetailed, object>();

        public static void ClearCaches()
        {
            ParameterCache = new ConditionalWeakTable<IMethodFrameDetailed, List<object>>();
            ReturnCache = new ConditionalWeakTable<IMethodFrameDetailed, object>();
        }

        internal byte[] EnterValue { get; set; }
        internal byte[] ExitValue { get; set; }

        private List<object> EnterParameters
        {
            get
            {
                if (ParameterCache.TryGetValue(this, out var value))
                    return value;

                if (EnterValue == null)
                    return null;

                var result = ValueSerializer.FromParameters(EnterValue);

                return result;
            }
        }

        private object ExitResult
        {
            get
            {
                if (ReturnCache.TryGetValue(this, out var value))
                    return value;

                if (ExitValue == null)
                    return null;

                var result = ValueSerializer.FromReturnValue(ExitValue);

                return result;
            }
        }

        public MethodFrameDetailed(IMethodInfo methodInfo, CallDetailedArgs args) : base(methodInfo, args.Sequence)
        {
            EnterValue = args.HRESULT == HRESULT.S_OK ? args.Value : null;
        }

        protected MethodFrameDetailed(IFrame newParent, MethodFrameDetailed originalFrame) : base(newParent, originalFrame)
        {
            EnterValue = originalFrame.EnterValue;
            ExitValue = originalFrame.ExitValue;
        }

        public List<object> GetEnterParameters() => EnterParameters;

        public object GetExitResult() => ExitResult;

        public override IMethodFrame CloneWithNewParent(IFrame newParent)
        {
            return new MethodFrameDetailed(newParent, this);
        }

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
