using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
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

        public bool bIsFree => data.bIsFree;
        public CLRDATA_ADDRESS Class => data.Class;
        public CLRDATA_ADDRESS ParentMethodTable => data.ParentMethodTable;
        public ushort wNumInterfaces => data.wNumInterfaces;
        public ushort wNumMethods => data.wNumMethods;
        public ushort wNumVtableSlots => data.wNumVtableSlots;
        public ushort wNumVirtuals => data.wNumVirtuals;
        public int BaseSize => data.BaseSize;
        public int ComponentSize => data.ComponentSize;
        public mdTypeDef cl => data.cl;
        public CorTypeAttr dwAttrClass => data.dwAttrClass;
        public bool bIsShared => data.bIsShared;
        public bool bIsDynamic => data.bIsDynamic;
        public bool bContainsPointers => data.bContainsPointers;

        private readonly DacpMethodTableData data;

        private SOSMethodTable(
            SOSModule module,
            CLRDATA_ADDRESS address,
            string name,
            DacpMethodTableData data)
        {
            Module = module;
            Address = address;
            Name = name;
            this.data = data;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
