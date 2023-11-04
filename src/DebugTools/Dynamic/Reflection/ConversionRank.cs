namespace DebugTools.Dynamic
{
    enum ConversionRank
    {
        None,
        ImplementsInterface,
        SimpleToString,
        StringToPrimitive,
        StringToEnum,
        EnumUnderlying,
        AssignableFrom,
        Exact,
    }
}
