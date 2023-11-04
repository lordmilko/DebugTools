using System;
using System.Collections;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using DebugTools.Dynamic;
using Microsoft.CSharp.RuntimeBinder;

namespace DebugTools.PowerShell
{
    class PowerShellLocalProxyNotifier : ILocalProxyNotifier
    {
        public static readonly PowerShellLocalProxyNotifier Instance = new PowerShellLocalProxyNotifier();

        public void Notify(LocalProxyStub localProxyStub)
        {
            PSObjectDynamicContainer.EnsurePSObject(localProxyStub);
        }

        public IDisposable DisableEnumeration() => new DisableEnumerationScope();
    }

    class DisableEnumerationScope : IDisposable
    {
        public DisableEnumerationScope()
        {
            PSObjectDynamicContainer.EnumerationEnabled = false;
        }

        public void Dispose()
        {
            PSObjectDynamicContainer.EnumerationEnabled = true;
        }
    }

    class PSObjectDynamicContainer
    {
        private static ConditionalWeakTable<object, PSObject> cache = new ConditionalWeakTable<object, PSObject>();

        [ThreadStatic]
        internal static bool EnumerationEnabled = true;

        public static void EnsurePSObject(object obj)
        {
            if (obj is IDynamicMetaObjectProvider provider)
            {
                if (EnumerationEnabled && obj is IEnumerable e)
                {
                    foreach (var item in e)
                        EnsurePSObject(item);
                }

                if (!cache.TryGetValue(provider, out var existing))
                {
                    existing = new PSObject(provider);

                    cache.Add(provider, existing);

                    var metaObject = provider.GetMetaObject(Expression.Parameter(provider.GetType()));
                    var properties = metaObject.GetDynamicMemberNames().ToArray();

                    foreach (var property in properties)
                    {
                        var variable = new PSVariableEx(
                            property,
                            null,
                            () =>
                            {
                                try
                                {
                                    return GetProperty(provider, property);
                                }
                                catch
                                {
                                    return null;
                                }
                            },
                            v => SetProperty(provider, property, v)
                        );

                        existing.Properties.Add(new PSVariableProperty(variable));
                    }
                }
            }
        }

        private static object GetProperty(IDynamicMetaObjectProvider target, string name)
        {
            var site = CallSite<Func<CallSite, object, object>>.Create(
                Binder.GetMember(
                    CSharpBinderFlags.None,
                    name,
                    target.GetType(),
                    new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }
                )
            );

            return site.Target(site, target);
        }

        private static void SetProperty(IDynamicMetaObjectProvider target, string name, object value)
        {
            while (value is PSObject o)
                value = o.BaseObject;

            var site = CallSite<Func<CallSite, object, object, object>>.Create(
                Binder.SetMember(
                    0,
                    name,
                    target.GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                    }
                )
            );

            site.Target(site, target, value);
        }
    }
}
