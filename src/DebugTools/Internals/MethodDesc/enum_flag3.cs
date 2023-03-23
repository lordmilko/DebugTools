namespace DebugTools.MethodDescFlags
{
    enum enum_flag3
    {
        // There are flags available for use here (currently 5 flags bits are available); however, new bits are hard to come by, so any new flags bits should
        // have a fairly strong justification for existence.
        enum_flag3_TokenRemainderMask = 0x3FFF, // This must equal METHOD_TOKEN_REMAINDER_MASK calculated higher in this file
        // These are seperate to allow the flags space available and used to be obvious here
        // and for the logic that splits the token to be algorithmically generated based on the
        // #define
        enum_flag3_HasForwardedValuetypeParameter = 0x4000, // Indicates that a type-forwarded type is used as a valuetype parameter (this flag is only valid for ngenned items)
        enum_flag3_ValueTypeParametersWalked = 0x4000, // Indicates that all typeref's in the signature of the method have been resolved to typedefs (or that process failed) (this flag is only valid for non-ngenned methods)
        enum_flag3_DoesNotHaveEquivalentValuetypeParameters = 0x8000, // Indicates that we have verified that there are no equivalent valuetype parameters for this method
    }
}
