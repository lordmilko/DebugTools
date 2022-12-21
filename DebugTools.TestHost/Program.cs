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
                case TestType.SigBlob:
                    ProcessSigBlobTest(args[1]);
                    break;

                case TestType.Profiler:
                    ProcessProfilerTest(args[1]);
                    break;

                default:
                    Debug.WriteLine($"Don't know how to run test type '{testType}'");
                    Environment.Exit(1);
                    break;
            }
        }

        private static void ProcessSigBlobTest(string subType)
        {
            var test = (SigBlobTestType)Enum.Parse(typeof(SigBlobTestType), subType);

            Debug.WriteLine($"Running test '{test}'");

            var instance = new SigBlobType();

            switch (test)
            {
                case SigBlobTestType.NoArgs_ReturnVoid:
                    instance.NoArgs_ReturnVoid();
                    break;

                case SigBlobTestType.OneArg_ReturnVoid:
                    instance.OneArg_ReturnVoid(1);
                    break;

                #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

                case SigBlobTestType.BoolArg:
                    instance.BoolArg(true);
                    break;

                case SigBlobTestType.CharArg:
                    instance.CharArg('b');
                    break;

                case SigBlobTestType.ByteArg:
                    instance.ByteArg(1);
                    break;

                case SigBlobTestType.SByteArg:
                    instance.SByteArg(1);
                    break;

                case SigBlobTestType.Int16Arg:
                    instance.Int16Arg(1);
                    break;

                case SigBlobTestType.UInt16Arg:
                    instance.UInt16Arg(1);
                    break;

                case SigBlobTestType.Int32Arg:
                    instance.Int32Arg(1);
                    break;

                case SigBlobTestType.UInt32Arg:
                    instance.UInt32Arg(1);
                    break;

                case SigBlobTestType.Int64Arg:
                    instance.Int64Arg(1);
                    break;

                case SigBlobTestType.UInt64Arg:
                    instance.UInt64Arg(1);
                    break;

                case SigBlobTestType.FloatArg:
                    instance.FloatArg(1.1f);
                    break;

                case SigBlobTestType.DoubleArg:
                    instance.DoubleArg(1.1);
                    break;

                case SigBlobTestType.IntPtrArg:
                    instance.IntPtrArg(new IntPtr(1));
                    break;

                case SigBlobTestType.UIntPtrArg:
                    instance.UIntPtrArg(new UIntPtr(1));
                    break;

                #endregion

                default:
                    Debug.WriteLine($"Don't know how to run profiler test '{test}'");
                    Environment.Exit(1);
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
                    Environment.Exit(1);
                    break;
            }
        }
    }
}
