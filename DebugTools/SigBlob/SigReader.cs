using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug;

namespace DebugTools
{
    struct SigReader
    {
        private static ISigParameter[] NoParams = new ISigParameter[0];

        private IntPtr currentSigBlob;
        internal IntPtr originalSigBlob;

        internal int Length { get; }

        internal int Read => (int) (currentSigBlob.ToInt64() - originalSigBlob.ToInt64());

        internal bool Completed => Read == Length;

        internal MetaDataImport Import { get; }

        internal mdMethodDef MethodDef { get; }

        internal SigReader(IntPtr sigBlob, int sigBlobLength, mdMethodDef methodDef, MetaDataImport import)
        {
            currentSigBlob = sigBlob;
            originalSigBlob = sigBlob;
            Length = sigBlobLength;
            MethodDef = methodDef;
            Import = import;
        }

        public SigMethod ParseSigMethodDefOrRef(string name, bool topLevel)
        {
            string[] genericTypeArgs = null;

            //The first byte of the Signature holds bits for HASTHIS, EXPLICITTHIS and calling convention (DEFAULT,
            //VARARG, or GENERIC). These are ORed together.
            var callingConvention = CorSigUncompressCallingConv();

            if ((callingConvention & CorCallingConvention.GENERIC) != 0)
            {
                var genParamCount = CorSigUncompressData();
                var genericParams = Import.EnumGenericParams(MethodDef);

                var list = new List<string>();

                foreach (var genericParam in genericParams)
                {
                    list.Add(Import.GetGenericParamProps(genericParam).wzname);
                }

                genericTypeArgs = list.ToArray();
            }

            var paramCount = CorSigUncompressData();

            var retType = SigType.New(ref this);

            var methodParams = ParseSigMethodParams(paramCount, topLevel);

            return new SigMethodDef(name, callingConvention, retType, methodParams, genericTypeArgs);
        }

        private ISigParameter[] ParseSigMethodParams(int sigParamCount, bool topLevel)
        {
            if (sigParamCount == 0)
                return NoParams;

            GetParamPropsResult[] metaDataParameters = null;

            if (topLevel)
            {
                var import = Import;
                metaDataParameters = Import.EnumParams(MethodDef).Select(p => import.GetParamProps(p)).ToArray();
            }
            
            var list = new List<ISigParameter>();

            for (var i = 0; i < sigParamCount; i++)
            {
                var sigType = SigType.New(ref this);

                if (topLevel)
                {
                    GetParamPropsResult metaDataParam = default;

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
                    list.Add(new SigFnPtrParameter(sigType));
            }

            return list.ToArray();

        }

        #region CorSigUncompress*

        internal CorCallingConvention CorSigUncompressCallingConv() => SigBlobHelpers.CorSigUncompressCallingConv(ref currentSigBlob);
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