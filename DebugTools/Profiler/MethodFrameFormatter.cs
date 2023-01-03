using System.Text;

namespace DebugTools.Profiler
{
    public class MethodFrameFormatter
    {
        public static readonly MethodFrameFormatter Default = new MethodFrameFormatter(false);

        private bool excludeNamespace;

        public MethodFrameFormatter(bool excludeNamespace)
        {
            this.excludeNamespace = excludeNamespace;
        }

        public string ToString(IFrame frame)
        {
            if (frame is RootFrame r)
            {
                if (r.ThreadName == null)
                    return r.ThreadId.ToString();

                return $"{r.ThreadName} {r.ThreadId}";
            }
            else if (frame is MethodFrameDetailed d)
            {
                var builder = new StringBuilder();

                var info = d.MethodInfo;

                var exitResult = d.GetExitResult();

                if (exitResult == null)
                    builder.Append("<Error> ");
                else
                {
                    builder.Append(exitResult).Append(" ");
                }

                builder.Append(GetTypeName(info.TypeName)).Append(".").Append(info.MethodName);
                builder.Append("(");

                var parameters = d.GetEnterParameters();

                if (parameters == null)
                    builder.Append("<Error>");
                else
                {
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        builder.Append(StringifyValue(parameters[i]));

                        if (i < parameters.Count - 1)
                            builder.Append(", ");
                    }
                }

                builder.Append(")");
                return builder.ToString();
            }
            else if (frame is MethodFrame m)
            {
                var info = m.MethodInfo;

                return $"{GetTypeName(info.TypeName)}.{info.MethodName}";
            }
            else
                return frame.ToString();
        }

        private string GetTypeName(string name)
        {
            if (name == null)
                return "null";

            if (excludeNamespace)
            {
                var dot = name.LastIndexOf('.');

                if (dot != -1)
                    return name.Substring(dot + 1);
            }

            return name;
        }

        private string StringifyValue(object value)
        {
            if (value is ClassValue c)
                return GetTypeName(c.Name);
            else if (value is ValueType v)
                return GetTypeName(v.Name);
            else if (value is StringValue s)
                return $"\"{s.Value}\"";

            return value.ToString();
        }
    }
}
