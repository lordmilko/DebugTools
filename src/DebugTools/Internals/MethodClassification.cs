namespace DebugTools
{
    // The MethodDesc is a union of several types. The following
    // 3-bit field determines which type it is. Note that JIT'ed/non-JIT'ed
    // is not represented here because this isn't known until the
    // method is executed for the first time. Because any thread could
    // change this bit, it has to be done in a place where access is
    // synchronized.

    // **** NOTE: if you add any new flags, make sure you add them to ClearFlagsOnUpdate
    // so that when a method is replaced its relevant flags are updated

    // Used in MethodDesc
    public enum MethodClassification
    {
        mcIL = 0, // IL
        mcFCall = 1, // FCall (also includes tlbimped ctor, Delegate ctor)
        mcNDirect = 2, // N/Direct
        mcEEImpl = 3, // special method; implementation provided by EE (like Delegate Invoke)
        mcArray = 4, // Array ECall
        mcInstantiated = 5, // Instantiated generic methods, including descriptors
        // for both shared and unshared code (see InstantiatedMethodDesc)

        // This needs a little explanation.  There are MethodDescs on MethodTables
        // which are Interfaces.  These have the mdcInterface bit set.  Then there
        // are MethodDescs on MethodTables that are Classes, where the method is
        // exposed through an interface.  These do not have the mdcInterface bit set.
        //
        // So, today, a dispatch through an 'mdcInterface' MethodDesc is either an
        // error (someone forgot to look up the method in a class' VTable) or it is
        // a case of COM Interop.

        mcComInterop = 6,
        mcDynamic = 7, // for method desc with no metadata behind
        mcCount,
    }
}
