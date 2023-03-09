using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools
{
    public class SigType
    {
        public static readonly SigType Sentinel = new SigType(CorElementType.Sentinel);

        public CorElementType Type { get; }

        internal static SigType New(ref SigReader reader)
        {
            var elementType = reader.CorSigUncompressElementType();

            switch (elementType)
            {
                #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

                case CorElementType.Boolean:
                case CorElementType.Char:
                case CorElementType.I1:
                case CorElementType.U1:
                case CorElementType.I2:
                case CorElementType.U2:
                case CorElementType.I4:
                case CorElementType.U4:
                case CorElementType.I8:
                case CorElementType.U8:
                case CorElementType.R4:
                case CorElementType.R8:
                case CorElementType.I:
                case CorElementType.U:
                    return new SigType(elementType);

                #endregion
                #region ARRAY Type ArrayShape (general array, see §II.23.2.13)

                case CorElementType.Array:
                    return new SigArrayType(elementType, ref reader);

                #endregion
                #region CLASS TypeDefOrRefOrSpecEncoded | VALUETYPE TypeDefOrRefOrSpecEncoded

                case CorElementType.Class:
                    return new SigClassType(elementType, ref reader);

                case CorElementType.ValueType:
                    return new SigValueType(elementType, ref reader);

                #endregion
                #region FNPTR MethodDefSig | FNPTR MethodRefSig

                case CorElementType.FnPtr:
                    return new SigFnPtrType(elementType, ref reader);

                #endregion
                #region GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type*

                case CorElementType.GenericInst:
                    return NewGenericType(ref reader);

                #endregion
                #region MVAR number | VAR number

                case CorElementType.MVar:
                    return new SigMethodGenericArgType(elementType, ref reader);

                case CorElementType.Var:
                    return new SigTypeGenericArgType(elementType, ref reader);

                #endregion
                #region OBJECT | STRING

                case CorElementType.Object:
                case CorElementType.String:
                    return new SigType(elementType);

                #endregion
                #region PTR CustomMod* Type | PTR CustomMod* VOID

                case CorElementType.Ptr:
                    return new SigPtrType(elementType, ref reader);

                #endregion
                #region SZARRAY CustomMod* Type (single dimensional, zero-based array i.e., vector)

                case CorElementType.SZArray:
                    return new SigSZArrayType(elementType, ref reader);

                #endregion

                case CorElementType.ByRef:
                    return new SigRefType(ref reader);

                //A RetType includes either a [ByRef] Type / TypedByRef / Void
                case CorElementType.Void:
                case CorElementType.TypedByRef:
                    return new SigType(elementType);

                //Contrary to the original ECMA 335 spec, this can occur before or after ByRef
                //https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md#4-byref-can-come-before-custom-modifiers
                case CorElementType.CModReqd:
                case CorElementType.CModOpt:
                    return new SigModType(elementType, ref reader);

                case CorElementType.Sentinel:
                    return SigType.Sentinel;

                default:
                    throw new NotImplementedException($"Don't know how to handle type '{elementType}'");
            }
        }

        private static SigType NewGenericType(ref SigReader reader)
        {
            var type = reader.CorSigUncompressElementType();

            switch (type)
            {
                case CorElementType.Class:
                case CorElementType.ValueType:
                    return new SigGenericType(type, ref reader);

                default:
                    throw new NotImplementedException($"Don't know how to create generic type for type '{type}'.");
            }
        }

        protected SigType(CorElementType type)
        {
            Type = type;
        }

        #region Metadata

        protected string GetName(mdToken token, MetaDataImport import)
        {
            switch (token.Type)
            {
                case CorTokenType.mdtTypeDef:
                    return import.GetTypeDefProps((mdTypeDef)token).szTypeDef;

                case CorTokenType.mdtTypeRef:
                    return import.GetTypeRefProps((mdTypeRef)token).szName;

                default:
                    throw new NotImplementedException($"Don't know how to get name for token of type '{token.Type}'.");
            }
        }

        public string GetMethodGenericArgName(int index, mdMethodDef mdMethodDef, MetaDataImport import)
        {
            var genericParams = import.EnumGenericParams(mdMethodDef);

            foreach (var genericParam in genericParams)
            {
                var props = import.GetGenericParamProps(genericParam);

                if (props.pulParamSeq == index)
                    return props.wzname;
            }

            throw new InvalidOperationException($"Cannot find method generic parameter {index}");
        }

        protected string GetTypeGenericArgName(int index, mdMethodDef mdMethodDef, MetaDataImport import)
        {
            var typeDef = import.GetMethodProps(mdMethodDef).pClass;

            //We probably shouldn't be enumerating all the properties every single time we want to get another generic arg's name.
            //If anything, we should perhaps be building a typed data hierarchy, as seen with the DbgMetaDataType type in the MixedModeDebugger sample
            var genericParams = import.EnumGenericParams(typeDef);

            foreach (var genericParam in genericParams)
            {
                var props = import.GetGenericParamProps(genericParam);

                if (props.pulParamSeq == index)
                    return props.wzname;
            }

            throw new InvalidOperationException($"Cannot find type generic parameter {index}");
        }

        #endregion

        public override string ToString()
        {
            switch (Type)
            {
                case CorElementType.Boolean:
                    return "bool";
                case CorElementType.Char:
                    return "char";
                case CorElementType.I1:
                    return "sbyte";
                case CorElementType.U1:
                    return "byte";
                case CorElementType.I2:
                    return "short";
                case CorElementType.U2:
                    return "ushort";
                case CorElementType.I4:
                    return "int";
                case CorElementType.U4:
                    return "uint";
                case CorElementType.I8:
                    return "long";
                case CorElementType.U8:
                    return "ulong";
                case CorElementType.R4:
                    return "float";
                case CorElementType.R8:
                    return "double";
                case CorElementType.I:
                    return "IntPtr";
                case CorElementType.U:
                    return "UIntPtr";

                case CorElementType.Object:
                    return "object";
                case CorElementType.String:
                    return "string";
                case CorElementType.Void:
                    return "void";
                default:
                    return Type.ToString();
            }
        }
    }
}
