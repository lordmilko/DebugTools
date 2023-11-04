using System.Reflection;

namespace DebugTools.Dynamic
{
    interface IMethodMatcher
    {
        MethodInfo Method { get; }
    }

    struct MethodInfoAndParameterRanks : IMethodMatcher
    {
        public MethodInfo Method { get; }

        public ParameterInfoAndRank[] Parameters { get; }

        public MethodInfoAndParameterRanks(MethodInfo method, ParameterInfoAndRank[] parameters)
        {
            Method = method;
            Parameters = parameters;
        }
    }
}
