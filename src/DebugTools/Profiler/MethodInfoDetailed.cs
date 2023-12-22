using System;
using System.Collections.Concurrent;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class MethodInfoDetailed : MethodInfo, IMethodInfoDetailed
    {
        private static ConcurrentDictionary<string, MetaDataImport> mdiCache = new ConcurrentDictionary<string, MetaDataImport>();

        public mdMethodDef Token { get; }

        private SigMethodDef sigMethod;

        SigMethodDef IMethodInfoDetailed.SigMethod
        {
            get
            {
                if (sigMethod == null)
                {
                    var mdi = GetMDI();

                    if (mdi == null)
                        return null;

                    var props = GetMDI().GetMethodProps(Token);

                    var reader = new SigReader(props.ppvSigBlob, props.pcbSigBlob, Token, mdi);

                    sigMethod = (SigMethodDef)reader.ParseMethod(MethodName, true);
                }

                return sigMethod;
            }
        }

        public MethodInfoDetailed(FunctionID functionId, string modulePath, string typeName, string methodName, mdMethodDef token) : base(functionId, modulePath, typeName, methodName)
        {
            Token = token;
        }

        private MetaDataImport GetMDI()
        {
            if (!mdiCache.TryGetValue(ModulePath, out var mdi))
            {
                //I'm not sure whether we should use the same dispenser for every module, or create a new one for each one. I guess this is safer?
                var dispenser = new MetaDataDispenserEx();

                //If it succeeds, we get an MDI. Otherwise, we get null. We only expect to see failure on dynamic modules
                dispenser.TryOpenScope(ModulePath, CorOpenFlags.ofReadOnly, out mdi);

                mdiCache[ModulePath] = mdi;
            }

            return mdi;
        }
    }
}
