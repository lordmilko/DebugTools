using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

                case TestType.Blacklist:
                    ProcessBlacklistTest(args[1]);
                    break;

                case TestType.StaticField:
                    ProcessStaticFieldTest(args[1], args.Skip(2).ToArray());
                    break;

                case TestType.SOS:
                    var parentProcess = Process.GetProcessById(Convert.ToInt32(args[1]));
                    parentProcess.EnableRaisingEvents = true;
                    parentProcess.Exited += (s, e) => Process.GetCurrentProcess().Kill();
                    Console.WriteLine("Waiting for parent process exit...");

                    while (true)
                        Thread.Sleep(1);
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

            object objRef;
            string strRef;
            int intRef;
            int* ptrRef;

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

                case ValueTestType.PtrCharRandomValueArg:
                    instance.PtrCharRandomValueArg((char*)1);
                    break;

                case ValueTestType.PtrVoidArg:
                        instance.PtrVoidArg((void*)1001);
                    break;

                case ValueTestType.PtrStructArg:
                    fixed (StructWithPrimitiveField* ptr = &new[] { new StructWithPrimitiveField { field1 = 1001 } }[0])
                        instance.PtrStructArg(ptr);
                    break;

                case ValueTestType.PtrComplexStructArg:
                {
                    fixed (char* charPtr = "String Value")
                    fixed (StructWithPrimitiveField* structPtr = new[] { new StructWithPrimitiveField { field1 = 1002 } })
                    fixed (ComplexPtrStruct* complexPtr = new[] {new ComplexPtrStruct
                    {
                        CharPtr = charPtr,
                        CharVal = 'X',
                        Struct = new StructWithPrimitiveField { field1 = 1001 },
                        StructPtr = structPtr
                    }})
                    {
                        instance.PtrComplexStructArg(complexPtr);
                    }
                    break;
                }

                case ValueTestType.PtrAddressOnly:
                    fixed (StructWithPrimitiveField* ptr = &new[] { new StructWithPrimitiveField { field1 = 1001 } }[0])
                        instance.PtrAddressOnly(ptr);
                    break;

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
                    fixed (StructWithPrimitiveField* ptr = &new[] { new StructWithPrimitiveField { field1 = 1001 } }[0])
                        instance.PtrPtrStructArg(&ptr);
                    break;

                case ValueTestType.PtrPtrComplexStructArg:
                {
                    fixed (char* charPtr = "String Value")
                    fixed (StructWithPrimitiveField* structPtr = new[] { new StructWithPrimitiveField { field1 = 1002 } })
                    fixed (ComplexPtrStruct* complexPtr = new[] {new ComplexPtrStruct
                    {
                        CharPtr = charPtr,
                        CharVal = 'X',
                        Struct = new StructWithPrimitiveField { field1 = 1001 },
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
                        new StructWithPrimitiveField {field1 = 1001},
                        new StructWithPrimitiveField {field1 = 1002}
                    };

                    fixed (StructWithPrimitiveField* ptr1 = &arr[0])
                    fixed (StructWithPrimitiveField* ptr2 = &arr[1])
                    {
                        instance.PtrStructArrayArg(new[]{ptr1, ptr2});
                    }

                    break;
                }

                case ValueTestType.PtrComplexStructArrayArg:
                {
                    fixed (char* charPtr1 = "First")
                    fixed (char* charPtr2 = "Second")
                    fixed (StructWithPrimitiveField* structPtr1 = new[] { new StructWithPrimitiveField { field1 = 2001 } })
                    fixed (StructWithPrimitiveField* structPtr2 = new[] { new StructWithPrimitiveField { field1 = 2002 } })
                    {
                        var arr = new[]
                        {
                            new ComplexPtrStruct
                            {
                                CharPtr = charPtr1,
                                CharVal = 'X',
                                Struct = new StructWithPrimitiveField {field1 = 1001},
                                StructPtr = structPtr1
                            },
                            new ComplexPtrStruct
                            {
                                CharPtr = charPtr2,
                                CharVal = 'Y',
                                Struct = new StructWithPrimitiveField {field1 = 1002},
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
                #region ByRef

                case ValueTestType.ByRef_Ref_BoxedValue:
                    objRef = 1;
                    instance.ByRef_Ref_BoxedValue(ref objRef);
                    break;

                #region ByRef Null

                case ValueTestType.ByRef_Ref_InNull_OutNull:
                    strRef = null;
                    instance.ByRef_Ref_InNull_OutNull(ref strRef);
                    break;

                case ValueTestType.ByRef_Ref_InNull_OutValue:
                    strRef = null;
                    instance.ByRef_Ref_InNull_OutValue(ref strRef);
                    break;

                case ValueTestType.ByRef_Ref_InNonNullValue_OutValue:
                    strRef = "oldValue";
                    instance.ByRef_Ref_InNonNullValue_OutValue(ref strRef);
                    break;

                case ValueTestType.ByRef_Out_Nullable_WithNull:
                    strRef = null;
                    instance.ByRef_Out_Nullable_WithNull(out strRef);
                    break;

                case ValueTestType.ByRef_Out_Nullable_WithNonNull:
                    strRef = null;
                    instance.ByRef_Out_Nullable_WithNonNull(out strRef);
                    break;

                case ValueTestType.ByRef_Out_NonNullNullable_WithNull:
                    strRef = "value";
                    instance.ByRef_Out_NonNullNullable_WithNull(out strRef);
                    break;

                #endregion
                #region ByRef Number

                case ValueTestType.ByRef_Ref_InZero_OutZero:
                    intRef = 0;
                    instance.ByRef_Ref_InZero_OutZero(ref intRef);
                    break;

                case ValueTestType.ByRef_Ref_InZero_OutValue:
                    intRef = 0;
                    instance.ByRef_Ref_InZero_OutValue(ref intRef);
                    break;

                case ValueTestType.ByRef_Ref_InNonZero_OutValue:
                    intRef = 1;
                    instance.ByRef_Ref_InNonZero_OutValue(ref intRef);
                    break;

                case ValueTestType.ByRef_Out_Number_WithZero:
                    intRef = 0;
                    instance.ByRef_Out_Number_WithZero(out intRef);
                    break;

                case ValueTestType.ByRef_Out_Number_WithNonZero:
                    intRef = 0;
                    instance.ByRef_Out_Number_WithNonZero(out intRef);
                    break;

                case ValueTestType.ByRef_Out_NonZeroNumber_WithNonZero:
                    intRef = 1;
                    instance.ByRef_Out_NonZeroNumber_WithNonZero(out intRef);
                    break;

                #endregion
                #region ByRef Ptr

                case ValueTestType.ByRef_Ref_InPtrZero_OutZero:
                    ptrRef = (int*) 0;
                    instance.ByRef_Ref_InPtrZero_OutZero(ref ptrRef);
                    break;

                case ValueTestType.ByRef_Ref_InPtrZero_OutValue:
                    ptrRef = (int*) 0;
                    instance.ByRef_Ref_InPtrZero_OutValue(ref ptrRef);
                    break;

                case ValueTestType.ByRef_Ref_InPtrNonZero_OutValue:
                    ptrRef = (int*) 1;
                    instance.ByRef_Ref_InPtrNonZero_OutValue(ref ptrRef);
                    break;

                case ValueTestType.ByRef_Out_Ptr_WithZero:
                    ptrRef = (int*) 0;
                    instance.ByRef_Out_Ptr_WithZero(out ptrRef);
                    break;

                case ValueTestType.ByRef_Out_Ptr_NonWithZero:
                    ptrRef = (int*) 0;
                    instance.ByRef_Out_Ptr_NonWithZero(out ptrRef);
                    break;

                case ValueTestType.ByRef_Out_NonZeroPtr_NonWithZero:
                    ptrRef = (int*) 1;
                    instance.ByRef_Out_NonZeroPtr_NonWithZero(out ptrRef);
                    break;

                #endregion
                #region Ref Return

                case ValueTestType.ByRef_RefReturn_Struct:
                {
                    var value = new Struct1();
                    instance.ByRef_RefReturn_Struct(ref value);
                    break;
                }

                case ValueTestType.ByRef_RefReturn_StructWithPrimitiveField:
                {
                    var value = new StructWithPrimitiveField{field1 = 1};
                    instance.ByRef_RefReturn_StructWithPrimitiveField(ref value);
                    break;
                }

                case ValueTestType.ByRef_RefReturn_StructWithReferenceField:
                {
                    var value = new StructWithReferenceField{field1 = "foo"};
                    instance.ByRef_RefReturn_StructWithReferenceField(ref value);
                    break;
                }

                #region MVar

                case ValueTestType.ByRef_RefReturn_Generic_MVar_PrimitiveValue:
                {
                    var value = 1;
                    instance.ByRef_RefReturn_Generic_MVar_PrimitiveValue(ref value);
                    break;
                }

                case ValueTestType.ByRef_RefReturn_Generic_MVar_NullablePrimitiveValue:
                {
                    int? value = 1;
                    instance.ByRef_RefReturn_Generic_MVar_NullablePrimitiveValue(ref value);
                    break;
                }

                case ValueTestType.ByRef_RefReturn_Generic_MVar_StructWithPrimitiveField:
                {
                    var value = new StructWithPrimitiveField { field1 = 1 };
                    instance.ByRef_RefReturn_Generic_MVar_StructWithPrimitiveField(ref value);
                    break;
                }

                #endregion
                #region Var

                case ValueTestType.ByRef_RefReturn_Generic_Var_PrimitiveValue:
                {
                    var value = 1;
                    new GenericValueType<int>().ByRef_RefReturn_Generic_Var_PrimitiveValue(ref value);
                    break;
                }

                case ValueTestType.ByRef_RefReturn_Generic_Var_NullablePrimitiveValue:
                {
                    int? value = 1;
                    new GenericValueType<int?>().ByRef_RefReturn_Generic_Var_NullablePrimitiveValue(ref value);
                    break;
                }

                case ValueTestType.ByRef_RefReturn_Generic_Var_StructWithPrimitiveField:
                {
                    var value = new StructWithPrimitiveField { field1 = 1 };
                    new GenericValueType<StructWithPrimitiveField>().ByRef_RefReturn_Generic_Var_StructWithPrimitiveField(ref value);
                    break;
                }

                #endregion
                #region Var Field

                case ValueTestType.ByRef_RefReturn_Generic_VarField_PrimitiveValue:
                    new GenericValueType<int>{ field1 = 1 }.ByRef_RefReturn_Generic_VarField_PrimitiveValue();
                    break;

                case ValueTestType.ByRef_RefReturn_Generic_VarField_NullablePrimitiveValue:
                    new GenericValueType<int?>{ field1 = 1 }.ByRef_RefReturn_Generic_VarField_NullablePrimitiveValue();
                    break;

                case ValueTestType.ByRef_RefReturn_Generic_VarField_StructWithPrimitiveField:
                    new GenericValueType<StructWithPrimitiveField>{ field1 = new StructWithPrimitiveField {field1 = 1} }.ByRef_RefReturn_Generic_VarField_StructWithPrimitiveField();
                    break;

                #endregion
                #endregion
                #endregion

                case ValueTestType.FnPtrArg:
                    static void localFn() { }

                    instance.FnPtrArg(&localFn);
                    break;

                case ValueTestType.FnPtrNullArg:
                    instance.FnPtrNullArg((delegate*<void>)new IntPtr(0));
                    break;

                case ValueTestType.DecimalArg:
                    instance.DecimalArg(1m);
                    break;

                case ValueTestType.StringArg:
                    instance.StringArg("foo");
                    break;

                case ValueTestType.ObjectArg:
                    instance.ObjectArg(new object());
                    break;

                case ValueTestType.ObjectArg_TraceDepth:
                    instance.ObjectArg_TraceDepth("foo");
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
                    instance.GenericValueTypeArg(new GenericValueTypeType<StructWithPrimitiveField>
                    {
                        field1 = new StructWithPrimitiveField
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
                    instance.GenericValueType_ToObjectArg(new GenericValueTypeType<StructWithPrimitiveField>
                    {
                        field1 = new StructWithPrimitiveField
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
                    instance.Generic_MethodVar_ElementTypeValueTypeArg(new StructWithPrimitiveField { field1 = 1 });
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
                    instance.Generic_MethodVar_ElementTypeValueTypeArrayArg(new []{new StructWithPrimitiveField { field1 = 1 }, new StructWithPrimitiveField { field1 = 2 }});
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
                    instance.Generic_MethodVar_ElementTypeGenericValueTypeArg(new GenericValueTypeType<StructWithPrimitiveField>
                    {
                        field1 = new StructWithPrimitiveField
                        {
                            field1 = 1
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueTypeArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueTypeArrayArg(new GenericValueTypeType<StructWithPrimitiveField[]>
                    {
                        field1 = new[]
                        {
                            new StructWithPrimitiveField
                            {
                                field1 = 1
                            },
                            new StructWithPrimitiveField
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
                        new GenericValueTypeType<StructWithPrimitiveField>{field1 = new StructWithPrimitiveField{field1 = 1}},
                        new GenericValueTypeType<StructWithPrimitiveField>{field1 = new StructWithPrimitiveField{field1 = 2}}
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg(new GenericValueTypeType<StructWithPrimitiveField[]>
                    {
                        field1 = new[]
                        {
                            new StructWithPrimitiveField{field1 = 1},
                            new StructWithPrimitiveField{field1 = 2}
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg(new[,]
                    {
                        {
                            new GenericValueTypeType<StructWithPrimitiveField>{field1 = new StructWithPrimitiveField{field1 = 1}},
                            new GenericValueTypeType<StructWithPrimitiveField>{field1 = new StructWithPrimitiveField{field1 = 2}}
                        },
                        {
                            new GenericValueTypeType<StructWithPrimitiveField>{field1 = new StructWithPrimitiveField{field1 = 3}},
                            new GenericValueTypeType<StructWithPrimitiveField>{field1 = new StructWithPrimitiveField{field1 = 4}}
                        }
                    });
                    break;

                case ValueTestType.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg:
                    instance.Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg(new GenericValueTypeType<StructWithPrimitiveField[,]>
                    {
                        field1 = new[,]
                        {
                            { new StructWithPrimitiveField{field1 = 1}, new StructWithPrimitiveField{field1 = 2} },
                            { new StructWithPrimitiveField{field1 = 3}, new StructWithPrimitiveField{field1 = 4} }
                        }
                    });
                    break;

                #endregion
                #region TypeVar

                case ValueTestType.Generic_TypeVar_ElementTypeClassArg:
                    new GenericValueType<Class1WithField>().Generic_TypeVar_ElementTypeClassArg(new Class1WithField { field1 = 1 });
                    break;

                case ValueTestType.Generic_TypeVar_ElementTypeValueTypeArg:
                    new GenericValueType<StructWithPrimitiveField>().Generic_TypeVar_ElementTypeValueTypeArg(new StructWithPrimitiveField { field1 = 1 });
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
                    new GenericValueType<StructWithPrimitiveField[]>().Generic_TypeVar_ElementTypeValueTypeArrayArg(new[]{new StructWithPrimitiveField { field1 = 1 },new StructWithPrimitiveField { field1 = 2 }});
                    break;

                case ValueTestType.Generic_TypeVar_ElementTypeSimpleArrayArg:
                    new GenericValueType<int[]>().Generic_TypeVar_ElementTypeSimpleArrayArg(new[] {1,2});
                    break;

                case ValueTestType.Generic_TypeVar_ReturnGenericTypeWithTypeArg:
                    new GenericValueType<StructWithPrimitiveField>().Generic_TypeVar_ReturnGenericTypeWithTypeArg(new StructWithPrimitiveField
                    {
                        field1 = 1
                    });
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

                case ValueTestType.StringArrayArg_TraceDepth:
                    instance.StringArrayArg_TraceDepth(new string[] { "a", "b" });
                    break;

                case ValueTestType.EmptyStringArrayArg:
                    instance.EmptyStringArrayArg(new string[0]);
                    break;

                case ValueTestType.ObjectArrayContainingStringArg:
                    instance.ObjectArrayContainingStringArg(new object[] { "a" });
                    break;

                case ValueTestType.MultiArrayThreeDimensionsArg:
                    instance.MultiArrayThreeDimensionsArg(new int[2,3,4]
                    {
                        //Two outer arrays
                        {
                            //Containing 3 inner arrays
                            //Each containing 4 elements
                            {1,2,3,4},
                            {5,6,7,8},
                            {9,10,11,12}
                        },
                        {
                            {13,14,15,16},
                            {17,18,19,20},
                            {21,22,23,24}
                        }
                    });
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

                #region Struct

                case ValueTestType.StructArg:
                    instance.StructArg(new Struct1());
                    break;

                case ValueTestType.StructWithPrimitiveFieldArg:
                    instance.StructWithPrimitiveFieldArg(new StructWithPrimitiveField {field1 = 1});
                    break;

                case ValueTestType.StructWithReferenceFieldArg:
                    instance.StructWithReferenceFieldArg(new StructWithReferenceField {field1 = "foo"});
                    break;

                case ValueTestType.StructWithPrimitivePropertyArg:
                    instance.StructWithPrimitivePropertyArg(new StructWithPrimitiveProperty { Property1 = 1});
                    break;

                case ValueTestType.StructWithReferencePropertyArg:
                    instance.StructWithReferencePropertyArg(new StructWithReferenceProperty {Property1 = "foo"});
                    break;

                case ValueTestType.ExternalStruct:
                    instance.ExternalStruct(new DateTime(2022, 8, 1, 3, 4, 5));
                    break;

                #endregion
                #region Struct Array

                case ValueTestType.StructWithPrimitiveFieldArrayArg:
                    instance.StructWithPrimitiveFieldArrayArg(new[]
                    {
                        new StructWithPrimitiveField { field1 = 1 },
                        new StructWithPrimitiveField { field1 = 2 }
                    });
                    break;

                case ValueTestType.StructWithReferenceFieldArrayArg:
                    instance.StructWithReferenceFieldArrayArg(new[]
                    {
                        new StructWithReferenceField { field1 = "foo" },
                        new StructWithReferenceField { field1 = "bar" }
                    });
                    break;

                case ValueTestType.StructWithPrimitivePropertyArrayArg:
                    instance.StructWithPrimitivePropertyArrayArg(new[]
                    {
                        new StructWithPrimitiveProperty { Property1 = 1 },
                        new StructWithPrimitiveProperty { Property1 = 2 }
                    });
                    break;

                case ValueTestType.StructWithReferencePropertyArrayArg:
                    instance.StructWithReferencePropertyArrayArg(new[]
                    {
                        new StructWithReferenceProperty { Property1 = "foo" },
                        new StructWithReferenceProperty { Property1 = "bar" }
                    });
                    break;

                case ValueTestType.ExternalStructArrayArg:
                    instance.ExternalStructArrayArg(new[] { new DateTime(2022, 8, 1, 3, 4, 5), new DateTime(2000, 8, 1, 3, 4, 5) });
                    break;

                #endregion
                #region Boxed Struct Array

                case ValueTestType.BoxedStructWithPrimitiveFieldArrayArg:
                    instance.BoxedStructWithPrimitiveFieldArrayArg(new object[]
                    {
                        new StructWithPrimitiveField { field1 = 1 },
                        new StructWithPrimitiveField { field1 = 2 }
                    });
                    break;

                case ValueTestType.BoxedStructWithReferenceFieldArrayArg:
                    instance.BoxedStructWithReferenceFieldArrayArg(new object[]
                    {
                        new StructWithReferenceField { field1 = "foo" },
                        new StructWithReferenceField { field1 = "bar" }
                    });
                    break;

                case ValueTestType.BoxedStructWithPrimitivePropertyArrayArg:
                    instance.BoxedStructWithPrimitivePropertyArrayArg(new object[]
                    {
                        new StructWithPrimitiveProperty { Property1 = 1 },
                        new StructWithPrimitiveProperty { Property1 = 2 }
                    });
                    break;

                case ValueTestType.BoxedStructWithReferencePropertyArrayArg:
                    instance.BoxedStructWithReferencePropertyArrayArg(new object[]
                    {
                        new StructWithReferenceProperty { Property1 = "foo" },
                        new StructWithReferenceProperty { Property1 = "bar" }
                    });
                    break;

                case ValueTestType.BoxedStructAndStringArrayArg:
                    instance.BoxedStructAndStringArrayArg(new object[] { new StructWithPrimitiveProperty { Property1 = 1 }, "b" });
                    break;

                case ValueTestType.BoxedExternalStructAndStringArrayArg:
                    instance.BoxedExternalStructAndStringArrayArg(new object[] { new DateTime(2022, 8, 1, 3, 4, 5), "b" });
                    break;

                #endregion
                #region Explicit Struct

                case ValueTestType.ExplicitStructArrayArg:
                    instance.ExplicitStructArrayArg(new[]
                    {
                        new StructWithExplicitBetweenStringAndInt("a", new StructWithExplicitFields
                        {
                            field1_0 = 10, //effective value: 30
                            field1_2 = 20, //effective value: 0
                            field2_0 = 30  //effective value: 30
                        }, 1),
                        new StructWithExplicitBetweenStringAndInt("b", new StructWithExplicitFields
                        {
                            field1_0 = 40, //effective value: 30
                            field1_2 = 50, //effective value: 0
                            field2_0 = 60  //effective value: 30
                        }, 2)
                    });
                    break;

                #endregion

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

                case ProfilerTestType.Async:
                    Task.Run(async () => await instance.Async()).Wait();
                    break;

                case ProfilerTestType.Thread_NameAfterCreate:
                {
                    var thread = new Thread(instance.Thread_NameAfterCreate);
                    thread.Start();
                    thread.Name = "NameAfterCreate";
                    break;
                }

                case ProfilerTestType.Thread_NameBeforeCreate:
                {
                    var thread = new Thread(instance.Thread_NameBeforeCreate);
                    thread.Name = "NameBeforeCreate";
                    thread.Start();
                    break;
                }

                case ProfilerTestType.Thread_NamedAndNeverStarted:
                {
                    var thread = new Thread(instance.SingleChild);
                    thread.Name = "NamedAndNeverStarted";
                    break;
                }

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

                case ExceptionTestType.UncaughtInNative_DoubleCallback:
                    instance.UncaughtInNative_DoubleCallback();
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

                case ExceptionTestType.UntracedThread:
                    instance.UntracedThread();
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(2);
                    break;
            }
        }

        private static void ProcessBlacklistTest(string subType)
        {
            var test = (BlacklistTestType)Enum.Parse(typeof(BlacklistTestType), subType);

            Trace.WriteLine($"Running test '{test}'");

            switch (test)
            {
                case BlacklistTestType.Simple:
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(2);
                    break;
            }
        }

        private static void ProcessStaticFieldTest(string subType, string[] additionalArgs)
        {
            var test = (StaticFieldTestType)Enum.Parse(typeof(StaticFieldTestType), subType);

            Trace.WriteLine($"Running test '{test}'");

            var instance = new StaticFieldType();

            switch (test)
            {
                case StaticFieldTestType.Normal:
                    instance.Normal();
                    break;

                case StaticFieldTestType.NotifyNormal:
                    if (additionalArgs.Length == 0)
                        throw new InvalidOperationException("Expected the global event name to signal to be specified.");

                    instance.NotifyNormal(additionalArgs[0]);
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(2);
                    break;
            }
        }
    }
}
