using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DebugTools.Profiler
{
    public enum MatchKind
    {
        All = 1,
        Contains,
        ModuleName,
        StartsWith,
        EndsWith,
        Literal
    }

    public class MatchCollection : IEnumerable
    {
        private List<(MatchKind kind, string value)> list = new List<(MatchKind kind, string value)>();

        public void Add(MatchKind kind, string value)
        {
            list.Add((kind, value));
        }

        public void Add((MatchKind kind, string value) item)
        {
            list.Add(item);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var item in list)
            {
                builder.Append((char) item.kind);
                builder.Append(item.value);
                builder.Append('\t');
            }

            builder.Append('\t');

            return builder.ToString();
        }

        public IEnumerator GetEnumerator() => list.GetEnumerator();
    }
}
