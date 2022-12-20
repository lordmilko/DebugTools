using System.Collections.Generic;
using System.Text;
using ClrDebug;

namespace DebugTools
{
    class SigGenericType : SigType
    {
        public string GenericTypeDefinitionName { get; }

        public mdToken GenericTypeDefinitionToken { get; }

        public SigType[] GenericArgs { get; }

        public SigGenericType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
        {
            GenericTypeDefinitionToken = reader.CorSigUncompressToken();
            GenericTypeDefinitionName = GetName(GenericTypeDefinitionToken, reader.Import);
            var genericArgsLength = reader.CorSigUncompressData();

            var args = new List<SigType>();

            for (var i = 0; i < genericArgsLength; i++)
                args.Add(New(ref reader));

            GenericArgs = args.ToArray();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            var name = GenericTypeDefinitionName;

            var index = name.IndexOf('`');

            if (index != -1)
                name = name.Substring(0, index);

            builder.Append(name);
            builder.Append("<");

            for (var i = 0; i < GenericArgs.Length; i++)
            {
                builder.Append(GenericArgs[i]);

                if (i < GenericArgs.Length - 1)
                    builder.Append(", ");
            }

            builder.Append(">");

            return builder.ToString();
        }
    }
}