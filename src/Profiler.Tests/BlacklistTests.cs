using System;
using System.Linq;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class BlacklistTests : BaseTest
    {
        [TestMethod]
        public void Blacklist_WithDefaultBlacklist()
        {
            //Sets the baseline of modules we expect to see with a blacklist (and thus not see in some of the tests below)

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules("DebugTools.TestHost.exe");
            });
        }

        [TestMethod]
        public void Blacklist_IgnoreDefaultBlacklist()
        {
            //Sets the baseline of modules we expect to see without a blacklist (and thus not see in some of the tests below)

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll",
                    "System.Configuration.dll",
                    "System.dll",
                    "System.Xml.dll"
                );
            }, ProfilerSetting.IgnoreDefaultBlacklist);
        }

        [TestMethod]
        public void Blacklist_Contains()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.Contains, "System"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_ModuleName()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "System.dll"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll",
                    "System.Configuration.dll",
                    "System.Xml.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_Contains_IgnoreCase()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.Contains, "system"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_ModuleName_IgnoreCase()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "system.dll"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll",
                    "System.Configuration.dll",
                    "System.Xml.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_WithWhitelistContains()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "System.dll"}
                    }
                ),
                ProfilerSetting.ModuleWhitelist(
                    new MatchCollection
                    {
                        {MatchKind.Contains, "WINDOWS"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll",
                    "System.Configuration.dll",
                    "System.dll",
                    "System.Xml.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_WithWhitelistModuleName()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "System.dll"}
                    }
                ),
                ProfilerSetting.ModuleWhitelist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "System.dll"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "mscorlib.dll",
                    "System.Configuration.dll",
                    "System.dll",
                    "System.Xml.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_WhitelistDefaultBlacklist()
        {
            var settings = new[]
            {
                ProfilerSetting.ModuleWhitelist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "System.dll"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules(
                    "DebugTools.TestHost.exe",
                    "System.dll"
                );
            }, settings);
        }

        [TestMethod]
        public void Blacklist_ExplicitAll()
        {
            var settings = new[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist,
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.All, string.Empty}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules();
            }, settings);
        }

        [TestMethod]
        public void Blacklist_EffectiveAll()
        {
            var settings = new[]
            {
                ProfilerSetting.ModuleBlacklist(
                    new MatchCollection
                    {
                        {MatchKind.ModuleName, "DebugTools.TestHost.exe"}
                    }
                )
            };

            Test(BlacklistTestType.Simple, v =>
            {
                v.HasModules();
            }, settings);
        }

        private void Test(BlacklistTestType type, Action<BlacklistVerifier> validate, params ProfilerSetting[] settings)
        {
            TestInternal(TestType.Blacklist, type.ToString(), v =>
            {
                var modules = v.Methods.Where(m => !(m is UnknownMethodInfo)).Select(m => m.ModuleName).Distinct().ToArray();

                validate(modules.Verify());
            }, settings);
        }
    }
}
