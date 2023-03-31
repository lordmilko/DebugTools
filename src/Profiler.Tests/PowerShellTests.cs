using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Profiler.Tests.ValueFactory;

namespace Profiler.Tests
{
    [TestClass]
    public class PowerShellTests : BaseTest
    {
        #region Get-DbgProfilerStackFrame

        [TestMethod]
        public void PowerShell_GetFrames_NoArgs()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree);

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                null,
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_Include()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Take(1).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Include = "*first*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_IncludeAll()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree);

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Include = "*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_Unique()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Take(2).ToArray();

            //Still ReferenceEquals because Get-DbgProfilerStackFrame doesn't need to rewrite the tree
            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Unique = true },
                tree,
                expected,
                FrameEqualityComparer.Instance.Equals,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_IncludeUnique_FrameStandaloneAndParentOfAnother()
        {
            var type = typeof(List<>.Enumerator);

            var moveNext = type.GetMethod("MoveNext");
            var moveNextRare = type.GetMethod("MoveNextRare", BindingFlags.Instance | BindingFlags.NonPublic);

            var tree = MakeRoot(
                MakeFrame(moveNext, null),
                MakeFrame(moveNext, null,
                    MakeFrame(moveNextRare, null)
                )
            );

            var flattened = Flatten(tree);

            var expected = new[]
            {
                flattened.First(),
                flattened.Last()
            };

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Include = "*movenext*", Unique = true },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_CalledFrom()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Skip(1).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { CalledFrom = "*first*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_CalledFromAll()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { CalledFrom = "*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_SortThreads()
        {
            var tree1 = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true))
                ),
                1001
            );

            var tree2 = MakeRoot(
                MakeFrame("third", String("aaa"),
                    MakeFrame("fourth", Boolean(true))
                ),
                1002
            );

            var expected = Flatten(tree1).Union(Flatten(tree2)).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                null,
                new[] {tree2, tree1},
                expected,
                ReferenceEquals
            );
        }

        #endregion
        #region Get-SOSAppDomain

        [TestMethod]
        public void PowerShell_SOSAppDomain_All()
        {
            TestSOS<SOSAppDomain>(WellKnownCmdlet.GetSOSAppDomain, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSAppDomain_Address()
        {
            SOSAppDomain domain = null;

            TestSOS<SOSAppDomain>(WellKnownCmdlet.GetSOSAppDomain, null, v => domain = v[0]);

            TestSOS<SOSAppDomain>(
                WellKnownCmdlet.GetSOSAppDomain,
                new { Address = domain.AppDomainPtr },
                v => Assert.AreEqual(domain.AppDomainPtr, v.Single().AppDomainPtr)
            );
        }

        [TestMethod]
        public void PowerShell_SOSAppDomain_Type()
        {
            TestSOS<SOSAppDomain>(WellKnownCmdlet.GetSOSAppDomain, new { Type = AppDomainType.Normal }, v =>
            {
                Assert.IsTrue(v.Length > 0);

                Assert.IsTrue(v.All(a => a.Type == AppDomainType.Normal));
            });
        }

        #endregion
        #region Get-SOSAssembly

        [TestMethod]
        public void PowerShell_SOSAssembly_All()
        {
            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSAssembly_Address()
        {
            SOSAssembly assembly = null;

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, v => assembly = v[0]);

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, assembly.AssemblyPtr, v => Assert.AreEqual(assembly.AssemblyPtr, v.Single().AssemblyPtr));
        }

        [TestMethod]
        public void PowerShell_SOSAssembly_Name()
        {
            SOSAssembly assembly = null;

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, v => assembly = v[0]);

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, assembly.Name, v => Assert.IsTrue(v.All(a => assembly.AssemblyPtr == a.AssemblyPtr)));
        }

        [TestMethod]
        public void PowerShell_SOSAssembly_FromAppDomain()
        {
            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        #endregion
        #region Get-SOSModule

        [TestMethod]
        public void PowerShell_SOSModule_All()
        {
            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSModule_Address()
        {
            SOSModule module = null;

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, v => module = v[0]);

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, module.Address, v => Assert.AreEqual(module.Address, v.Single().Address));
        }

        [TestMethod]
        public void PowerShell_SOSModule_Name()
        {
            SOSModule module = null;

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, v => module = v[0]);

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, module.FileName, v => Assert.IsTrue(v.All(m => m.FileName == module.FileName)));
        }

        [TestMethod]
        public void PowerShell_SOSModule_FromAppDomain()
        {
            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSModule_FromAssembly()
        {
            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        #endregion
        #region Get-SOSMethodTable

        [TestMethod]
        public void PowerShell_SOSMethodTable_All()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_Address()
        {
            SOSMethodTable methodTable = null;

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, v => methodTable = v[0]);

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, methodTable.Address, v => Assert.AreEqual(methodTable.Address, v.Single().Address));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_Name()
        {
            SOSMethodTable methodTable = null;

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, v => methodTable = v.Last());

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, methodTable.Name, v => Assert.IsTrue(v.All(m => methodTable.Name == m.Name)));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_FromAppDomain()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_FromAssembly()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_FromModule()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, WellKnownCmdlet.GetSOSModule, v => Assert.IsTrue(v.Length > 0));
        }

        #endregion
        #region Get-SOSFieldDesc

        [TestMethod]
        public void PowerShell_SOSFieldDesc_All()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_Address()
        {
            SOSFieldDesc field = null;

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, v => field = v[0]);

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, field.Address, v => Assert.AreEqual(field.Address, v.Single().Address));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_Name()
        {
            SOSFieldDesc field = null;

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, v => field = v[0]);

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, field.Name, v => Assert.IsTrue(v.All(f => field.Address == f.Address)));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromAppDomain()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromAssembly()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromModule()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSModule, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromMethodTable()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSMethodTable, v => Assert.IsTrue(v.Length > 0));
        }

        #endregion
        #region Get-SOSMethodDesc

        [TestMethod]
        public void PowerShell_SOSMethodDesc_All()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_Address()
        {
            SOSMethodDesc method = null;

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, v => method = v[0]);

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, method.MethodDescPtr, v => Assert.AreEqual(method.Name, v.Single().Name));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_Name()
        {
            SOSMethodDesc method = null;

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, v => method = v.Last());

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, method.Name, v => Assert.IsTrue(v.All(m => m.Name == method.Name)));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromAppDomain()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromAssembly()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromModule()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSModule, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromMethodTable()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSMethodTable, v => Assert.IsTrue(v.Length > 0));
        }

        #endregion

        private void TestProfiler<T>(string cmdlet, object param, RootFrame root, T[] expected, params Func<T, T, bool>[] validate) =>
            TestProfiler(cmdlet, param, new[] { root }, expected, validate);

        private void TestProfiler<T>(string cmdlet, object param, RootFrame[] root, T[] expected, params Func<T, T, bool>[] validate)
        {
            var actual = InvokeProfiler<T>(cmdlet, param, root);

            if (actual.Length != expected.Length)
                Assert.Fail($"Expected frames: {expected.Length}. Actual: {actual.Length}");

            for (var i = 0; i < actual.Length; i++)
            {
                for (var j = 0; j < validate.Length; j++)
                {
                    Assert.IsTrue(validate[j](expected[i], actual[i]), $"'{expected[i]}' did not equal '{actual[i]}' in the correct way (Frame {i}, Validation {j})");
                }
            }
        }

        private void TestSOS<T>(string cmdlet, object param, Action<T[]> validate)
        {
            var result = InvokeSOS<T>(cmdlet, param);

            validate(result);
        }

        private void TestSOS<T>(string cmdlet, object param, string inputCmdlet, Action<T[]> validate)
        {
            var result = InvokeSOS<T>(cmdlet, param, inputCmdlet);

            validate(result);
        }

        private T[] InvokeProfiler<T>(string cmdlet, object param, params RootFrame[] roots)
        {
            var session = new MockProfilerSession(roots);

            var invoker = new PowerShellInvoker();

            var results = invoker.Invoke<T>(cmdlet, param, new { Session = session }, null);

            return results;
        }

        private T[] InvokeSOS<T>(string cmdlet, object param, string inputCmdlet = null)
        {
            var sosProcess = AcquireSOSProcess();

            var invoker = new PowerShellInvoker();

            var results = invoker.Invoke<T>(cmdlet, param, new { Process = sosProcess }, inputCmdlet);

            return results;
        }

        private static object objLock = new object();
        private static LocalSOSProcess cachedSOSProcess;

        private LocalSOSProcess AcquireSOSProcess()
        {
            lock (objLock)
            {
                if (cachedSOSProcess == null)
                {
                    //CreateSOSProcess will store the process in a dictionary for lookup later on
                    var process = Process.GetCurrentProcess();
                    var hostApp = DebugToolsSessionState.GetDetectedHost(process);
                    var handle = hostApp.CreateSOSProcess(process.Id, true);

                    var sosProcess = new LocalSOSProcess(handle);

                    cachedSOSProcess = sosProcess;
                }

                return cachedSOSProcess;
            }
        }
    }
}
