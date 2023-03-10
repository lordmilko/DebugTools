using System;
using System.IO;
using System.Runtime.InteropServices;
using ClrDebug;
using DebugTools;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodInfoDetailed : IMethodInfoDetailed
    {
        public FunctionID FunctionID { get; }
        public string ModuleName { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public bool WasUnknown { get; set; }

        public SigMethodDef SigMethod { get; }

        private System.Reflection.MethodInfo realMethodInfo;

        private MetaDataImport import;

        public MockMethodInfoDetailed(System.Reflection.MethodInfo methodInfo)
        {
            realMethodInfo = methodInfo;

            var disp = new MetaDataDispenserEx();

            import = disp.OpenScope<MetaDataImport>(methodInfo.DeclaringType.Assembly.Location, CorOpenFlags.ofRead);

            ModuleName = Path.GetFileName(methodInfo.DeclaringType.Assembly.Location);
            TypeName = methodInfo.DeclaringType.FullName;
            MethodName = methodInfo.Name;
            SigMethod = MakeSigMethodDef(methodInfo);
        }

        private unsafe SigMethodDef MakeSigMethodDef(System.Reflection.MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return null;

            var methodDef = (mdMethodDef) methodInfo.MetadataToken;

            var props = import.GetMethodProps(methodDef);
            var sigBlob = new byte[props.pcbSigBlob];
            Marshal.Copy(props.ppvSigBlob, sigBlob, 0, props.pcbSigBlob);

            fixed (byte* ptr = sigBlob)
            {
                var reader = new SigReader((IntPtr)ptr, sigBlob.Length, methodDef, import);

                var sigMethod = (SigMethodDef)reader.ParseMethod(MethodName, true);

                return sigMethod;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is MockMethodInfoDetailed m)
                return m.realMethodInfo.Equals(realMethodInfo);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return realMethodInfo.GetHashCode();
        }
    }
}
