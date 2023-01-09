using System;
using System.Collections.Generic;
using System.Diagnostics;
using Profiler.Tests;

namespace DebugTools.TestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            if (args.Length == 0)
            {
                Console.WriteLine("No TestType was specified");
                Environment.Exit(1);
            }

            if (args.Length == 1)
            {
                Console.WriteLine("No sub type was specified");
                Environment.Exit(1);
            }

            var testType = (TestType) Enum.Parse(typeof(TestType), args[0]);

            switch (testType)
            {
                case TestType.Value:
                    ProcessValueTest(args[1]);
                    break;

                case TestType.Profiler:
                    ProcessProfilerTest(args[1]);
                    break;

                case TestType.Exception:
                    ProcessExceptionTest(args[1]);
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run test type '{testType}'");
                    Environment.Exit(1);
                    break;
            }

            Console.WriteLine("Done");
        }

        private static unsafe void ProcessValueTest(string subType)
        {
            var test = (ValueTestType)Enum.Parse(typeof(ValueTestType), subType);

            Debug.WriteLine($"Running test '{test}'");

            var instance = new ValueType();

            switch (test)
            {
                case ValueTestType.NoArgs_ReturnVoid:
                    instance.NoArgs_ReturnVoid();
                    break;

                case ValueTestType.OneArg_ReturnVoid:
                    instance.OneArg_ReturnVoid(1);
                    break;

                #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

                case ValueTestType.BoolArg:
                    instance.BoolArg(true);
                    break;

                case ValueTestType.CharArg:
                    instance.CharArg('b');
                    break;

                case ValueTestType.ByteArg:
                    instance.ByteArg(1);
                    break;

                case ValueTestType.SByteArg:
                    instance.SByteArg(1);
                    break;

                case ValueTestType.Int16Arg:
                    instance.Int16Arg(1);
                    break;

                case ValueTestType.UInt16Arg:
                    instance.UInt16Arg(1);
                    break;

                case ValueTestType.Int32Arg:
                    instance.Int32Arg(1);
                    break;

                case ValueTestType.UInt32Arg:
                    instance.UInt32Arg(1);
                    break;

                case ValueTestType.Int64Arg:
                    instance.Int64Arg(1);
                    break;

                case ValueTestType.UInt64Arg:
                    instance.UInt64Arg(1);
                    break;

                case ValueTestType.FloatArg:
                    instance.FloatArg(1.1f);
                    break;

                case ValueTestType.DoubleArg:
                    instance.DoubleArg(1.1);
                    break;

                case ValueTestType.IntPtrArg:
                    instance.IntPtrArg(new IntPtr(1));
                    break;

                case ValueTestType.UIntPtrArg:
                    instance.UIntPtrArg(new UIntPtr(1));
                    break;

                #endregion
                #region Ptr

                case ValueTestType.PtrArg:
                    fixed (int* ptr = &new[] { 1001 }[0])
                        instance.PtrArg(ptr);
                    break;

                case ValueTestType.PtrCharArg:
                    fixed (char* ptr = "String Value")
                        instance.PtrCharArg(ptr);
                    break;

                case ValueTestType.PtrVoidArg:
                        instance.PtrVoidArg((void*)1001);
                    break;

                case ValueTestType.PtrStructArg:
                    fixed (Struct1WithField* ptr = &new[] { new Struct1WithField { field1 = 1001 } }[0])
                        instance.PtrStructArg(ptr);
                    break;

                case ValueTestType.PtrComplexStructArg:
                {
                    fixed (char* charPtr = "String Value")
                    fixed (Struct1WithField* structPtr = new[] { new Struct1WithField { field1 = 1002 } })
                    fixed (ComplexPtrStruct* complexPtr = new[] {new ComplexPtrStruct
                    {
                        CharPtr = charPtr,
                        CharVal = 'X',
                        Struct = new Struct1WithField { field1 = 1001 },
                        StructPtr = structPtr
                    }})
                    {
                        instance.PtrComplexStructArg(complexPtr);
                    }
                    break;
                }

                #endregion
                #region PtrPtr

                case ValueTestType.PtrPtrArg:
                    fixed (int* ptr = &new[] { 1001 }[0])
                        instance.PtrPtrArg(&ptr);
                    break;

                case ValueTestType.PtrPtrCharArg:
                    fixed (char* ptr = "String Value")
                        instance.PtrPtrCharArg(&ptr);
                    break;

                case ValueTestType.PtrPtrVoidArg:
                {
                    void* ptr = (void*)1001;
                    instance.PtrPtrVoidArg(&ptr);
                    break;
                }

                case ValueTestType.PtrPtrStructArg:
                    fixed (Struct1WithField* ptr = &new[] { new Struct1WithField { field1 = 1001 } }[0])
                        instance.PtrPtrStructArg(&ptr);
                    break;

                case ValueTestType.PtrPtrComplexStructArg:
                {
                    fixed (char* charPtr = "String Value")
                    fixed (Struct1WithField* structPtr = new[] { new Struct1WithField { field1 = 1002 } })
                    fixed (ComplexPtrStruct* complexPtr = new[] {new ComplexPtrStruct
                    {
                        CharPtr = charPtr,
                        CharVal = 'X',
                        Struct = new Struct1WithField { field1 = 1001 },
                        StructPtr = structPtr
                    }})
                    {
                        instance.PtrPtrComplexStructArg(&complexPtr);
                    }
                    break;
                }

                #endregion
                #region Ptr Array

                case ValueTestType.PtrArrayArg:
                {
                    var arr = new[] {1001,1002};

                    fixed (int* ptr1 = &arr[0])
                    fixed (int* ptr2 = &arr[1])
                    {
                        instance.PtrArrayArg(new[]{ptr1, ptr2});
                    }

                    break;
                }

                case ValueTestType.PtrCharArrayArg:
                {
                    fixed (char* ptr1 = "First")
                    fixed (char* ptr2 = "Second")
                    {
                        instance.PtrCharArrayArg(new[] { ptr1, ptr2 });
                    }

                    break;
                }

                case ValueTestType.PtrVoidArrayArg:
                    instance.PtrVoidArrayArg(new[]{(void*) 1001, (void*) 1002});
                    break;

                case ValueTestType.PtrStructArrayArg:
                {
                    var arr = new[]
                    {
                        new Struct1WithField {field1 = 1001},
                        new Struct1WithField {field1 = 1002}
                    };

                    fixed (Struct1WithField* ptr1 = &arr[0])
                    fixed (Struct1WithField* ptr2 = &arr[1])
                    {
                        instance.PtrStructArrayArg(new[]{ptr1, ptr2});
                    }

                    break;
                }

                case ValueTestType.PtrComplexStructArrayArg:
                {
                    fixed (char* charPtr1 = "First")
                    fixed (char* charPtr2 = "Second")
                    fixed (Struct1WithField* structPtr1 = new[] { new Struct1WithField { field1 = 2001 } })
                    fixed (Struct1WithField* structPtr2 = new[] { new Struct1WithField { field1 = 2002 } })
                    {
                        var arr = new[]
                        {
                            new ComplexPtrStruct
                            {
                                CharPtr = charPtr1,
                                CharVal = 'X',
                                Struct = new Struct1WithField {field1 = 1001},
                                StructPtr = structPtr1
                            },
                            new ComplexPtrStruct
                            {
                                CharPtr = charPtr2,
                                CharVal = 'Y',
                                Struct = new Struct1WithField {field1 = 1002},
                                StructPtr = structPtr2
                            }
                        };

                        fixed (ComplexPtrStruct* complex1 = &arr[0])
                        fixed (ComplexPtrStruct* complex2 = &arr[1])
                        {
                            instance.PtrComplexStructArrayArg(new[] {complex1, complex2});
                        }
                    }

                    break;
                }

                #endregion

                case ValueTestType.DecimalArg:
                    instance.DecimalArg(1m);
                    break;

                case ValueTestType.StringArg:
                    instance.StringArg("foo");
                    break;

                case ValueTestType.ObjectArg:
                    instance.ObjectArg(new object());
                    break;

                case ValueTestType.InterfaceArg:
                    instance.InterfaceArg(new ImplInterface());
                    break;

                case ValueTestType.NestedNestedExternalType:
                    instance.NestedNestedExternalType(new Dictionary<string, int>{{"a", 1}}.Values.GetEnumerator());
                    break;

                case ValueTestType.GenericWithObjectTypeArg:
                    instance.GenericWithObjectTypeArg(new GenericValueTypeType<object>{field1 = new object()});
                    break;

                case ValueTestType.GenericClassArg:
                    instance.GenericClassArg(new GenericClassType<Class1WithField>
                    {
                        field1 = new Class1WithField
                        {
                            field1 = 1
                        }
                    });
                    break;

                case ValueTestType.GenericTwoTypeArgs:
                    instance.GenericTwoTypeArgs(new Dictionary<string, int>
                    {
                        { "first", 1 },
                        { "second", 2 }
                    });
                    break;

                case ValueTestType.GenericValueTypeArg:
                    instance.GenericValueTypeArg(new GenericValueTypeType<Struct1WithField>
                    {
                        field1 = new Struct1WithField
                        {
                            field1 = 1
                        }
                    });
                    break;

                case ValueTestType.GenericClass_ToObjectArg:
                    instance.GenericClass_ToObjectArg(new GenericClassType<Class1WithField>
                    {
                        field1 = new Class1WithField
                        {
                            field1 = 1
                        }
                    });
                    break;

                case ValueTestType.GenericValueType_ToObjectArg:
                    instance.GenericValueType_ToObjectArg(new GenericValueTypeType<Struct1WithField>
                    {
                        field1 = new Struct1WithField
                        {
                            field1 = 1
                        }
                    });
                    break;

                #region MethodVar

                case ValueTestType.Generic_MethodVar_ElementTypeClassArg:
                    instance.Generic_MethodVar_ElementTypeClassArg(new Class1WithField {field1 = 1});
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeValueTypeArg:
                    instance.Generic_MethodVar_ElementTypeValueTypeArg(new Struct1WithField { field1 = 1 });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeSimpleArg:
                    instance.Generic_MethodVar_ElementTypeSimpleArg(1);
                    break;

                #endregion
                #region MethodVar Array

                case ValueTestType.Generic_MethodVar_ElementTypeClassArrayArg:
                    instance.Generic_MethodVar_ElementTypeClassArrayArg(new[]{ new Class1WithField { field1 = 1 }, new Class1WithField { field1 = 2 }});
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeValueTypeArrayArg:
                    instance.Generic_MethodVar_ElementTypeValueTypeArrayArg(new []{new Struct1WithField { field1 = 1 }, new Struct1WithField { field1 = 2 }});
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeSimpleArrayArg:
                    instance.Generic_MethodVar_ElementTypeSimpleArrayArg(new[]{1, 2});
                    break;

                #endregion
                #region MethodVar Generic Value

                case ValueTestType.Generic_MethodVar_ElementTypeGenericClassArg:
                    instance.Generic_MethodVar_ElementTypeGenericClassArg(new GenericClassType<Class1WithField>{field1 = new Class1WithField
                    {
                        field1 = 1
                    }});
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericClassArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericClassArrayArg(new GenericClassType<Class1WithField[]>
                    {
                        field1 = new[]
                        {
                            new Class1WithField
                            {
                                field1 = 1
                            },
                            new Class1WithField
                            {
                                field1 = 2
                            }
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueTypeArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueTypeArg(new GenericValueTypeType<Struct1WithField>
                    {
                        field1 = new Struct1WithField
                        {
                            field1 = 1
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueTypeArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueTypeArrayArg(new GenericValueTypeType<Struct1WithField[]>
                    {
                        field1 = new[]
                        {
                            new Struct1WithField
                            {
                                field1 = 1
                            },
                            new Struct1WithField
                            {
                                field1 = 2
                            }
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SimpleArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_SimpleArg(new GenericValueTypeType<int>
                    {
                        field1 = 1
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeNullablePrimitive:
                    instance.Generic_MethodVar_ElementTypeNullablePrimitive(1);
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeNullableValueType:
                    instance.Generic_MethodVar_ElementTypeNullableValueType(new DateTime(2022, 8, 1, 3, 4, 5));
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg(new[]
                    {
                        new GenericValueTypeType<Struct1WithField>{field1 = new Struct1WithField{field1 = 1}},
                        new GenericValueTypeType<Struct1WithField>{field1 = new Struct1WithField{field1 = 2}}
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg(new GenericValueTypeType<Struct1WithField[]>
                    {
                        field1 = new[]
                        {
                            new Struct1WithField{field1 = 1},
                            new Struct1WithField{field1 = 2}
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg(new[,]
                    {
                        {
                            new GenericValueTypeType<Struct1WithField>{field1 = new Struct1WithField{field1 = 1}},
                            new GenericValueTypeType<Struct1WithField>{field1 = new Struct1WithField{field1 = 2}}
                        },
                        {
                            new GenericValueTypeType<Struct1WithField>{field1 = new Struct1WithField{field1 = 3}},
                            new GenericValueTypeType<Struct1WithField>{field1 = new Struct1WithField{field1 = 4}}
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg(new GenericValueTypeType<Struct1WithField[,]>
                    {
                        field1 = new[,]
                        {
                            { new Struct1WithField{field1 = 1}, new Struct1WithField{field1 = 2} },
                            { new Struct1WithField{field1 = 3}, new Struct1WithField{field1 = 4} }
                        }
                    });
                    break;

                #endregion
                #region TypeVar

                case ValueTestType.Generic_TypeVar_ElementTypeClassArg:
                    new GenericValueType<Class1WithField>().Generic_TypeVar_ElementTypeClassArg(new Class1WithField { field1 = 1 });
                    break;

                case ValueTestType.Generic_TypeVar_ElementTypeValueTypeArg:
                    new GenericValueType<Struct1WithField>().Generic_TypeVar_ElementTypeValueTypeArg(new Struct1WithField { field1 = 1 });
                    break;

                case ValueTestType.Generic_TypeVar_ElementTypeSimpleArg:
                    new GenericValueType<int>().Generic_TypeVar_ElementTypeSimpleArg(1);
                    break;

                #endregion
                #region TypeVar Array

                case ValueTestType.Generic_TypeVar_ElementTypeClassArrayArg:
                    new GenericValueType<Class1WithField[]>().Generic_TypeVar_ElementTypeClassArrayArg(new[]{new Class1WithField { field1 = 1 },new Class1WithField { field1 = 2 }});
                    break;

                case ValueTestType.Generic_TypeVar_ElementTypeValueTypeArrayArg:
                    new GenericValueType<Struct1WithField[]>().Generic_TypeVar_ElementTypeValueTypeArrayArg(new[]{new Struct1WithField { field1 = 1 },new Struct1WithField { field1 = 2 }});
                    break;

                case ValueTestType.Generic_TypeVar_ElementTypeSimpleArrayArg:
                    new GenericValueType<int[]>().Generic_TypeVar_ElementTypeSimpleArrayArg(new[] {1,2});
                    break;

                #endregion

                case ValueTestType.EmptyStringArg:
                    instance.EmptyStringArg(string.Empty);
                    break;

                case ValueTestType.NullStringArg:
                    instance.NullStringArg(null);
                    break;

                case ValueTestType.StringArrayArg:
                    instance.StringArrayArg(new string[] { "a", "b" });
                    break;

                case ValueTestType.EmptyStringArrayArg:
                    instance.EmptyStringArrayArg(new string[0]);
                    break;

                case ValueTestType.ObjectArrayContainingStringArg:
                    instance.ObjectArrayContainingStringArg(new object[] { "a" });
                    break;

                case ValueTestType.ClassArg:
                    instance.ClassArg(new Class1());
                    break;

                case ValueTestType.ClassWithFieldArg:
                    instance.ClassWithFieldArg(new Class1WithField{field1 = 1});
                    break;

                case ValueTestType.ClassWithPropertyArg:
                    instance.ClassWithPropertyArg(new Class1WithProperty{Property1 = 1});
                    break;

                case ValueTestType.ExternalClass:
                    instance.ExternalClass(new Uri("https://www.google.com"));
                    break;

                case ValueTestType.ClassArrayArg:
                    instance.ClassArrayArg(new Class1[]{new Class1(), new Class1()});
                    break;

                case ValueTestType.EmptyClassArrayArg:
                    instance.EmptyClassArrayArg(new Class1[0]);
                    break;

                case ValueTestType.ObjectArrayArg:
                    instance.ObjectArrayArg(new[] { new object() });
                    break;

                case ValueTestType.EmptyObjectArrayArg:
                    instance.EmptyObjectArrayArg(new object[0]);
                    break;

                case ValueTestType.ObjectArrayOfObjectArray:
                    instance.ObjectArrayOfObjectArray(new object[]
                    {
                        new object[] {1},
                        new object[] {"2"}
                    });
                    break;

                case ValueTestType.ValueTypeArrayArg:
                    instance.ValueTypeArrayArg(new int[] {1, 2});
                    break;

                case ValueTestType.EmptyValueTypeArrayArg:
                    instance.EmptyValueTypeArrayArg(new int[0]);
                    break;

                case ValueTestType.StructArg:
                    instance.StructArg(new Struct1());
                    break;

                case ValueTestType.StructWithFieldArg:
                    instance.StructWithFieldArg(new Struct1WithField {field1 = 1});
                    break;

                case ValueTestType.StructWithPropertyArg:
                    instance.StructWithPropertyArg(new Struct1WithProperty {Property1 = 1});
                    break;

                case ValueTestType.ExternalStruct:
                    instance.ExternalStruct(new DateTime(2022, 8, 1, 3, 4, 5));
                    break;

                case ValueTestType.StructArrayArg:
                    instance.StructArrayArg(new[]{new Struct1WithProperty { Property1 = 1 }, new Struct1WithProperty { Property1 = 2 }});
                    break;

                case ValueTestType.ExternalStructArrayArg:
                    instance.ExternalStructArrayArg(new[]{ new DateTime(2022, 8, 1, 3, 4, 5) , new DateTime(2000, 8, 1, 3, 4, 5) });
                    break;

                case ValueTestType.BoxedStructArrayArg:
                    instance.BoxedStructArrayArg(new object[] { new Struct1WithProperty { Property1 = 1 }, "b" });
                    break;

                case ValueTestType.BoxedExternalStructArrayArg:
                    instance.BoxedExternalStructArrayArg(new object[] { new DateTime(2022, 8, 1, 3, 4, 5), "b" });
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(2);
                    break;
            }
        }

        private static void ProcessProfilerTest(string subType)
        {
            var test = (ProfilerTestType)Enum.Parse(typeof(ProfilerTestType), subType);

            Debug.WriteLine($"Running test '{test}'");

            var instance = new ProfilerType();

            switch (test)
            {
                case ProfilerTestType.NoArgs:
                    instance.NoArgs();
                    break;

                case ProfilerTestType.SingleChild:
                    instance.SingleChild();
                    break;

                case ProfilerTestType.TwoChildren:
                    instance.TwoChildren();
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(2);
                    break;
            }
        }

        private static void ProcessExceptionTest(string subType)
        {
            var test = (ExceptionTestType)Enum.Parse(typeof(ExceptionTestType), subType);

            Debug.WriteLine($"Running test '{test}'");

            var instance = new ExceptionType();

            switch (test)
            {
                case ExceptionTestType.CaughtWithinMethod:
                    instance.CaughtWithinMethod();
                    break;

                case ExceptionTestType.UnwindOneFrame:
                    instance.UnwindOneFrame1();
                    break;

                case ExceptionTestType.Nested_ThrownInCatchAndImmediatelyCaught:
                    instance.Nested_ThrownInCatchAndImmediatelyCaught();
                    break;

                case ExceptionTestType.Nested_ThrownInCatchAndCaughtByOuterCatch:
                    instance.Nested_ThrownInCatchAndCaughtByOuterCatch();
                    break;

                case ExceptionTestType.Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch:
                    instance.Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch1();
                    break;

                case ExceptionTestType.Nested_CaughtByOuterCatch:
                    instance.Nested_CaughtByOuterCatch();
                    break;

                case ExceptionTestType.Nested_UnwindOneFrameFromThrowInCatch:
                    instance.Nested_UnwindOneFrameFromThrowInCatch1();
                    break;

                case ExceptionTestType.Nested_UnwindTwoFramesFromThrowInCatch:
                    instance.Nested_UnwindTwoFramesFromThrowInCatch1();
                    break;

                case ExceptionTestType.Nested_ThrownInFinallyAndImmediatelyCaught:
                    instance.Nested_ThrownInFinallyAndImmediatelyCaught();
                    break;

                case ExceptionTestType.Nested_ThrownInFinallyAndUnwindOneFrame:
                    instance.Nested_ThrownInFinallyAndUnwindOneFrame1();
                    break;

                case ExceptionTestType.Nested_ThrownInFinallyAndUnwindTwoFrames:
                    instance.Nested_ThrownInFinallyAndUnwindTwoFrames1();
                    break;

                case ExceptionTestType.NoCatchThrowWithFinallyUnwindOneFrame:
                    instance.NoCatchThrowWithFinallyUnwindOneFrame1();
                    break;

                case ExceptionTestType.NoCatchThrowInFinallyUnwindOneFrame:
                    instance.NoCatchThrowInFinallyUnwindOneFrame1();
                    break;

                case ExceptionTestType.UncaughtInNative:
                    instance.UncaughtInNative();
                    break;

                case ExceptionTestType.CaughtInNative:
                    instance.CaughtInNative();
                    break;

                case ExceptionTestType.Rethrow:
                    instance.Rethrow();
                    break;

                case ExceptionTestType.CallFunctionInCatchAndThrow:
                    instance.CallFunctionInCatchAndThrow1();
                    break;

                case ExceptionTestType.ThrownInFilterAndCaught:
                    instance.ThrownInFilterAndCaught();
                    break;

                case ExceptionTestType.ThrownInFilterAndNotCaught:
                    instance.ThrownInFilterAndNotCaught();
                    break;

                case ExceptionTestType.ThrownInFilterThatUnwindsOneFrameAndNotCaught:
                    instance.ThrownInFilterThatUnwindsOneFrameAndNotCaught();
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(2);
                    break;
            }
        }
    }
}
