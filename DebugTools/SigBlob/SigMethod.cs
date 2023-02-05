using System.Text;

namespace DebugTools
{
    public abstract class SigMethod
    {
        public string Name { get; }

        /// <summary>
        /// Gets the calling convention of the method.
        /// </summary>
        public CallingConvention CallingConvention { get; }

        public SigType RetType { get; }

        public ISigParameter[] Parameters { get; }

        protected SigMethod(string name, CallingConvention callingConvention, SigType retType, ISigParameter[] methodParams)
        {
            Name = name;
            CallingConvention = callingConvention;
            RetType = retType;
            Parameters = methodParams;
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

            builder.Append(")");

            return builder.ToString();
        }
    }
}
