namespace DebugTools.MethodDescFlags
{
    enum enum_flag2
    {
        // enum_flag2_HasPrecode implies that enum_flag2_HasStableEntryPoint is set.
        enum_flag2_HasStableEntryPoint = 0x01,   // The method entrypoint is stable (either precode or actual code)
        enum_flag2_HasPrecode = 0x02,   // Precode has been allocated for this method

        enum_flag2_IsUnboxingStub = 0x04,
        // unused                                       = 0x08,

        enum_flag2_IsJitIntrinsic = 0x10,   // Jit may expand method as an intrinsic

        enum_flag2_IsEligibleForTieredCompilation = 0x20,

        enum_flag2_RequiresCovariantReturnTypeChecking = 0x40

        // unused                           = 0x80,
    }
}
