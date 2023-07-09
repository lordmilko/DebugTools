using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugTools.Dynamic
{
    struct IndexerAndParameters
    {
        public MethodInfo GetIndex { get; }
        public MethodInfo SetIndex { get; }

        public ParameterInfo[] Parameters { get; }

        public IndexerAndParameters(MethodInfo getIndex, MethodInfo setIndex, ParameterInfo[] parameters)
        {
            GetIndex = getIndex;
            SetIndex = setIndex;
            Parameters = parameters;
        }
    }

    struct MethodInfoAndParameters
    {
        public MethodInfo Method { get; }

        public ParameterInfo[] Parameters { get; }

        public MethodInfoAndParameters(MethodInfo method, ParameterInfo[] parameters)
        {
            Method = method;
            Parameters = parameters;
        }
    }

    class ReflectionCache
    {
        public Type Type { get; }

        public Dictionary<string, FieldInfo> Fields { get; } = new Dictionary<string, FieldInfo>();
        public Dictionary<string, FieldInfo> FieldsIgnoreCase { get; } = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, PropertyInfo> Properties { get; } = new Dictionary<string, PropertyInfo>();
        public Dictionary<string, PropertyInfo> PropertiesIgnoreCase { get; } = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        public IndexerAndParameters[] Indexers { get; }

        public Dictionary<string, MethodInfoAndParameters[]> Methods { get; }

        public Dictionary<string, MethodInfoAndParameters[]> MethodsIgnoreCase { get; }

        public ReflectionCache(Type type)
        {
            Type = type;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var indexers = new List<IndexerAndParameters>();

            //Fields

            var fields = Type.GetFields(flags);

            foreach (var field in fields)
            {
                if (field.Name.StartsWith("<") && field.Name.EndsWith(">k__BackingField"))
                    continue;

                Fields[field.Name] = field;
                FieldsIgnoreCase[field.Name] = field;
            }

            //Properties

            var properties = Type.GetProperties(flags);

            foreach (var property in properties)
            {
                var indexParameters = property.GetIndexParameters();

                if (indexParameters.Length > 0)
                {
                    indexers.Add(new IndexerAndParameters(property.GetGetMethod(true), property.GetSetMethod(true), indexParameters));
                }
                else
                {
                    Properties[property.Name] = property;
                    PropertiesIgnoreCase[property.Name] = property;
                }
            }

            //Methods

            var methods = Type.GetMethods(flags);

            var methodBuilder = new Dictionary<string, List<MethodInfoAndParameters>>();
            var methodIgnoreCaseBuilder = new Dictionary<string, List<MethodInfoAndParameters>>();

            if (typeof(IList).IsAssignableFrom(Type))
            {
                var interfaceMap = Type.GetInterfaceMap(typeof(IList));

                var ifaceGet = typeof(IList).GetMethod("get_Item");
                var ifaceSet = typeof(IList).GetMethod("set_Item");

                var getItemIndex = Array.IndexOf(interfaceMap.InterfaceMethods, ifaceGet);
                var setItemIndex = Array.IndexOf(interfaceMap.InterfaceMethods, ifaceSet);

                var getItem = interfaceMap.TargetMethods[getItemIndex];
                var setItem = interfaceMap.TargetMethods[setItemIndex];

                if (!indexers.Any(i => i.GetIndex == getItem && i.SetIndex == setItem))
                    indexers.Add(new IndexerAndParameters(getItem, setItem, getItem.GetParameters()));
            }

            foreach (var method in methods)
            {
                if (indexers.Any(i => i.GetIndex == method || i.SetIndex == method))
                    continue;

                var parameters = method.GetParameters();

                var item = new MethodInfoAndParameters(method, parameters);

                if (methodBuilder.TryGetValue(method.Name, out var list))
                    list.Add(item);
                else
                    methodBuilder[method.Name] = new List<MethodInfoAndParameters> {item};

                if (methodIgnoreCaseBuilder.TryGetValue(method.Name, out list))
                    list.Add(item);
                else
                    methodIgnoreCaseBuilder[method.Name] = new List<MethodInfoAndParameters> { item };
            }

            Methods = methodBuilder.ToDictionary(m => m.Key, m => m.Value.ToArray());
            MethodsIgnoreCase = methodIgnoreCaseBuilder.ToDictionary(m => m.Key, m => m.Value.ToArray());
            Indexers = indexers.ToArray();
        }
    }
}
