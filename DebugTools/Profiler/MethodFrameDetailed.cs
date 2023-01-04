using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DebugTools.Profiler
{
    public class MethodFrameDetailed : MethodFrame
    {
        //Conditional weak table is thread safe
        internal static ConditionalWeakTable<MethodFrameDetailed, List<object>> ParameterCache = new ConditionalWeakTable<MethodFrameDetailed, List<object>>();
        internal static ConditionalWeakTable<MethodFrameDetailed, object> ReturnCache = new ConditionalWeakTable<MethodFrameDetailed, object>();

        public static void ClearCaches()
        {
            ParameterCache = new ConditionalWeakTable<MethodFrameDetailed, List<object>>();
            ReturnCache = new ConditionalWeakTable<MethodFrameDetailed, object>();
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

        public List<object> GetEnterParameters() => EnterParameters;

        public object GetExitResult() => ExitResult;

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
