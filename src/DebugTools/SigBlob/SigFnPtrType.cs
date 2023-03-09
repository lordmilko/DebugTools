using System.Text;
using ClrDebug;

namespace DebugTools
{
    class SigFnPtrType : SigType
    {
        public SigMethod Method { get; }

        public SigFnPtrType(CorElementType type, ref SigReader reader) : base(type)
        {
            Method = reader.ParseMethod("delegate*", false);
        }

        public override string ToString()
        {
            if (Method == null)
                return base.ToString();

            var builder = new StringBuilder();

            builder.Append("delegate*<");

            foreach (var parameter in Method.Parameters)
            {
                builder.Append(parameter);

                builder.Append(", ");
            }

            builder.Append(Method.RetType);

            builder.Append(">");

            return builder.ToString();
        }
    }
}
