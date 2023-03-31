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

        unsafe SigMethodDef IMethodInfoDetailed.SigMethod
        {
            get
            {
                if (sigMethod == null)
                {
                    var props = GetMDI().GetMethodProps(Token);

                    var reader = new SigReader(props.ppvSigBlob, props.pcbSigBlob, Token, GetMDI());

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
                var dispenser = new MetaDataDispenserEx();

                mdi = dispenser.OpenScope<MetaDataImport>(ModulePath, CorOpenFlags.ofReadOnly);

                mdiCache[ModulePath] = mdi;
            }

            return mdi;
        }
    }
}
