using ClrDebug;

namespace DebugTools.Profiler
{
    public interface IMethodInfoDetailed : IMethodInfo
    {
        SigMethodDef SigMethod { get; }

        MetaDataImport GetMDI();
    }
}
