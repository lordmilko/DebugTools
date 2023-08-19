using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;

namespace Profiler.Tests
{
    public class FakePSHostUserInterface : PSHostUserInterface
    {
        public StringBuilder Buffer { get; } = new StringBuilder();

        #region PSHostUserInterface

        public override PSHostRawUserInterface RawUI { get; } = new FakePSHostRawUserInterface();

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions) => null;

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice) => 0;

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) =>
            throw new NotImplementedException();

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName) =>
            throw new NotImplementedException();

        public override string ReadLine() =>
            throw new NotImplementedException();

        public override SecureString ReadLineAsSecureString() =>
            throw new NotImplementedException();

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) =>
            throw new NotImplementedException();

        public override void Write(string value) =>
            throw new NotImplementedException();

        public override void WriteDebugLine(string message) =>
            throw new NotImplementedException();

        public override void WriteErrorLine(string value) =>
            throw new NotImplementedException();

        public override void WriteLine(string value)
        {
            Buffer.AppendLine(value);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record) =>
            throw new NotImplementedException();

        public override void WriteVerboseLine(string message) =>
            throw new NotImplementedException();

        public override void WriteWarningLine(string message) =>
            throw new NotImplementedException();

        #endregion
    }
}
