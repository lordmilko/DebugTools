using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using DebugTools.SOS;
using Microsoft.Diagnostics.Runtime;

namespace DebugTools.Host
{
    [Serializable]
    public class StaticFieldInfo
    {
        public string AssemblyName { get; }

        internal string AssemblyPath { get; }

        public string DeclaringType { get; }

        public string Name { get; }

        internal string InternalName { get; }

        public string FieldType { get; }

        [NonSerialized]
        private Process process;

        internal Process Process
        {
            get => process;
            set => process = value;
        }

        public StaticFieldInfo(ClrType type, ClrStaticField field)
        {
            if (type.Module.IsDynamic)
                throw new NotImplementedException("Don't know how to get the assembly name of dynamic assemblies"); //dont know how to handle resolving the assembly name for retrieving a proxy since whatever the name is its not the location (touching the location on a dynamic assembly will throw)

            AssemblyPath = type.Module.AssemblyName;
            AssemblyName = Path.GetFileName(AssemblyPath);
            DeclaringType = type.Name;
            InternalName = field.Name;
            Name = Regex.Replace(InternalName, "<(.+)>k__BackingField", "$1");
            FieldType = field.Type?.Name;
        }

        internal StaticFieldInfo(SOSAssembly assembly, string declaringType, SOSFieldDesc fieldDesc, SOSProcess sosProcess)
        {
            if (assembly.isDynamic)
                throw new NotImplementedException("Don't know how to get the assembly name of dynamic assemblies");

            AssemblyPath = assembly.Name;
            AssemblyName = Path.GetFileName(AssemblyPath);
            DeclaringType = declaringType;
            InternalName = fieldDesc.Name;
            Name = Regex.Replace(InternalName, "<(.+)>k__BackingField", "$1");

            FieldType = SOSMethodTable.GetMethodTable(fieldDesc.MTOfType, sosProcess.SOS)?.Name;
        }

        public StaticFieldInfo(string assemblyName, string declaringType, string name, string fieldtype)
        {
            AssemblyName = assemblyName;
            DeclaringType = declaringType;
            Name = name;
            FieldType = fieldtype;
        }

        public override string ToString()
        {
            return $"{DeclaringType}.{Name}";
        }
    }
}
