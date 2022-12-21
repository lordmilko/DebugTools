using System.Text;
using ClrDebug;

namespace DebugTools
{
    /* --> HASTHIS ---> EXPLICITTHIS ------|-> DEFAULT ------------------->|
     * \            \_________________^ ^  |                               |
     *  \______________________________/   |-> VARARG  ------------------->|
     *                                     |                               |
     *                                     |-> GENERIC -> GenParamCount -->|
     *                                                                     |
     *  |<-----------------------------------------------------------------|
     *  |                                    _________
     *  |                                   /         \
     *  v                                  v           \
     *  -> ParamCount ---> RetType  ---------> Param ----------->
     *                                 \                   ^
     *                                  \_________________/
     */

    /// <summary>
    /// Represents a MethodDefSig (§II.23.2.1) that describes the definition of a method.
    /// </summary>
    public class SigMethodDef : SigMethod
    {
        public string[] GenericTypeArgs { get; }

        internal SigMethodDef(string name, CallingConvention callingConvention, SigType retType, ISigParameter[] methodParams, string[] genericTypeArgs)
            : base(name, callingConvention, retType, methodParams)
        {
            GenericTypeArgs = genericTypeArgs;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(RetType).Append(" ").Append(Name);

            if (CallingConvention.IsGeneric)
            {
                builder.Append("<");

                for (var i = 0; i < GenericTypeArgs.Length; i++)
                {
                    builder.Append(GenericTypeArgs[i]);

                    if (i < GenericTypeArgs.Length - 1)
                        builder.Append(", ");
                }

                builder.Append(">");
            }

            builder.Append("(");

            for (var i = 0; i < Parameters.Length; i++)
            {
                builder.Append(Parameters[i]);

                if (i < Parameters.Length - 1)
                    builder.Append(", ");
            }

            builder.Append(")");

            return builder.ToString();
        }
    }
}
