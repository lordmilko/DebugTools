using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgStaticField")]
    public class GetDbgStaticField : HostCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string TypeName { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string FieldName { get; set; }

        [Parameter(Mandatory = false)]
        public string AssemblyName { get; set; }

        protected override void ProcessRecordEx()
        {
            var assemblyNameRegex = GetWildcardRegex(AssemblyName);
            var typeNameRegex = GetWildcardRegex(TypeName);
            var fieldNameRegex = GetWildcardRegex(FieldName);

            var result = HostApp.GetStaticFields(Process.Id, assemblyNameRegex, typeNameRegex, fieldNameRegex);

            foreach (var warning in result.Warnings)
                WriteWarning($"Cannot get generic type static field '{warning}'");

            foreach (var field in result.Fields)
            {
                field.Process = Process;
                WriteObject(field);
            }
        }
    }
}
