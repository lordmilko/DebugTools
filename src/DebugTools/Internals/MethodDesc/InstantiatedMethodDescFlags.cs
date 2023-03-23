namespace DebugTools.MethodDescFlags
{
    enum InstantiatedMethodDescFlags
    {
        KindMask = 0x07,
        GenericMethodDefinition = 0x00,
        UnsharedMethodInstantiation = 0x01,
        SharedMethodInstantiation = 0x02,
        WrapperStubWithInstantiations = 0x03,

        // Non-virtual method added through EditAndContinue.
        EnCAddedMethod = 0x07,

        Unrestored = 0x08,

        HasComPlusCallInfo = 0x10, // this IMD contains an optional ComPlusCallInfo
    }
}
