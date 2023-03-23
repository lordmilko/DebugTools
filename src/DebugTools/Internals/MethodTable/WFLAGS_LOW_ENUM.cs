namespace DebugTools.MethodTableFlags
{
    enum WFLAGS_LOW_ENUM
    {
        // AS YOU ADD NEW FLAGS PLEASE CONSIDER WHETHER Generics::NewInstantiation NEEDS
        // TO BE UPDATED IN ORDER TO ENSURE THAT METHODTABLES DUPLICATED FOR GENERIC INSTANTIATIONS
        // CARRY THE CORECT FLAGS.
        //

        // We are overloading the low 2 bytes of m_dwFlags to be a component size for Strings
        // and Arrays and some set of flags which we can be assured are of a specified state
        // for Strings / Arrays, currently these will be a bunch of generics flags which don't
        // apply to Strings / Arrays.

        enum_flag_UNUSED_ComponentSize_1 = 0x00000001,

        enum_flag_StaticsMask = 0x00000006,
        enum_flag_StaticsMask_NonDynamic = 0x00000000,
        enum_flag_StaticsMask_Dynamic = 0x00000002,   // dynamic statics (EnC, reflection.emit)
        enum_flag_StaticsMask_Generics = 0x00000004,   // generics statics
        enum_flag_StaticsMask_CrossModuleGenerics = 0x00000006, // cross module generics statics (NGen)
        enum_flag_StaticsMask_IfGenericsThenCrossModule = 0x00000002, // helper constant to get rid of unnecessary check

        enum_flag_NotInPZM = 0x00000008,   // True if this type is not in its PreferredZapModule

        enum_flag_GenericsMask = 0x00000030,
        enum_flag_GenericsMask_NonGeneric = 0x00000000,   // no instantiation
        enum_flag_GenericsMask_GenericInst = 0x00000010,   // regular instantiation, e.g. List<String>
        enum_flag_GenericsMask_SharedInst = 0x00000020,   // shared instantiation, e.g. List<__Canon> or List<MyValueType<__Canon>>
        enum_flag_GenericsMask_TypicalInst = 0x00000030,   // the type instantiated at its formal parameters, e.g. List<T>

        enum_flag_HasVariance = 0x00000100,   // This is an instantiated type some of whose type parameters are co- or contra-variant

        enum_flag_HasDefaultCtor = 0x00000200,
        enum_flag_HasPreciseInitCctors = 0x00000400,   // Do we need to run class constructors at allocation time? (Not perf important, could be moved to EEClass

        enum_flag_IsByRefLike = 0x00001000,

        // In a perfect world we would fill these flags using other flags that we already have
        // which have a constant value for something which has a component size.
        enum_flag_UNUSED_ComponentSize_5 = 0x00002000,
        enum_flag_UNUSED_ComponentSize_6 = 0x00004000,
        enum_flag_UNUSED_ComponentSize_7 = 0x00008000,

        // IMPORTANT! IMPORTANT! IMPORTANT!
        //
        // As you change the flags in WFLAGS_LOW_ENUM you also need to change this
        // to be up to date to reflect the default values of those flags for the
        // case where this MethodTable is for a String or Array
        enum_flag_StringArrayValues = (enum_flag_StaticsMask_NonDynamic & 0xffff) |
                                      (enum_flag_NotInPZM & 0) |
                                      (enum_flag_GenericsMask_NonGeneric & 0xffff) |
                                      (enum_flag_HasVariance & 0) |
                                      (enum_flag_HasDefaultCtor & 0) |
                                      (enum_flag_HasPreciseInitCctors & 0),
    }
}
