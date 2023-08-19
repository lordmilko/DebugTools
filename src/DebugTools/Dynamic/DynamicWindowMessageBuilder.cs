using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClrDebug;
using DebugTools.Ui;

namespace DebugTools.Dynamic
{
    public class DynamicWindowMessageBuilder
    {
        struct FieldItem
        {
            public Type Type;
            public FieldBuilder Field;
            public bool IsPointer;
        }

        private static MethodInfo getWPARAM;
        private static MethodInfo getLPARAM;
        private static MethodInfo cordbAddressToULong;

        private static MethodInfo method_HIWORD;
        private static MethodInfo method_LOWORD;
        private static MethodInfo method_GET_Y_LPARAM;
        private static MethodInfo method_GET_X_LPARAM;

        static DynamicWindowMessageBuilder()
        {
            getWPARAM = typeof(WindowMessage).GetProperty(nameof(WindowMessage.wParam)).GetGetMethod();
            getLPARAM = typeof(WindowMessage).GetProperty(nameof(WindowMessage.lParam)).GetGetMethod();

            cordbAddressToULong = typeof(CORDB_ADDRESS).GetMethods().Single(m => m.Name == "op_Implicit" && m.ReturnType == typeof(ulong));

            var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            method_HIWORD = typeof(WindowMessage).GetMethod(nameof(WindowMessage.HIWORD), staticFlags);
            method_LOWORD = typeof(WindowMessage).GetMethod(nameof(WindowMessage.LOWORD), staticFlags);
            method_GET_Y_LPARAM = typeof(WindowMessage).GetMethod(nameof(WindowMessage.GET_Y_LPARAM), staticFlags);
            method_GET_X_LPARAM = typeof(WindowMessage).GetMethod(nameof(WindowMessage.GET_X_LPARAM), staticFlags);
        }

        internal IReadOnlyDictionary<WM, (Type type, ConstructorInfo ctor, WindowMessageStructReader structReader)> Build()
        {
            var messages = Enum.GetValues(typeof(WM)).Cast<WM>().ToArray();
            var baseCtor = typeof(WindowMessage).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();

            var dict = new Dictionary<WM, (Type type, ConstructorInfo ctor, WindowMessageStructReader structReader)>();

            foreach (var message in messages)
            {
                var result = ProcessMessage(message, baseCtor);

                dict[message] = (result.Item1, result.Item1.GetConstructors().Single(), result.Item2);
            }

            return new ReadOnlyDictionary<WM, (Type type, ConstructorInfo ctor, WindowMessageStructReader structReader)>(dict);
        }

        private (Type, WindowMessageStructReader) ProcessMessage(WM message, ConstructorInfo baseCtor)
        {
            var typeBuilder = DynamicAssembly.Instance.DefineWindowMessage(message.ToString());

            var fieldInfo = typeof(WM).GetMember(message.ToString())[0];

            var wParamAttribs = fieldInfo.GetCustomAttributes<WPARAMAttribute>().Cast<WMParamAttribute>().ToArray();
            var lParamAttribs = fieldInfo.GetCustomAttributes<LPARAMAttribute>().Cast<WMParamAttribute>().ToArray();

            var fieldInfos = new List<FieldItem>();

            ProcessParams(typeBuilder, wParamAttribs, getWPARAM, fieldInfos);
            ProcessParams(typeBuilder, lParamAttribs, getLPARAM, fieldInfos);

            AddCtor(typeBuilder, baseCtor, fieldInfos.Where(v => v.IsPointer).ToArray());

            var type = typeBuilder.CreateType();

            if (fieldInfos.Count == 0)
                return (type, null);

            return (type, new WindowMessageStructReader(fieldInfos.Where(v => v.IsPointer).Select(v => v.Type).ToArray()));
        }

        private void ProcessParams(TypeBuilder typeBuilder, WMParamAttribute[] attribs, MethodInfo getParam, List<FieldItem> fieldInfos)
        {
            foreach (var attrib in attribs)
            {
                var mode = attrib.IsStructPointerOrPart;

                if (mode.IsLeft)
                {
                    //Pointer/literal

                    if (mode.Left && (attrib.Type.Assembly == typeof(WM).Assembly && !attrib.Type.IsEnum) || attrib.Type == typeof(string))
                        ProcessPointerOrLiteralParam(typeBuilder, attrib, fieldInfos, true); 
                    else
                        ProcessPointerOrLiteralParam(typeBuilder, attrib, fieldInfos, false);
                }
                else
                {
                    //HIWORD/LOWORD

                    ProcessPartParam(typeBuilder, attrib, getParam);
                }
            }
        }

        private void ProcessPointerOrLiteralParam(TypeBuilder typeBuilder, WMParamAttribute attrib, List<FieldItem> fieldInfos, bool isPointer)
        {
            var propertyBuilder = typeBuilder.DefineProperty(attrib.Name, PropertyAttributes.None, attrib.Type, null);
            var fieldBuilder = typeBuilder.DefineField(char.ToLower(attrib.Name[0]) + attrib.Name.Substring(1), attrib.Type, FieldAttributes.Private);

            var methodBuilder = typeBuilder.DefineMethod(
                $"get_{attrib.Name}",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                propertyBuilder.PropertyType,
                null
            );

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);
            il.Emit(OpCodes.Ret);

            fieldInfos.Add(new FieldItem {Type = attrib.Type, Field = fieldBuilder, IsPointer = isPointer });

            propertyBuilder.SetGetMethod(methodBuilder);
        }

        private void ProcessPartParam(TypeBuilder typeBuilder, WMParamAttribute attrib, MethodInfo getParam)
        {
            var propertyBuilder = typeBuilder.DefineProperty(attrib.Name, PropertyAttributes.None, attrib.Type, null);

            var methodBuilder = typeBuilder.DefineMethod(
                $"get_{attrib.Name}",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                propertyBuilder.PropertyType,
                null
            );

            var getPart = GetParamPartMethodKindMethod(attrib);

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); //this
            il.Emit(OpCodes.Call, getParam);
            il.Emit(OpCodes.Call, cordbAddressToULong);
            il.Emit(OpCodes.Call, getPart);

            if (attrib.Type.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(attrib.Type);

                if (underlying == typeof(int))
                    il.Emit(OpCodes.Conv_I4);
                else if (underlying == typeof(uint))
                    il.Emit(OpCodes.Conv_U4);
                else
                    throw new NotImplementedException($"Don't know how to handle enum with backing value of type '{attrib.Type.Name}'");
            }
            else if (attrib.Type == typeof(int))
            {
                //Nothing to do
            }
            else
                throw new NotImplementedException($"Don't know how to handle HIWORD/LOWORD value of type '{attrib.Type.Name}'");

            il.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(methodBuilder);
        }

        private MethodInfo GetParamPartMethodKindMethod(WMParamAttribute attrib)
        {
            var partKind = attrib.IsStructPointerOrPart.Right;

            switch (attrib.PartMethodKind.Value)
            {
                case ParamPartMethodKind.Default:
                    return partKind == ParamPartKind.HIWORD ? method_HIWORD : method_LOWORD;

                case ParamPartMethodKind.Coordinate:
                    return partKind == ParamPartKind.HIWORD ? method_GET_Y_LPARAM : method_GET_X_LPARAM;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ParamPartMethodKind)} '{attrib.PartMethodKind.Value}'");
            }
        }

        private static void AddCtor(TypeBuilder typeBuilder, ConstructorInfo baseCtor, FieldItem[] fieldInfos)
        {
            var baseCtorParameterTypes = baseCtor.GetParameters().Select(p => p.ParameterType).ToArray();
            var ctorParameterTypes = baseCtorParameterTypes.Concat(fieldInfos.Select(v => v.Type)).ToArray();

            var newCtor = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, ctorParameterTypes);

            var il = newCtor.GetILGenerator();

            void EmitArg(int index)
            {
                switch (index)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, (byte) index); //If you don't cast the int to a byte you get a bunch of nops
                        break;
                }
            }

            il.Emit(OpCodes.Ldarg_0); //this

            for (var i = 1; i <= baseCtorParameterTypes.Length; i++)
                EmitArg(i);

            il.Emit(OpCodes.Call, baseCtor);

            for (var i = 0; i < fieldInfos.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);

                var index = i + baseCtorParameterTypes.Length + 1;

                EmitArg(index);
                il.Emit(OpCodes.Stfld, fieldInfos[i].Field);
            }

            il.Emit(OpCodes.Ret);
        }
    }
}
