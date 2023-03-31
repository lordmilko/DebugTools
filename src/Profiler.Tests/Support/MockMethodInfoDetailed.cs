using System;
using System.IO;
using ClrDebug;
using DebugTools;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodInfoDetailed : IMethodInfoDetailed
    {
        public FunctionID FunctionID { get; }
        public string ModulePath { get; }
        public string ModuleName => Path.GetFileName(ModulePath);
        public string TypeName { get; }
        public string MethodName { get; }
        public bool WasUnknown { get; set; }

        public mdMethodDef Token { get; }
        public SigMethodDef SigMethod { get; }

        private System.Reflection.MethodInfo realMethodInfo;

        private MetaDataImport import;

        public MockMethodInfoDetailed(System.Reflection.MethodInfo methodInfo)
        {
            FunctionID = methodInfo.Name.GetHashCode();

            realMethodInfo = methodInfo;

            var disp = new MetaDataDispenserEx();

            import = disp.OpenScope<MetaDataImport>(methodInfo.DeclaringType.Assembly.Location, CorOpenFlags.ofRead);

            ModulePath = methodInfo.DeclaringType.Assembly.Location;
            TypeName = methodInfo.DeclaringType.FullName;

            if (TryGetInterface(methodInfo, out var iface) && iface == typeof(IDisposable))
                MethodName = iface.FullName + "." + methodInfo.Name;
            else
                MethodName = methodInfo.Name;

            var result = MakeSigMethodDef(methodInfo);

            SigMethod = result.Item1;
            Token = result.Item2;
        }

        private bool TryGetInterface(System.Reflection.MethodInfo method, out Type type)
        {
            foreach (var iface in method.DeclaringType.GetInterfaces())
            {
                var map = method.DeclaringType.GetInterfaceMap(iface);

                foreach(var item in map.TargetMethods)
                {
                    if (method == item)
                    {
                        type = iface;
                        return true;
                    }
                }
            }

            type = null;
            return false;
        }

        private unsafe (SigMethodDef, mdMethodDef) MakeSigMethodDef(System.Reflection.MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return default;

            var methodDef = (mdMethodDef) methodInfo.MetadataToken;

            var props = import.GetMethodProps(methodDef);

            var reader = new SigReader(props.ppvSigBlob, props.pcbSigBlob, methodDef, import);

            var sigMethod = (SigMethodDef)reader.ParseMethod(MethodName, true);

            return (sigMethod, methodDef);
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
