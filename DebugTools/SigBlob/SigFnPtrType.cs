using System.Text;
using ClrDebug;

namespace DebugTools
{
    class SigFnPtrType : SigType
    {
        public SigMethod Method { get; }

        public SigFnPtrType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
        {
            Method = reader.ParseSigMethodDefOrRef("delegate*", false);
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