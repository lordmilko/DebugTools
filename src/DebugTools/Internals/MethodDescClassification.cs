namespace DebugTools
{
    public enum MethodDescClassification
    {
        // Method is IL, FCall etc., see MethodClassification above.
        mdcClassification = 0x0007,
        mdcClassificationCount = mdcClassification + 1,

        // Note that layout of code:MethodDesc::s_ClassificationSizeTable depends on the exact values
        // of mdcHasNonVtableSlot and mdcMethodImpl

        // Has local slot (vs. has real slot in MethodTable)
        mdcHasNonVtableSlot = 0x0008,

        // Method is a body for a method impl (MI_MethodDesc, MI_NDirectMethodDesc, etc)
        // where the function explicitly implements IInterface.foo() instead of foo().
        mdcMethodImpl = 0x0010,

        // Has slot for native code
        mdcHasNativeCodeSlot = 0x0020,

        mdcHasComPlusCallInfo = 0x0040,

        // Method is static
        mdcStatic = 0x0080,

        // unused                           = 0x0100,
        // unused                           = 0x0200,

        // Duplicate method. When a method needs to be placed in multiple slots in the
        // method table, because it could not be packed into one slot. For eg, a method
        // providing implementation for two interfaces, MethodImpl, etc
        mdcDuplicate = 0x0400,

        // Has this method been verified?
        mdcVerifiedState = 0x0800,

        // Is the method verifiable? It needs to be verified first to determine this
        mdcVerifiable = 0x1000,

        // Is this method ineligible for inlining?
        mdcNotInline = 0x2000,

        // Is the method synchronized
        mdcSynchronized = 0x4000,

        // Does the method's slot number require all 16 bits
        mdcRequiresFullSlotNumber = 0x8000
    }
}
