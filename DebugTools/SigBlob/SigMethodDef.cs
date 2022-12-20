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
    public class SigMethodDef : SigMethod
    {
        public string[] GenericTypeArgs { get; }

        internal SigMethodDef(string name, CorCallingConvention callingConvention, SigType retType, ISigParameter[] methodParams, string[] genericTypeArgs)
            : base(name, callingConvention, retType, methodParams)
        {
            GenericTypeArgs = genericTypeArgs;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(RetType).Append(" ").Append(Name);

            if ((CallingConvention & CorCallingConvention.GENERIC) != 0)
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