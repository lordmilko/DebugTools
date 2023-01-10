using System;
using System.Linq;
using ClrDebug;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class ValueTests : BaseTest
    {
        #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

        [TestMethod]
        public void Value_BoolArg() =>
            Test(ValueTestType.BoolArg, v => v.HasValue(true));

        [TestMethod]
        public void Value_CharArg() =>
            Test(ValueTestType.CharArg, v => v.HasValue('b'));

        [TestMethod]
        public void Value_SByteArg() =>
            Test(ValueTestType.SByteArg, v => v.HasValue<sbyte>(1));

        [TestMethod]
        public void Value_ByteArg() =>
            Test(ValueTestType.ByteArg, v => v.HasValue<byte>(1));

        [TestMethod]
        public void Value_Int16Arg() =>
            Test(ValueTestType.Int16Arg, v => v.HasValue<short>(1));

        [TestMethod]
        public void Value_UInt16Arg() =>
            Test(ValueTestType.UInt16Arg, v => v.HasValue<ushort>(1));

        [TestMethod]
        public void Value_Int32Arg() =>
            Test(ValueTestType.Int32Arg, v => v.HasValue<int>(1));

        [TestMethod]
        public void Value_UInt32Arg() =>
            Test(ValueTestType.UInt32Arg, v => v.HasValue<uint>(1));

        [TestMethod]
        public void Value_Int64Arg() =>
            Test(ValueTestType.Int64Arg, v => v.HasValue<long>(1));

        [TestMethod]
        public void Value_UInt64Arg() =>
            Test(ValueTestType.UInt64Arg, v => v.HasValue<ulong>(1));

        [TestMethod]
        public void Value_FloatArg() =>
            Test(ValueTestType.FloatArg, v => v.HasValue<float>(1.1f));

        [TestMethod]
        public void Value_DoubleArg() =>
            Test(ValueTestType.DoubleArg, v => v.HasValue<double>(1.1));

        [TestMethod]
        public void Value_IntPtrArg() =>
            Test(ValueTestType.IntPtrArg, v => v.HasValue(new IntPtr(1)));

        [TestMethod]
        public void Value_UIntPtrArg() =>
            Test(ValueTestType.UIntPtrArg, v => v.HasValue(new UIntPtr(1)));

        #endregion
        #region Ptr

        [TestMethod]
        public void Value_PtrArg() =>
            Test(ValueTestType.PtrArg, v => v.HasPtrDisplay("int* (1001)").HasPtrValue(1001));

        [TestMethod]
        public void Value_PtrCharArg() =>
            Test(ValueTestType.PtrCharArg, v => v.HasPtrDisplay("char* (\"String Value\")").HasPtrValue("String Value"));

        [TestMethod]
        public void Value_PtrCharRandomValueArg() =>
            Test(ValueTestType.Value_PtrCharRandomValueArg, v => v.GetParameter().VerifyValue().HasPtrValue((string) null));

        [TestMethod]
        public void Value_PtrVoidArg() =>
            Test(ValueTestType.PtrVoidArg, v => v.HasPtrDisplay("void* (0x3E9)").HasPtrValue(new IntPtr(1001)));

        [TestMethod]
        public void Value_PtrStructArg() =>
            Test(ValueTestType.PtrStructArg, v => v.HasPtrDisplay("Struct1WithField*").HasPtrValue(e => e.HasFieldValue(1001)));

        [TestMethod]
        public void Value_PtrComplexStruct() =>
            Test(ValueTestType.PtrComplexStructArg, v => v.HasPtrDisplay("ComplexPtrStruct*").HasPtrValue(e1 =>
            {
                e1.HasFieldValue(0, f =>
                {
                    f.HasPtrDisplay("char* (\"String Value\")").HasPtrValue("String Value");
                });

                e1.HasFieldValue(1, 'X');

                e1.HasFieldValue(2, f =>
                {
                    f.HasValueType("DebugTools.TestHost.Struct1WithField");

                    f.HasFieldValue(1001);
                });

                e1.HasFieldValue(3, f =>
                {
                    f.HasPtrDisplay("Struct1WithField*").HasPtrValue(e2 =>
                        e2.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1002)
                    );
                });
            }));

        #endregion
        #region PtrPtr

        [TestMethod]
        public void Value_PtrPtrArg() =>
            Test(ValueTestType.PtrPtrArg, v => v.HasPtrDisplay("int** (1001)").HasPtrValue(e1 => e1.HasPtrValue(1001)));

        [TestMethod]
        public void Value_PtrPtrCharArg() =>
            Test(ValueTestType.PtrPtrCharArg, v => v.HasPtrDisplay("char** (\"String Value\")").HasPtrValue(e => e.HasPtrValue("String Value")));

        [TestMethod]
        public void Value_PtrPtrVoidArg() =>
            Test(ValueTestType.PtrPtrVoidArg, v => v.HasPtrDisplay("void** (0x3E9)").HasPtrValue(e => e.HasPtrValue(new IntPtr(1001))));

        [TestMethod]
        public void Value_PtrPtrStructArg() =>
            Test(ValueTestType.PtrPtrStructArg, v => v.HasPtrDisplay("Struct1WithField**").HasPtrValue(e1 => e1.HasPtrValue(e2 => e2.HasFieldValue(1001))));

        [TestMethod]
        public void Value_PtrPtrComplexStruct() =>
            Test(ValueTestType.PtrPtrComplexStructArg, v => v.HasPtrDisplay("ComplexPtrStruct**").HasPtrValue(e1 =>
            {
                e1.HasPtrValue(e2 =>
                {
                    e2.HasFieldValue(0, f =>
                    {
                        f.HasPtrDisplay("char* (\"String Value\")").HasPtrValue("String Value");
                    });

                    e2.HasFieldValue(1, 'X');

                    e2.HasFieldValue(2, f =>
                    {
                        f.HasValueType("DebugTools.TestHost.Struct1WithField");

                        f.HasFieldValue(1001);
                    });

                    e2.HasFieldValue(3, f =>
                    {
                        f.HasPtrDisplay("Struct1WithField*").HasPtrValue(e3 =>
                            e3.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1002)
                        );
                    });
                });
            }));

        #endregion
        #region Ptr Array

        //CORPROF_E_CLASSID_IS_COMPOSITE

        [TestMethod]
        public void Value_PtrArrayArg() =>
            Test(ValueTestType.PtrArrayArg, v => v.HasError());

        [TestMethod]
        public void Value_PtrCharArrayArg() =>
            Test(ValueTestType.PtrCharArrayArg, v => v.HasError());

        [TestMethod]
        public void Value_PtrVoidArrayArg() =>
            Test(ValueTestType.PtrVoidArrayArg, v => v.HasError());

        [TestMethod]
        public void Value_PtrStructArrayArg() =>
            Test(ValueTestType.PtrStructArrayArg, v => v.HasError());

        [TestMethod]
        public void Value_PtrComplexStructArrayArg() =>
            Test(ValueTestType.PtrComplexStructArrayArg, v => v.HasError());

        #endregion

        [TestMethod]
        public void Value_FnPtr()
        {
            Test(ValueTestType.FnPtr, v =>
            {
                var parameter = v.GetParameter();

                Assert.IsInstanceOfType(parameter, typeof(FnPtrValue));
            });
        }

        [TestMethod]
        public void Value_FnPtrNull()
        {
            Test(ValueTestType.FnPtrNull, v =>
            {
                var parameter = v.GetParameter();

                Assert.IsInstanceOfType(parameter, typeof(FnPtrValue));

                var fnPtr = (FnPtrValue) parameter;

                Assert.AreEqual((ulong) 0, fnPtr.Value);
            });
        }

        [TestMethod]
        public void Value_DecimalArg()
        {
            Test(ValueTestType.DecimalArg, v =>
            {
                v.HasValueType("System.Decimal")
                    .HasFieldValue(0, 0)
                    .HasFieldValue(1, 0)
                    .HasFieldValue(2, 1)
                    .HasFieldValue(3, 0);
            });
        }

        #region String

        [TestMethod]
        public void Value_StringArg() =>
            Test(ValueTestType.StringArg, v => v.HasValue("foo"));

        [TestMethod]
        public void Value_EmptyStringArg() =>
            Test(ValueTestType.EmptyStringArg, v => v.HasValue(string.Empty));

        [TestMethod]
        public void Value_NullStringArg() =>
            Test(ValueTestType.NullStringArg, v => v.HasValue<string>(null));

        #endregion

        [TestMethod]
        public void Value_ObjectArg() =>
            Test(ValueTestType.ObjectArg, v => v.HasClassType("System.Object"));

        [TestMethod]
        public void Value_InterfaceArg() =>
            Test(ValueTestType.InterfaceArg, v => v.HasClassType("DebugTools.TestHost.ImplInterface"));

        [TestMethod]
        public void Value_NestedNestedExternalType() =>
            Test(ValueTestType.NestedNestedExternalType, v => v.HasValueType("Enumerator"));

        [TestMethod]
        public void Value_GenericWithObjectTypeArg() =>
            Test(ValueTestType.GenericWithObjectTypeArg, v => v.HasValueType("DebugTools.TestHost.GenericValueTypeType`1").HasFieldValue(f => f.HasClassType("System.Object")));

        [TestMethod]
        public void Value_GenericClassArg() =>
            Test(ValueTestType.GenericClassArg, v =>
                v.HasClassType("DebugTools.TestHost.GenericClassType`1")
                 .HasFieldValue(f => f.HasClassType("DebugTools.TestHost.Class1WithField").HasFieldValue(1)));

        [TestMethod]
        public void Value_GenericTwoTypeArgs() =>
            Test(ValueTestType.GenericTwoTypeArgs, v =>
            {
                var dict = (ClassValue) v.GetParameter();
                var entries = (SZArrayValue) dict.FieldValues[1];

                var first = (StructValue) entries.Value[0];
                first.VerifyValue().HasFieldValue(2, "first");
                first.VerifyValue().HasFieldValue(3, 1);

                var second = (StructValue)entries.Value[1];
                second.VerifyValue().HasFieldValue(2, "second");
                second.VerifyValue().HasFieldValue(3, 2);
            });

        [TestMethod]
        public void Value_GenericValueTypeArg() =>
            Test(ValueTestType.GenericValueTypeArg, v =>
                v.HasValueType("DebugTools.TestHost.GenericValueTypeType`1")
                 .HasFieldValue(f => f.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1)));

        [TestMethod]
        public void Value_GenericClass_ToObjectArg() =>
            Test(ValueTestType.GenericClass_ToObjectArg, v =>
                v.HasClassType("DebugTools.TestHost.GenericClassType`1")
                    .HasFieldValue(f => f.HasClassType("DebugTools.TestHost.Class1WithField").HasFieldValue(1)));

        [TestMethod]
        public void Value_GenericValueType_ToObjectArg() =>
            Test(ValueTestType.GenericValueType_ToObjectArg, v =>
                v.HasValueType("DebugTools.TestHost.GenericValueTypeType`1")
                    .HasFieldValue(f => f.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1)));

        #region Generic
        #region MethodVar

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeClassArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeClassArg, v => v.HasClassType("DebugTools.TestHost.Class1WithField").HasFieldValue(1));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeValueTypeArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeValueTypeArg, v => v.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeSimpleArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeSimpleArg, v => v.HasValue(1));

        #endregion
        #region MethodVar Array

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeClassArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeClassArrayArg, v =>
            {
                v.HasArrayClassValues(CorElementType.Class, "DebugTools.TestHost.Class1WithField", "DebugTools.TestHost.Class1WithField");

                v.VerifyArray(
                    e => e.HasFieldValue(1),
                    e => e.HasFieldValue(2)
                );
            });

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeValueTypeArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeValueTypeArrayArg, v =>
            {
                v.HasArrayStructValues(CorElementType.ValueType, "DebugTools.TestHost.Struct1WithField", "DebugTools.TestHost.Struct1WithField");

                v.VerifyArray(
                    e => e.HasFieldValue(1),
                    e => e.HasFieldValue(2)
                );
            });

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeSimpleArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeSimpleArrayArg, v => v.HasArrayValues(CorElementType.I4, 1, 2));

        #endregion
        #region MethodVar Generic Value

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericClassArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericClassArg, v => v.HasFieldValue(
                f => f.HasFieldValue(1)
            ));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericClassArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericClassArrayArg, v => v.HasFieldValue(
                f => f.VerifyArray(
                    e => e.HasFieldValue(1),
                    e => e.HasFieldValue(2)
                )
            ));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueTypeArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueTypeArg, v => v.HasFieldValue(
                f => f.HasFieldValue(1)
            ));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueTypeArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueTypeArrayArg, v => v.HasFieldValue(
                f => f.VerifyArray(
                    e => e.HasFieldValue(1),
                    e => e.HasFieldValue(2)
                )
            ));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueType_SimpleArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SimpleArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeNullablePrimitive() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeNullablePrimitive, v => v.HasFieldValue(0, true).HasFieldValue(1, 1));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeNullableValueType() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeNullableValueType, v => v.HasFieldValue(0, true).HasFieldValue(1, f => f.HasFieldValue((ulong)637949198450000000)));

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg, v =>
            {
                v.HasArrayStructValues(CorElementType.ValueType, "DebugTools.TestHost.GenericValueTypeType`1", "DebugTools.TestHost.GenericValueTypeType`1");

                v.VerifyArray(
                    e => e.HasFieldValue(f => f.HasFieldValue(1)),
                    e => e.HasFieldValue(f => f.HasFieldValue(2))
                );
            });

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg, v =>
            {
                v.HasValueType("DebugTools.TestHost.GenericValueTypeType`1");
                v.HasFieldValue(
                    f1 => f1.VerifyArray(
                        e => e.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1),
                        e => e.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(2)
                    )
                );
            });

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg, v =>
            {
                v.VerifyMultiArray(
                    e1 => e1.HasValueType("DebugTools.TestHost.GenericValueTypeType`1").HasFieldValue(
                        f1 => f1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1)
                    ),
                    e1 => e1.HasValueType("DebugTools.TestHost.GenericValueTypeType`1").HasFieldValue(
                        f1 => f1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(2)
                    ),

                    e1 => e1.HasValueType("DebugTools.TestHost.GenericValueTypeType`1").HasFieldValue(
                        f1 => f1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(3)
                    ),
                    e1 => e1.HasValueType("DebugTools.TestHost.GenericValueTypeType`1").HasFieldValue(
                        f1 => f1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(4)
                    )
                );
            });

        [TestMethod]
        public void Value_Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg() =>
            Test(ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg, v =>
            {
                v.HasValueType("DebugTools.TestHost.GenericValueTypeType`1");
                v.HasFieldValue(
                    f1 => f1.VerifyMultiArray(
                        e1 => e1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(1),
                        e1 => e1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(2),

                        e1 => e1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(3),
                        e1 => e1.HasValueType("DebugTools.TestHost.Struct1WithField").HasFieldValue(4)
                    )
                );
            });

        #endregion
        #region TypeVar

        [TestMethod]
        public void Value_Generic_TypeVar_ElementTypeClassArg() =>
            Test(ValueTestType.Generic_TypeVar_ElementTypeClassArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_Generic_TypeVar_ElementTypeValueTypeArg() =>
            Test(ValueTestType.Generic_TypeVar_ElementTypeValueTypeArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_Generic_TypeVar_ElementTypeSimpleArg() =>
            Test(ValueTestType.Generic_TypeVar_ElementTypeSimpleArg, v => v.HasValue(1));

        #endregion
        #region TypeVar Array

        [TestMethod]
        public void Value_Generic_TypeVar_ElementTypeClassArrayArg() =>
            Test(ValueTestType.Generic_TypeVar_ElementTypeClassArrayArg, v => v.VerifyArray(
                e => e.HasFieldValue(1),
                e => e.HasFieldValue(2)
            ));

        [TestMethod]
        public void Value_Generic_TypeVar_ElementTypeValueTypeArrayArg() =>
            Test(ValueTestType.Generic_TypeVar_ElementTypeValueTypeArrayArg, v => v.VerifyArray(
                e => e.HasFieldValue(1),
                e => e.HasFieldValue(2)
            ));

        [TestMethod]
        public void Value_Generic_TypeVar_ElementTypeSimpleArrayArg() =>
            Test(ValueTestType.Generic_TypeVar_ElementTypeSimpleArrayArg, v => v.HasArrayValues(CorElementType.I4, 1, 2));

        #endregion
        #endregion
        #region String Array

        [TestMethod]
        public void Value_StringArrayArg() =>
            Test(ValueTestType.StringArrayArg, v => v.HasArrayValues(CorElementType.Class, "a", "b"));

        [TestMethod]
        public void Value_EmptyStringArrayArg() =>
            Test(ValueTestType.EmptyStringArrayArg, v => v.HasArrayValues(CorElementType.Class, new string[0]));

        [TestMethod]
        public void Value_ObjectArrayContainingStringArg() =>
            Test(ValueTestType.ObjectArrayContainingStringArg, v => v.HasArrayValues(CorElementType.Class, "a"));

        #endregion

        [TestMethod]
        public void Value_MultiArrayThreeDimensionsArg() =>
            Test(ValueTestType.MultiArrayThreeDimensionsArg, v => v.VerifyMultiArray(
                Enumerable.Range(1, 24).Select<int, Action<ValueVerifier>>(i => e => e.HasValue(i)).ToArray()
            ));

        #region Class

        [TestMethod]
        public void Value_ClassArg() =>
            Test(ValueTestType.ClassArg, v => v.HasClassType("DebugTools.TestHost.Class1"));

        [TestMethod]
        public void Value_ClassWithFieldArg() =>
            Test(ValueTestType.ClassWithFieldArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_ClassWithPropertyArg() =>
            Test(ValueTestType.ClassWithPropertyArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_ExternalClass() =>
            Test(ValueTestType.ExternalClass, v => v.HasFieldValue(0, "https://www.google.com"));

        #endregion
        #region Class Array

        [TestMethod]
        public void Value_ClassArrayArg() =>
            Test(ValueTestType.ClassArrayArg, v =>
                v.HasArrayClassValues(CorElementType.Class, "DebugTools.TestHost.Class1", "DebugTools.TestHost.Class1"));

        [TestMethod]
        public void Value_EmptyClassArrayArg() =>
            Test(ValueTestType.EmptyClassArrayArg, v => v.HasArrayClassValues(CorElementType.Class));

        #endregion
        #region Object Array

        [TestMethod]
        public void Value_ObjectArrayArg() =>
            Test(ValueTestType.ObjectArrayArg, v => v.HasArrayClassValues(CorElementType.Class, "System.Object"));

        [TestMethod]
        public void Value_EmptyObjectArrayArg() =>
            Test(ValueTestType.EmptyObjectArrayArg, v => v.HasArrayValues(CorElementType.Class, new object[0]));

        [TestMethod]
        public void Value_ObjectArrayOfObjectArray() =>
            Test(ValueTestType.ObjectArrayOfObjectArray, v =>
            {
                v.VerifyArray(
                    e1 => e1.VerifyArray(
                        e2 => e2.HasValue(1)
                    ),
                    e1 => e1.VerifyArray(
                        e2 => e2.HasValue("2")
                    )
                );
            });

        #endregion
        #region ValueType Array

        [TestMethod]
        public void Value_ValueTypeArrayArg() =>
            Test(ValueTestType.ValueTypeArrayArg, v => v.HasArrayValues(CorElementType.I4, 1, 2));

        [TestMethod]
        public void Value_EmptyValueTypeArrayArg() =>
            Test(ValueTestType.EmptyValueTypeArrayArg, v => v.HasArrayValues(CorElementType.I4, new int[0]));

        #endregion
        #region Struct

        [TestMethod]
        public void Value_StructArg() =>
            Test(ValueTestType.StructArg, v => v.HasValueType("DebugTools.TestHost.Struct1"));

        [TestMethod]
        public void Value_StructWithFieldArg() =>
            Test(ValueTestType.StructWithFieldArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_StructWithPropertyArg() =>
            Test(ValueTestType.StructWithPropertyArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_ExternalStruct() =>
            Test(ValueTestType.ExternalStruct, v => v.HasFieldValue((ulong) 637949198450000000));

        #endregion
        #region Struct Array

        //todo: externalstruct (resolving datetime) isnt working when we're targeting net5.0

        [TestMethod]
        public void Value_StructArrayArg() =>
            Test(ValueTestType.StructArrayArg, v =>
            {
                v.HasArrayStructValues(CorElementType.ValueType, "DebugTools.TestHost.Struct1WithProperty", "DebugTools.TestHost.Struct1WithProperty");

                v.VerifyArray(
                    e => e.HasFieldValue(1),
                    e => e.HasFieldValue(2)
                );
            });

        [TestMethod]
        public void Value_ExternalStructArrayArg() =>
            Test(ValueTestType.ExternalStructArrayArg, v =>
            {
                v.HasArrayStructValues(CorElementType.ValueType, "System.DateTime", "System.DateTime");

                v.VerifyArray(
                    e => e.HasFieldValue((ulong)637949198450000000),
                    e => e.HasFieldValue((ulong)631006958450000000)
                );
            });

        [TestMethod]
        public void Value_BoxedStructArrayArg() =>
            Test(ValueTestType.BoxedStructArrayArg, v =>
            {
                v.VerifyArray(
                    e => e.HasFieldValue(1),
                    e => e.HasValue("b")
                );
            });

        [TestMethod]
        public void Value_BoxedExternalStructArrayArg() =>
            Test(ValueTestType.BoxedExternalStructArrayArg, v =>
            {
                var parameter = (SZArrayValue)v.GetParameter();

                v.VerifyArray(
                    e => e.HasFieldValue((ulong)637949198450000000),
                    e => e.HasValue("b")
                );
            });

        #endregion

        internal void Test(ValueTestType type, Action<FrameVerifier> validate, params ProfilerSetting[] settings)
        {
            var settingsList = settings.ToList();
            settingsList.Add(ProfilerSetting.Detailed);

            TestInternal(TestType.Value, type.ToString(), v => validate(v.FindFrame(type.ToString()).Verify()), settingsList.ToArray());
        }
    }
}
