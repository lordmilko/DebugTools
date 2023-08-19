using System;
using System.Globalization;
using System.Management.Automation.Host;

namespace Profiler.Tests
{
    class FakePSHost : PSHost
    {
        #region PSHost

        public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

        public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

        public override Guid InstanceId { get; } = Guid.NewGuid();

        public override string Name { get; } = nameof(FakePSHost);

        public override PSHostUserInterface UI { get; } = new FakePSHostUserInterface();

        public override Version Version { get; } = new Version(1, 0);

        public override void EnterNestedPrompt() => throw new NotImplementedException();

        public override void ExitNestedPrompt() => throw new NotImplementedException();

        public override void NotifyBeginApplication() => throw new NotImplementedException();

        public override void NotifyEndApplication() => throw new NotImplementedException();

        public override void SetShouldExit(int exitCode) => throw new NotImplementedException();

        #endregion
    }
}
