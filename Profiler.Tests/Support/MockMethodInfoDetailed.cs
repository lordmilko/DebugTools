﻿using System;
using System.Runtime.InteropServices;
using ClrDebug;
using DebugTools;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodInfoDetailed : IMethodInfoDetailed
    {
        public long FunctionID { get; }
        public string ModuleName { get; }
        public string TypeName { get; }
        public string MethodName { get; }

        public SigMethodDef SigMethod { get; }

        private MetaDataImport import;

        public MockMethodInfoDetailed(System.Reflection.MethodInfo methodInfo)
        {
            TypeName = methodInfo?.DeclaringType.Name;
            MethodName = methodInfo?.Name;
            SigMethod = MakeSigMethodDef(methodInfo);
        }

        private unsafe SigMethodDef MakeSigMethodDef(System.Reflection.MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return null;

            var import = GetMDI();

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

        public MetaDataImport GetMDI()
        {
            if (import == null)
            {
                var disp = new MetaDataDispenserEx();

                import = disp.OpenScope<MetaDataImport>(GetType().Assembly.Location, CorOpenFlags.ofRead);
            }

            return import;
        }
    }
}
