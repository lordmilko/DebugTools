using System;
using System.Collections.Generic;

namespace DebugTools.Dynamic
{
    class MemberDictionary<T>
    {
        private Dictionary<string, T> dict;
        private Dictionary<string, T> explicitInterfaceDict;
        IEqualityComparer<string> comparer;

        public Dictionary<string, T>.KeyCollection Keys => dict.Keys;

        public MemberDictionary()
        {
            dict = new Dictionary<string, T>();
            explicitInterfaceDict = new Dictionary<string, T>();
        }

        public MemberDictionary(IEqualityComparer<string> comparer)
        {
            dict = new Dictionary<string, T>(comparer);
            explicitInterfaceDict = new Dictionary<string, T>(comparer);
            this.comparer = comparer;
        }

        private MemberDictionary(Dictionary<string, T> value)
        {
            dict = value;
        }

        public bool TryGetValue(string key, out T value)
        {
            if (dict.TryGetValue(key, out value))
                return true;

            if (explicitInterfaceDict.TryGetValue(key, out value))
                return true;

            value = default;
            return false;
        }

        public T this[string key]
        {
            get => dict[key];
            set => dict[key] = value;
        }

        public void BuildExplicitInterfaceMap(Func<T[], T> flatten)
        {
            var map = new Dictionary<string, List<T>>(comparer);

            foreach (var kv in dict)
            {
                var key = kv.Key;

                var dot = key.LastIndexOf('.');

                if (dot != -1 && dot < key.Length - 1)
                {
                    var subStr = key.Substring(dot + 1);

                    if (map.TryGetValue(subStr, out var list))
                    {
                        list.Add(kv.Value);
                    }
                    else
                    {
                        map[subStr] = new List<T>
                        {
                            kv.Value
                        };
                    }
                }
            }

            foreach (var kv in map)
            {
                var val = flatten(kv.Value.ToArray());

                if (val != null)
                    explicitInterfaceDict[kv.Key] = val;
            }
        }

        public void Clear() => dict.Clear();
    }
}
