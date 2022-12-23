using System;
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

                default:
                    Debug.WriteLine($"Don't know how to run test type '{testType}'");
                    Environment.Exit(1);
                    break;
            }

            Console.WriteLine("Done");
        }

        private static void ProcessValueTest(string subType)
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

                case ValueTestType.DecimalArg:
                    instance.DecimalArg(1m);
                    break;

                case ValueTestType.StringArg:
                    instance.StringArg("foo");
                    break;

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

                case ValueTestType.ObjectArrayArg:
                    instance.ObjectArrayArg(new[] { new object() });
                    break;

                case ValueTestType.EmptyObjectArrayArg:
                    instance.EmptyObjectArrayArg(new object[0]);
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
    }
}
