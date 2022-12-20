using ClrDebug;

namespace DebugTools
{
    /* --> HASTHIS ---> EXPLICITTHIS --------> VARARG ---> ParamCount ---->|
     * \            \_________________^ ^                                  |
     *  \______________________________/                                   |
     *                                                                     |
     *                                                                     |
     *                                                                     |
     *  |<-----------------------------------------------------------------|
     *  |                                    _________                       _________
     *  |                                   /         \                     /         \
     *  v                                  v           \                   v           \
     *  -> RetType  ---------> Param --------------------------> SENTINEL ----> Param -------->
     *                                 \                   ^  \                            ^
     *                                  \_________________/    \__________________________/
     */
    public class SigMethodRef : SigMethod
    {
        public SigType[] VarArgParamTypes { get; }

        internal SigMethodRef(string name, CorCallingConvention callingConvention, SigType retType, ISigParameter[] methodParams, SigType[] varArgParamTypes)
            : base(name, callingConvention, retType, methodParams)
        {
            VarArgParamTypes = varArgParamTypes;
        }
    }
}