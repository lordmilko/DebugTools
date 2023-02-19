using System.Text;
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
    
    /// <summary>
    /// Represents a MethodRefSig that describes how a MethodDefSig is invoked when the <see cref="SigMethod.CallingConvention"/> is <see cref="CorCallingConvention.VARARG"/>.
    /// A MethodRefSig is represented using a <see cref="mdMemberRef"/>.
    /// </summary>
    public class SigMethodRef : SigMethod
    {
        public ISigParameter[] VarArgParamTypes { get; }

        internal SigMethodRef(string name, CallingConvention callingConvention, SigType retType, ISigParameter[] methodParams, ISigParameter[] varArgParamTypes)
            : base(name, callingConvention, retType, methodParams)
        {
            VarArgParamTypes = varArgParamTypes;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(RetType).Append(" ").Append(Name);

            builder.Append("(");

            for (var i = 0; i < Parameters.Length; i++)
            {
                builder.Append(Parameters[i]);

                if (i < Parameters.Length - 1)
                    builder.Append(", ");
            }

            if (VarArgParamTypes.Length > 0)
            {
                if (Parameters.Length > 0)
                    builder.Append(", ");

                for (var i = 0; i < VarArgParamTypes.Length; i++)
                {
                    builder.Append(VarArgParamTypes[i]);

                    if (i < VarArgParamTypes.Length - 1)
                        builder.Append(", ");
                }
            }

            builder.Append(")");

            return builder.ToString();
        }
    }
}
