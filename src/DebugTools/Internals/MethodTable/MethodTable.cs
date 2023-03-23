using System;
using DebugTools.MethodTableFlags;

namespace DebugTools
{
    //Some useful info on generics: https://yizhang82.dev/dotnet-generics-typeof-t
    [Serializable]
    public struct MethodTable
    {
#pragma warning disable CS0169
#pragma warning disable CS0649

        /// <summary>
        /// Low WORD is component size for array and string types (HasComponentSize() returns true). Used for flags otherwise.
        /// </summary>
        private readonly int m_dwFlags;
        private readonly int m_BaseSize;
        private readonly short m_wFlags2;

#pragma warning restore CS0169
#pragma warning restore CS0649

        public bool HasClassConstructor => GetFlag(WFLAGS2_ENUM.enum_flag_HasCctor) != 0;

        public bool HasComponentSize => GetFlag(WFLAGS_HIGH_ENUM.enum_flag_HasComponentSize) != 0;

        public bool HasDefaultConstructor => GetFlag(WFLAGS_LOW_ENUM.enum_flag_HasDefaultCtor) != 0;

        public bool HasInstantiation => !TestFlagWithMask(WFLAGS_LOW_ENUM.enum_flag_GenericsMask, WFLAGS_LOW_ENUM.enum_flag_GenericsMask_NonGeneric);

        public bool IsArray => GetFlag(WFLAGS_HIGH_ENUM.enum_flag_Category_Array_Mask) == (int)WFLAGS_HIGH_ENUM.enum_flag_Category_Array;

        public bool IsNullable => (int)GetFlag(WFLAGS_HIGH_ENUM.enum_flag_Category_Mask) == (int)WFLAGS_HIGH_ENUM.enum_flag_Category_Nullable;

        public bool IsSharedByGenericInstantiations => TestFlagWithMask(WFLAGS_LOW_ENUM.enum_flag_GenericsMask, WFLAGS_LOW_ENUM.enum_flag_GenericsMask_SharedInst);

        public bool IsStringOrArray => HasComponentSize;

        public bool IsTruePrimitive => GetFlag(WFLAGS_HIGH_ENUM.enum_flag_Category_Mask) == (int) WFLAGS_HIGH_ENUM.enum_flag_Category_TruePrimitive;

        public bool IsValueType => GetFlag(WFLAGS_HIGH_ENUM.enum_flag_Category_ValueType_Mask) == (int) WFLAGS_HIGH_ENUM.enum_flag_Category_ValueType;

        private int GetFlag(WFLAGS_HIGH_ENUM flag)
        {
            return m_dwFlags & (int)flag;
        }

        int GetFlag(WFLAGS_LOW_ENUM flag)
        {
            return (IsStringOrArray ? ((int)WFLAGS_LOW_ENUM.enum_flag_StringArrayValues & (int)flag) : (m_dwFlags & (int)flag));
        }

        private bool TestFlagWithMask(WFLAGS_LOW_ENUM mask, WFLAGS_LOW_ENUM flag)
        {
            return (IsStringOrArray ? (((int)WFLAGS_LOW_ENUM.enum_flag_StringArrayValues & (int) mask) == (int) flag) :
                ((m_dwFlags & (int) mask) == (int) flag));
        }

        private int GetFlag(WFLAGS2_ENUM flag)
        {
            return m_wFlags2 & (int)flag;
        }
    }
}
