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

        public override string ToString()
        {
            return GetIndex?.ToString() ?? SetIndex?.ToString() ?? base.ToString();
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

        public override string ToString() => Method.ToString();
    }

    class ReflectionCache
    {
        public Type Type { get; }

        public Dictionary<string, FieldInfo> Fields { get; } = new Dictionary<string, FieldInfo>();
        public Dictionary<string, FieldInfo> FieldsIgnoreCase { get; } = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);

        public MemberDictionary<PropertyInfo> Properties { get; } = new MemberDictionary<PropertyInfo>();
        public MemberDictionary<PropertyInfo> PropertiesIgnoreCase { get; } = new MemberDictionary<PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        public IndexerAndParameters[] Indexers { get; }

        public MemberDictionary<MethodInfoAndParameters[]> Methods { get; } = new MemberDictionary<MethodInfoAndParameters[]>();

        public MemberDictionary<MethodInfoAndParameters[]> MethodsIgnoreCase { get; } = new MemberDictionary<MethodInfoAndParameters[]>(StringComparer.OrdinalIgnoreCase);

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

            var propertyMethods = new List<MethodInfo>();

            foreach (var property in properties)
            {
                var getter = property.GetGetMethod(true);
                var setter = property.GetSetMethod(true);

                if (getter != null)
                    propertyMethods.Add(getter);

                if (setter != null)
                    propertyMethods.Add(setter);

                var indexParameters = property.GetIndexParameters();

                if (indexParameters.Length > 0)
                {
                    indexers.Add(new IndexerAndParameters(getter, setter, indexParameters));
                }
                else
                {
                    Properties[property.Name] = property;
                    PropertiesIgnoreCase[property.Name] = property;
                }
            }

            if (Type.IsEnum)
            {
                //We don't want to have value__ or anything like that
                Fields.Clear();
                FieldsIgnoreCase.Clear();
                Properties.Clear();
                PropertiesIgnoreCase.Clear();
            }

            //Methods

            var methods = Type.GetMethods(flags);

            var methodBuilder = new Dictionary<string, List<MethodInfoAndParameters>>();
            var methodIgnoreCaseBuilder = new Dictionary<string, List<MethodInfoAndParameters>>(StringComparer.OrdinalIgnoreCase);

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
                if (propertyMethods.Contains(method))
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

            foreach (var kv in methodBuilder)
            {
                Methods[kv.Key] = kv.Value.ToArray();
                MethodsIgnoreCase[kv.Key] = kv.Value.ToArray();
            }

            Indexers = indexers.ToArray();

            Properties.BuildExplicitInterfaceMap(FlattenProperties);
            PropertiesIgnoreCase.BuildExplicitInterfaceMap(FlattenProperties);
            Methods.BuildExplicitInterfaceMap(FlattenMethods);
            MethodsIgnoreCase.BuildExplicitInterfaceMap(FlattenMethods);
        }

        private PropertyInfo FlattenProperties(PropertyInfo[] properties)
        {
            if (properties.Length == 1)
                return properties[0];

            var list = properties.Select(p => new PropertyAndMethod(p)).ToList();

            //If we have more than property, ostensibly that should be because we're processing the "have explicit interface"
            //scenario. If there is a clear single best interface that supersedes all other interfaces (e.g. IEnumerator<T>.Current
            //is better than IEnumerator.Current), we go with that interface's property. Otherwise, we fail this property and return null.
            if (ReflectionProvider.TryGetInterfaceOverride(list, out var result))
                return result.Property;

            return null;
        }

        private MethodInfoAndParameters[] FlattenMethods(MethodInfoAndParameters[][] methods)
        {
            return methods.SelectMany(m => m).ToArray();
        }

        private struct PropertyAndMethod : IMethodMatcher
        {
            public MethodInfo Method { get; }

            public PropertyInfo Property { get; }

            public PropertyAndMethod(PropertyInfo property)
            {
                Property = property;
                Method = property.GetGetMethod(true) ?? property.GetSetMethod(true);
            }
        }
    }
}
