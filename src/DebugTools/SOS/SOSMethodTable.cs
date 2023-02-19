using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSMethodTable
    {
        public static SOSMethodTable[] GetMethodTables(SOSModule module, SOSDacInterface sos)
        {
            var list = new List<SOSMethodTable>();

            Exception outerEx = null;

            //Any exceptions that occur in TraverseModuleMap will be caught, so we must save them and rethrowthem afterwards
            var hr = sos.TryTraverseModuleMap(ModuleMapType.TypeDefToMethodTable, module.Address, (index, mt, token) =>
            {
                //Clear GC mark bits
                mt &= ~3;

                try
                {
                    if (sos.TryGetMethodTableName(mt, out var name) == HRESULT.S_OK &&
                        sos.TryGetMethodTableData(mt, out var data) == HRESULT.S_OK)
                    {
                        list.Add(new SOSMethodTable(module, mt, name, data));
                    }
                }
                catch (Exception ex)
                {
                    if (sos.TryGetMethodTableName(mt, out var name) == HRESULT.S_OK)
                        outerEx = new SOSException($"An error occurred while attempting to read method table {mt} ({name})", ex);
                    else
                        outerEx = new SOSException($"An error occurred while attempting to read method table {mt}", ex);

                    throw;
                }
            }, IntPtr.Zero);

            if (outerEx != null)
                throw outerEx;

            hr.ThrowOnNotOK();

            return list.ToArray();
        }

        public static SOSMethodTable GetMethodTable(CLRDATA_ADDRESS methodTable, SOSDacInterface sos)
        {
            if (sos.TryGetMethodTableName(methodTable, out var name) == HRESULT.S_OK &&
                sos.TryGetMethodTableData(methodTable, out var data) == HRESULT.S_OK)
            {
                var module = SOSModule.GetModule(data.Module, sos);

                return new SOSMethodTable(module, methodTable, name, data);
            }

            return null;
        }

        public string Name { get; }
        public SOSModule Module { get; }
        public CLRDATA_ADDRESS Address { get; }

        public bool bIsFree { get; }
        public CLRDATA_ADDRESS Class { get; }
        public CLRDATA_ADDRESS ParentMethodTable { get; }
        public ushort wNumInterfaces { get; }
        public ushort wNumMethods { get; }
        public ushort wNumVtableSlots { get; }
        public ushort wNumVirtuals { get; }
        public int BaseSize { get; }
        public int ComponentSize { get; }
        public mdTypeDef cl { get; }
        public CorTypeAttr dwAttrClass { get; }
        public bool bIsShared { get; }
        public bool bIsDynamic { get; }
        public bool bContainsPointers { get; }

        private SOSMethodTable(
            SOSModule module,
            CLRDATA_ADDRESS address,
            string name,
            DacpMethodTableData data)
        {
            Module = module;
            Address = address;
            Name = name;

            bIsFree = data.bIsFree;
            Class = data.Class;
            ParentMethodTable = data.ParentMethodTable;
            wNumInterfaces = data.wNumInterfaces;
            wNumMethods = data.wNumMethods;
            wNumVtableSlots = data.wNumVtableSlots;
            wNumVirtuals = data.wNumVirtuals;
            BaseSize = data.BaseSize;
            ComponentSize = data.ComponentSize;
            cl = data.cl;
            dwAttrClass = data.dwAttrClass;
            bIsShared = data.bIsShared;
            bIsDynamic = data.bIsDynamic;
            bContainsPointers = data.bContainsPointers;
    }

        public override string ToString()
        {
            return Name;
        }
    }
}
