using System;
using System.Collections.Concurrent;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class MethodInfoDetailed : MethodInfo
    {
        private static ConcurrentDictionary<string, MetaDataImport> mdiCache = new ConcurrentDictionary<string, MetaDataImport>();

        public mdMethodDef Token { get; }

        public byte[] SigBlob { get; }

        public int SigBlobLength { get; }

        private SigMethodDef sigMethod;

        public unsafe SigMethodDef SigMethod
        {
            get
            {
                if (sigMethod == null)
                {
                    fixed (byte* ptr = SigBlob)
                    {
                        var reader = new SigReader((IntPtr) ptr, SigBlobLength, Token, GetMDI());

                        sigMethod = (SigMethodDef) reader.ParseSigMethodDefOrRef(MethodName, true);
                    }
                }

                return sigMethod;
            }
        }

        public MethodInfoDetailed(long functionId, string modulePath, string typeName, string methodName, mdMethodDef token, byte[] sigBlob, int sigBlobLength) : base(functionId, modulePath, typeName, methodName)
        {
            Token = token;
            SigBlob = sigBlob;
            SigBlobLength = sigBlobLength;

            var a = SigMethod;
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