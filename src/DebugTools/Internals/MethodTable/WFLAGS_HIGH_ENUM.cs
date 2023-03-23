namespace DebugTools.MethodTableFlags
{
    enum WFLAGS_HIGH_ENUM : uint
    {
        // DO NOT use flags that have bits set in the low 2 bytes.
        // These flags are DWORD sized so that our atomic masking
        // operations can operate on the entire 4-byte aligned DWORD
        // instead of the logical non-aligned WORD of flags.  The
        // low WORD of flags is reserved for the component size.

        // The following bits describe mutually exclusive locations of the type
        // in the type hiearchy.
        enum_flag_Category_Mask = 0x000F0000,

        enum_flag_Category_Class = 0x00000000,
        enum_flag_Category_Unused_1 = 0x00010000,
        enum_flag_Category_Unused_2 = 0x00020000,
        enum_flag_Category_Unused_3 = 0x00030000,

        enum_flag_Category_ValueType = 0x00040000,
        enum_flag_Category_ValueType_Mask = 0x000C0000,
        enum_flag_Category_Nullable = 0x00050000, // sub-category of ValueType
        enum_flag_Category_PrimitiveValueType = 0x00060000, // sub-category of ValueType, Enum or primitive value type
        enum_flag_Category_TruePrimitive = 0x00070000, // sub-category of ValueType, Primitive (ELEMENT_TYPE_I, etc.)

        enum_flag_Category_Array = 0x00080000,
        enum_flag_Category_Array_Mask = 0x000C0000,
        // enum_flag_Category_IfArrayThenUnused                 = 0x00010000, // sub-category of Array
        enum_flag_Category_IfArrayThenSzArray = 0x00020000, // sub-category of Array

        enum_flag_Category_Interface = 0x000C0000,
        enum_flag_Category_Unused_4 = 0x000D0000,
        enum_flag_Category_Unused_5 = 0x000E0000,
        enum_flag_Category_Unused_6 = 0x000F0000,

        enum_flag_Category_ElementTypeMask = 0x000E0000, // bits that matter for element type mask


        enum_flag_HasFinalizer = 0x00100000, // instances require finalization

        enum_flag_IDynamicInterfaceCastable = 0x00200000, // class implements IDynamicInterfaceCastable interface

        enum_flag_ICastable = 0x00400000, // class implements ICastable interface

        enum_flag_HasIndirectParent = 0x00800000, // m_pParentMethodTable has double indirection

        enum_flag_ContainsPointers = 0x01000000,

        enum_flag_HasTypeEquivalence = 0x02000000, // can be equivalent to another type

        enum_flag_IsTrackedReferenceWithFinalizer = 0x04000000,

        enum_flag_HasCriticalFinalizer = 0x08000000, // finalizer must be run on Appdomain Unload
        enum_flag_Collectible = 0x10000000,
        enum_flag_ContainsGenericVariables = 0x20000000,   // we cache this flag to help detect these efficiently and
        // to detect this condition when restoring

        enum_flag_ComObject = 0x40000000, // class is a com object

        enum_flag_HasComponentSize = 0x80000000,   // This is set if component size is used for flags.

        // Types that require non-trivial interface cast have this bit set in the category
        enum_flag_NonTrivialInterfaceCast = enum_flag_Category_Array
                                            | enum_flag_ComObject
                                            | enum_flag_ICastable
                                            | enum_flag_IDynamicInterfaceCastable
                                            | enum_flag_Category_ValueType
    }
}
