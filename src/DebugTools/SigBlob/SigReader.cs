using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug;

namespace DebugTools
{
    public struct SigReader
    {
        private IntPtr currentSigBlob;
        internal IntPtr originalSigBlob;

        internal int Length { get; }

        internal int Read => (int) (currentSigBlob.ToInt64() - originalSigBlob.ToInt64());

        internal bool Completed => Read == Length;

        internal MetaDataImport Import { get; }

        internal mdToken Token { get; }

        public SigReader(IntPtr sigBlob, int sigBlobLength, mdToken token, MetaDataImport import)
        {
            currentSigBlob = sigBlob;
            originalSigBlob = sigBlob;
            Length = sigBlobLength;
            Token = token;
            Import = import;
        }

        public SigMethod ParseMethod(string name, bool topLevel)
        {
            string[] genericTypeArgs = null;

            //The first byte of the Signature holds bits for HASTHIS, EXPLICITTHIS and calling convention (DEFAULT,
            //VARARG, or GENERIC). These are ORed together.
            var callingConvention = (CallingConvention) CorSigUncompressCallingConv();

            if (callingConvention.IsGeneric)
            {
                var genParamCount = CorSigUncompressData();

                if (Token.Type == CorTokenType.mdtModule)
                {
                    var genericParams = Import.EnumGenericParams((mdMethodDef) Token);

                    var list = new List<string>();

                    foreach (var genericParam in genericParams)
                    {
                        list.Add(Import.GetGenericParamProps(genericParam).wzname);
                    }

                    genericTypeArgs = list.ToArray();
                }
            }

            var paramCount = CorSigUncompressData();

            var retType = SigType.New(ref this);

            var methodParams = ParseSigMethodParams(paramCount, topLevel, callingConvention);

            if (callingConvention.IsVarArg && Token.Type == CorTokenType.mdtMethodDef)
                methodParams.normal.Add(new SigArgListParameter());

            if (methodParams.vararg == null || methodParams.vararg.Count == 0)
                return new SigMethodDef(name, callingConvention, retType, methodParams.normal.ToArray(), genericTypeArgs);
            else
                return new SigMethodRef(name, callingConvention, retType, methodParams.normal.ToArray(), methodParams.vararg.ToArray());
        }

        public SigType ParseField()
        {
            var callingConvention = (CallingConvention)CorSigUncompressCallingConv();

            var type = SigType.New(ref this);

            return type;
        }

        private (List<ISigParameter> normal, List<ISigParameter> vararg) ParseSigMethodParams(int sigParamCount, bool topLevel, CallingConvention callingConvention)
        {
            if (sigParamCount == 0)
                return (new List<ISigParameter>(), null);

            GetParamPropsResult[] metaDataParameters = null;

            if (topLevel && Token.Type == CorTokenType.mdtMethodDef)
            {
                var import = Import;
                metaDataParameters = Import.EnumParams((mdMethodDef) Token).Select(p => import.GetParamProps(p)).ToArray();
            }
            
            var normal = new List<ISigParameter>();
            var varargs = new List<ISigParameter>();

            var list = normal;
            bool haveSentinel = false;

            for (var i = 0; i < sigParamCount; i++)
            {
                var sigType = SigType.New(ref this);

                if (sigType == SigType.Sentinel)
                {
                    list = varargs;
                    haveSentinel = true;
                    sigType = SigType.New(ref this);
                }

                if (haveSentinel)
                {
                    list.Add(new SigVarArgParameter(sigType));
                    continue;
                }

                if (topLevel)
                {
                    if (Token.Type == CorTokenType.mdtMethodDef)
                    {
                        GetParamPropsResult metaDataParam = default(GetParamPropsResult);

                        if (i < metaDataParameters.Length)
                        {
                            metaDataParam = metaDataParameters[i];

                            if (metaDataParam.pulSequence != i + 1)
                                metaDataParam = metaDataParameters.FirstOrDefault(p => p.pulSequence == i + 1);
                        }
                        else
                        {
                            if (metaDataParameters.Length > 0)
                                metaDataParam = metaDataParameters.FirstOrDefault(p => p.pulSequence == i + 1);
                        }

                        list.Add(new SigParameter(sigType, metaDataParam.Equals(default(GetParamPropsResult)) ? null : (GetParamPropsResult?) metaDataParam));
                    }
                    else
                        list.Add(new SigParameter(sigType, null));
                }
                else
                    list.Add(new SigFnPtrParameter(sigType));
            }

            return (normal, varargs);

        }

        #region CorSigUncompress*

        internal CorHybridCallingConvention CorSigUncompressCallingConv() => SigBlobHelpers.CorSigUncompressCallingConv(ref currentSigBlob);
        internal int CorSigUncompressData() => SigBlobHelpers.CorSigUncompressData(ref currentSigBlob);

        internal int CorSigUncompressSignedInt()
        {
            var bytes = SigBlobHelpers.CorSigUncompressSignedInt(currentSigBlob, out var result);

            if (bytes > 0)
                currentSigBlob += bytes;

            return result;
        }
        internal CorElementType CorSigUncompressElementType() => SigBlobHelpers.CorSigUncompressElementType(ref currentSigBlob);
        internal mdToken CorSigUncompressToken() => SigBlobHelpers.CorSigUncompressToken(ref currentSigBlob);

        #endregion
    }
}
