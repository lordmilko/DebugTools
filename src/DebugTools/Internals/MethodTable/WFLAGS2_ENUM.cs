namespace DebugTools.MethodTableFlags
{
    enum WFLAGS2_ENUM
    {
        // AS YOU ADD NEW FLAGS PLEASE CONSIDER WHETHER Generics::NewInstantiation NEEDS
        // TO BE UPDATED IN ORDER TO ENSURE THAT METHODTABLES DUPLICATED FOR GENERIC INSTANTIATIONS
        // CARRY THE CORECT FLAGS.

        // The following bits describe usage of optional slots. They have to stay
        // together because of we index using them into offset arrays.
        enum_flag_MultipurposeSlotsMask = 0x001F,
        enum_flag_HasPerInstInfo = 0x0001,
        enum_flag_HasInterfaceMap = 0x0002,
        enum_flag_HasDispatchMapSlot = 0x0004,
        enum_flag_HasNonVirtualSlots = 0x0008,
        enum_flag_HasModuleOverride = 0x0010,

        enum_flag_IsZapped = 0x0020, // This could be fetched from m_pLoaderModule if we run out of flags

        enum_flag_IsPreRestored = 0x0040, // Class does not need restore
        // This flag is set only for NGENed classes (IsZapped is true)

        enum_flag_HasModuleDependencies = 0x0080,

        enum_flag_IsIntrinsicType = 0x0100,

        enum_flag_RequiresDispatchTokenFat = 0x0200,

        enum_flag_HasCctor = 0x0400,
        enum_flag_HasVirtualStaticMethods = 0x0800,

        enum_flag_RequiresAlign8 = 0x1000, // Type requires 8-byte alignment (only set on platforms that require this and don't get it implicitly)

        enum_flag_HasBoxedRegularStatics = 0x2000, // GetNumBoxedRegularStatics() != 0

        enum_flag_HasSingleNonVirtualSlot = 0x4000,

        enum_flag_DependsOnEquivalentOrForwardedStructs = 0x8000, // Declares methods that have type equivalent or type forwarded structures in their signature

    }
}
