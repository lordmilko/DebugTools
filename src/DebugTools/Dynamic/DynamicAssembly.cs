using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using DebugTools.Ui;

namespace DebugTools.Dynamic
{
    public class DynamicAssembly
    {
        private static object lockObj = new object();

        public static readonly DynamicAssembly Instance = new DynamicAssembly("DebugTools.GeneratedCode");

        public string Name { get; }

        public DynamicAssembly(string name)
        {
            Name = name;
        }

        #region AssemblyBuilder

        AssemblyBuilder assemblyBuilder;
        internal AssemblyBuilder AssemblyBuilder
        {
            get
            {
                lock (lockObj)
                {
                    if (assemblyBuilder == null)
                    {
                        InitAssembly();
                        InitModule();
                    }
                }

                return assemblyBuilder;
            }
        }

        void InitAssembly()
        {
            var assemblyName = new AssemblyName(Name);

            //RunAndSave must be specified to allow debugging in memory (even if we don't actually save it)
            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

#if DEBUG
            //Specify a DebuggableAttribute to enable debugging
            var attribute = typeof(DebuggableAttribute);
            var ctor = attribute.GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });

            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[]
            {
                DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default
            });

            assemblyBuilder.SetCustomAttribute(attributeBuilder);
#endif
        }

        #endregion
        #region ModuleBuilder

        ModuleBuilder moduleBuilder;
        ModuleBuilder ModuleBuilder
        {
            get
            {
                lock (lockObj)
                {
                    if (moduleBuilder == null)
                    {
                        InitAssembly();
                        InitModule();
                    }
                }

                return moduleBuilder;
            }
        }

        void InitModule()
        {
            //All assemblies contain at least one module. This implementation detail is typically invisible
            moduleBuilder = assemblyBuilder.DefineDynamicModule(Name, Name + ".dll", true);
        }

        #endregion

        public TypeBuilder DefineWindowMessage(string name)
        {
            var typeBuilder = ModuleBuilder.DefineType($"{AssemblyBuilder.GetName().Name}.{name}", TypeAttributes.Public, typeof(WindowMessage));

            return typeBuilder;
        }

        public void Save()
        {
            AssemblyBuilder.Save(Name + ".dll");
        }
    }
}
