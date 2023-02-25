using ClrDebug;

namespace DebugTools.Profiler
{
    public class ModuleInfo
    {
        public int UniqueModuleID { get; }

        public string Path { get; }

        private MetaDataImport import;

        public ModuleInfo(int uniqueModuleID, string path)
        {
            UniqueModuleID = uniqueModuleID;
            Path = path;
        }

        public MetaDataImport GetMDI()
        {
            if (import == null)
            {
                var dispenser = new MetaDataDispenserEx();

                import = dispenser.OpenScope<MetaDataImport>(Path, CorOpenFlags.ofReadOnly);
            }

            return import;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
