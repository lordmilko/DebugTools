using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using ClrDebug;

namespace DebugTools.PowerShell.Cmdlets
{
    //Locates types in files that match the specified search criteria
    [Cmdlet(VerbsCommon.Get, "ClrType")]
    public class GetClrType : PSCmdlet
    {
        class Match
        {
            public string TypeName { get; }

            public string AssemblyName { get; }

            public Match(string typeName, string assemblyName)
            {
                TypeName = typeName;
                AssemblyName = assemblyName;
            }
        }

        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        [Parameter(Mandatory = false)]
        public string Name { get; set; }

        [Parameter(Mandatory = false)]
        public string Interface { get; set; }

        private WildcardPattern nameWildcard;
        private WildcardPattern ifaceWildcard;
        private MetaDataDispenser disp = new MetaDataDispenserEx();

        protected override void BeginProcessing()
        {
            if (Name != null)
                nameWildcard = new WildcardPattern(Name, WildcardOptions.IgnoreCase);

            if (Interface != null)
                ifaceWildcard = new WildcardPattern(Interface, WildcardOptions.IgnoreCase);
        }

        protected override void ProcessRecord()
        {
            if (Directory.Exists(Path))
                ProcessDirectory();
            else if (File.Exists(Path))
                ProcessFile(Path, true);
            else
                throw new InvalidOperationException($"Path '{Path}' is not a valid file or directory");
        }

        private void ProcessDirectory()
        {
            var files = Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories).Where(v =>
            {
                var ext = System.IO.Path.GetExtension(v);

                if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".dll") || StringComparer.OrdinalIgnoreCase.Equals(".exe"))
                    return true;

                return false;
            }).ToArray();

            foreach (var file in files)
                ProcessFile(file, false);
        }

        private void ProcessFile(string file, bool baseIsFile)
        {
            if (disp.TryOpenScope<MetaDataImport>(file, CorOpenFlags.ofRead, out var mdi) == HRESULT.S_OK)
            {
                var typeDefs = mdi.EnumTypeDefs();

                foreach (var typeDef in typeDefs)
                {
                    var typeName = mdi.GetTypeDefProps(typeDef).szTypeDef;

                    if (!TryMatchName(typeName))
                        continue;

                    if (!TryMatchInterface(mdi, typeDef))
                        continue;

                    WriteObject(new Match(typeName, baseIsFile ? System.IO.Path.GetFileName(file) : file.Replace(Path, string.Empty).TrimStart('\\')));
                }
            }
        }

        private bool TryMatchName(string typeName)
        {
            if (nameWildcard == null)
                return true;

            return nameWildcard.IsMatch(typeName);
        }

        private bool TryMatchInterface(MetaDataImport mdi, mdTypeDef typeDef)
        {
            if (ifaceWildcard == null)
                return true;

            var impls = mdi.EnumInterfaceImpls(typeDef);

            foreach (var impl in impls)
            {
                var ifaceProps = mdi.GetInterfaceImplProps(impl);

                string ifaceName;

                switch (ifaceProps.ptkIface.Type)
                {
                    case CorTokenType.mdtTypeDef:
                        ifaceName = mdi.GetTypeDefProps((mdTypeDef)ifaceProps.ptkIface).szTypeDef;
                        break;

                    case CorTokenType.mdtTypeRef:
                        ifaceName = mdi.GetTypeRefProps((mdTypeRef)ifaceProps.ptkIface).szName;
                        break;

                    //TypeSpec is not currently supported
                    case CorTokenType.mdtTypeSpec:
                        continue;

                    default:
                        throw new NotImplementedException($"Don't know how to handle token of type {ifaceProps.ptkIface.Type}");
                }

                if (Interface.Contains("."))
                    throw new NotImplementedException("Filtering by a namespace + interface is not currently supported. Please specify an interface name wildcard only.");

                var dot = ifaceName.LastIndexOf('.');

                var nameWithoutNamespace = ifaceName.Substring(dot + 1);

                if (ifaceWildcard.IsMatch(nameWithoutNamespace))
                    return true;
            }

            return false;
        }
    }
}
