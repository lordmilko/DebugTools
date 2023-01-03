using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class MethodFrameDetailed : MethodFrame
    {
        internal byte[] EnterValue { get; set; }
        internal byte[] ExitValue { get; set; }

        private List<object> EnterParameters
        {
            get
            {
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
            return MethodFrameFormatter.Default.ToString(this);
        }
    }
}
