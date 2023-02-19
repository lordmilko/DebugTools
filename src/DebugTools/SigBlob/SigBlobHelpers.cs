using System;
using ClrDebug;

namespace DebugTools
{
    static class SigBlobHelpers
    {
        internal static unsafe CorHybridCallingConvention CorSigUncompressCallingConv(ref IntPtr pData)
        {
            var ptr = (byte*)pData;
            var conv = (CorHybridCallingConvention)(*ptr++);
            pData = (IntPtr)ptr;
            return conv;
        }

        internal static unsafe int CorSigUncompressData(ref IntPtr pData)
        {
            byte* pBytes = (byte*)pData;

            if ((*pBytes & 0x80) == 0x00)
            {
                var result = (int)(*pBytes++);
                pData = (IntPtr)pBytes;
                return result;
            }

            return CorSigUncompressBigData(ref pData);
        }

        internal static unsafe int CorSigUncompressBigData(ref IntPtr pData)
        {
            byte* pBytes = (byte*)pData;
            int result;

            if ((*pBytes & 0xC0) == 0x80)
            {
                result = ((*pBytes++ & 0x3f) << 8);
                result |= *pBytes++;
            }
            else
            {
                result = (*pBytes++ & 0x1f) << 24;
                result |= (*pBytes++) << 16;
                result |= (*pBytes++) << 8;
                result |= (*pBytes++);
            }

            pData = (IntPtr)pBytes;
            return result;
        }

        private enum SignMasks : uint
        {
            ONEBYTE = 0xffffffc0,         // Mask the same size as the missing bits.  
            TWOBYTE = 0xffffe000,         // Mask the same size as the missing bits.  
            FOURBYTE = 0xf0000000,        // Mask the same size as the missing bits.  
        }

        internal static int CorSigUncompressSignedInt(IntPtr pData, out int pInt)
        {
            int cb;
            uint ulSigned;
            uint iData;

            cb = CorSigUncompressData(pData, out iData);
            pInt = 0;
            if (cb == -1)
                return cb;
            ulSigned = iData & 0x1;
            iData = iData >> 1;
            if (ulSigned != 0)
            {
                if (cb == 1)
                {
                    iData |= (uint)SignMasks.ONEBYTE;
                }
                else if (cb == 2)
                {
                    iData |= (uint)SignMasks.TWOBYTE;
                }
                else
                {
                    iData |= (uint)SignMasks.FOURBYTE;
                }
            }
            pInt = (int)iData;
            return cb;
        }

        internal static unsafe int CorSigUncompressData(IntPtr pData, out uint pDataOut)
        {
            int cb = -1;
            byte* pBytes = (byte*)(pData);
            pDataOut = 0;

            // Smallest.    
            if ((*pBytes & 0x80) == 0x00)
            {
                pDataOut = *pBytes;
                cb = 1;
            }
            // Medium.  
            else if ((*pBytes & 0xC0) == 0x80)
            {
                pDataOut = (uint)(((*pBytes & 0x3f) << 8 | *(pBytes + 1)));
                cb = 2;
            }
            else if ((*pBytes & 0xE0) == 0xC0)
            {
                pDataOut = (uint)(((*pBytes & 0x1f) << 24 | *(pBytes + 1) << 16 | *(pBytes + 2) << 8 | *(pBytes + 3)));
                cb = 4;
            }
            return cb;
        }

        internal static unsafe CorElementType CorSigUncompressElementType(ref IntPtr pData)
        {
            var ptr = (byte*)pData;
            var type = (CorElementType)(*ptr++);
            pData = (IntPtr)ptr;
            return type;
        }

        static CorTokenType[] g_tkCorEncodeToken =
        {
            CorTokenType.mdtTypeDef,
            CorTokenType.mdtTypeRef,
            CorTokenType.mdtTypeSpec,
            CorTokenType.mdtBaseType
        };

        internal static mdToken CorSigUncompressToken(ref IntPtr pData)
        {
            mdToken tk;
            CorTokenType tkType;

            tk = CorSigUncompressData(ref pData);
            tkType = g_tkCorEncodeToken[tk & 0x3];
            tk = TokenFromRid(tk >> 2, tkType);
            return tk;
        }

        private static mdToken TokenFromRid(int rid, CorTokenType tktype) => rid | (int)tktype;
    }
}
