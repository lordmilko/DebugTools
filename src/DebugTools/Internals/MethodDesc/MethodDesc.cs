using System;
using System.Runtime.InteropServices;
using DebugTools.MethodDescFlags;

namespace DebugTools
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct MethodDesc
    {
        public const int MethodDescSize = 8;
        public static int InstantiatedMethodDescSize => MethodDescSize + Marshal.SizeOf<InstantiatedMethodDesc_Fragment>();

        #region Fields

        [FieldOffset(0)]
        private readonly ushort m_wFlags3AndTokenRemainder;

        [FieldOffset(2)]
        private readonly byte m_chunkIndex;

        [FieldOffset(3)]
        private readonly byte m_bFlags2;

        // The slot number of this MethodDesc in the vtable array.
        // Note that we may store other information in the high bits if available --
        // see enum_packedSlotLayout and mdcRequiresFullSlotNumber for details.
        [FieldOffset(4)]
        private readonly short m_wSlotNumber;

        [FieldOffset(6)]
        private readonly short m_wFlags;

        //All fields below here may not exist on a given MethodDesc

        /// <summary>
        /// Stores the trailing members that would exist on this type when it is an InstantiatedMethodDesc.
        /// </summary>
        [FieldOffset(8)]
        private InstantiatedMethodDesc_Fragment InstantiatedMethodDesc;

        #endregion
        #region Properties (MethodDesc)

        /*public bool AcquiresInstMethodTableFromThis
        {
            get
            {
                return IsSharedByGenericInstantiations &&
                    !HasMethodInstantiation &&
                    !IsStatic &&
                    !methodTable.IsValueType &&
                    !(methodTable.IsInterface && !IsAbstract); //todo: IsAbstract requires metadata
            }
        }*/

        public MethodClassification Classification => (MethodClassification)(m_wFlags & (int)MethodDescClassification.mdcClassification);

        //public bool HasClassInstantiation => methodTable.HasInstantiation;

        public bool HasMethodImplSlot => ((int)MethodDescClassification.mdcMethodImpl & m_wFlags) != 0;

        public bool HasMethodInstantiation => Classification == MethodClassification.mcInstantiated && IMD_HasMethodInstantiation.Value;

        public bool IsIL => MethodClassification.mcIL == Classification || MethodClassification.mcInstantiated == Classification;

        public bool IsInstantiatingStub => Classification == MethodClassification.mcInstantiated
                                           && !IsUnboxingStub
                                           && IMD_IsWrapperStubWithInstantiations.Value;

        public bool IsGenericMethodDefinition => Classification == MethodClassification.mcInstantiated && IMD_IsGenericMethodDefinition.Value;

        public bool IsRuntimeMethodHandle => !HasMethodInstantiation || !IsSharedByGenericMethodInstantiations;

        /*public bool IsSharedByGenericInstantiations
        {
            get
            {
                if (IsWrapperStub)
                    return false;

                if (methodTable.IsSharedByGenericInstantiations)
                    return true;

                return IsSharedByGenericMethodInstantiations;
            }
        }*/

        public bool IsSharedByGenericMethodInstantiations
        {
            get
            {
                if (Classification == MethodClassification.mcInstantiated)
                    return IMD_IsSharedByGenericMethodInstantiations.Value;

                return false;
            }
        }

        public bool IsStatic => (m_wFlags & (int)MethodDescClassification.mdcStatic) != 0;

        /*public bool IsTypicalMethodDefinition
        {
            get
            {
                if (HasMethodInstantiation && !IsGenericMethodDefinition)
                    return false;

                if (HasClassInstantiation && !methodTable.IsGenericTypeDefinition)
                    return false;

                return true;
            }
        }*/

        public bool IsUnboxingStub => (m_bFlags2 & (int)enum_flag2.enum_flag2_IsUnboxingStub) != 0;

        public bool IsWrapperStub => IsUnboxingStub || IsInstantiatingStub;

        public bool RequiresFullSlotNumber => (m_wFlags & (int)MethodDescClassification.mdcRequiresFullSlotNumber) != 0;

        /*public bool RequiresInstArg
        {
            get
            {
                return IsSharedByGenericInstantiations &&
                       (HasMethodInstantiation || IsStatic || methodTable.IsValueType || (methodTable.IsInterface && !IsAbstract)); //todo: IsAbstract requires metadata
            }
        }

        public bool RequiresInstMethodDescArg => IsSharedByGenericInstantiations && HasMethodInstantiation;

        public bool RequiresInstMethodTableArg
        {
            get
            {
                return IsSharedByGenericInstantiations &&
                       !HasMethodInstantiation &&
                       (IsStatic || methodTable.IsValueType || (methodTable.IsInterface && !IsAbstract)); //todo: IsAbstract requires metadata
            }
        }*/

        public short Slot
        {
            get
            {
                if (!RequiresFullSlotNumber)
                    return (short)(m_wSlotNumber & (short)enum_packedSlotLayout.enum_packedSlotLayout_SlotMask);

                return m_wSlotNumber;
            }
        }

        #endregion
        #region Properties (InstantiatedMethodDesc)

        public bool? IMD_HasMethodInstantiation
        {
            get
            {
                if (Classification == MethodClassification.mcInstantiated)
                {
                    if (IMD_IsGenericMethodDefinition.Value)
                        return true;

                    return InstantiatedMethodDesc.m_pPerInstInfo != IntPtr.Zero;
                }

                return null;
            }
        }

        public bool? IMD_IsGenericMethodDefinition
        {
            get
            {
                if (Classification == MethodClassification.mcInstantiated)
                    return ((InstantiatedMethodDesc.m_wFlags2 & (int)InstantiatedMethodDescFlags.KindMask) == (int)InstantiatedMethodDescFlags.GenericMethodDefinition);

                return null;
            }
        }

        public bool? IMD_IsSharedByGenericMethodInstantiations
        {
            get
            {
                if (Classification == MethodClassification.mcInstantiated)
                    return ((InstantiatedMethodDesc.m_wFlags2 & (int)InstantiatedMethodDescFlags.KindMask) == (int)InstantiatedMethodDescFlags.SharedMethodInstantiation);

                return null;
            }
        }

        public bool? IMD_IsWrapperStubWithInstantiations
        {
            get
            {
                if (Classification == MethodClassification.mcInstantiated)
                    return ((InstantiatedMethodDesc.m_wFlags2 & (int)InstantiatedMethodDescFlags.KindMask) == (int)InstantiatedMethodDescFlags.WrapperStubWithInstantiations);

                return null;
            }
        }

        #endregion
    }
}
