using System;
using System.Diagnostics;
using System.Threading;
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
                Console.WriteLine("No test was specified");
                Environment.Exit(1);
            }

            var test = (ProfilerTestType) Enum.Parse(typeof(ProfilerTestType), args[0]);

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
                    Debug.WriteLine($"Don't know how to run test '{test}'");
                    Environment.Exit(1);
                    break;
            }
        }
    }
}
