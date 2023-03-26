using System;
using System.Collections.Generic;
using ClrDebug;
using static ClrDebug.HRESULT;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSFieldDesc
    {
        public static SOSFieldDesc[] GetFieldDescs(SOSMethodTable methodTable, SOSDacInterface sos)
        {
            int numInstanceFieldsSeen = 0;
            var mtFieldData = sos.GetMethodTableFieldData(methodTable.Address);
            var results = GetFieldDescsInternal(methodTable, sos, ref numInstanceFieldsSeen, mtFieldData);

            return results ?? new SOSFieldDesc[0];
        }

        private static SOSFieldDesc[] GetFieldDescsInternal(
            SOSMethodTable methodTable,
            SOSDacInterface sos,
            ref int numInstanceFieldsSeen,
            DacpMethodTableFieldData mtFieldData)
        {
            var results = new List<SOSFieldDesc>();

            if (methodTable.ParentMethodTable != 0)
            {
                var parentMethodTable = SOSMethodTable.GetMethodTable(methodTable.ParentMethodTable, sos);

                if (sos.TryGetMethodTableFieldData(methodTable.ParentMethodTable, out var parentMTFieldData) != S_OK)
                    return null;

                if (parentMethodTable != null)
                {
                    var parentFields = GetFieldDescsInternal(parentMethodTable, sos, ref numInstanceFieldsSeen,
                        parentMTFieldData);

                    if (parentFields == null)
                        return null;

                    foreach (var field in parentFields)
                        results.Add(field);
                }
                else
                    return null;
            }

            int numStaticFieldsSeen = 0;

            var fieldAddress = mtFieldData.FirstField;

            var xclrDataModule = sos.GetModule(methodTable.Module.Address);
            var import = xclrDataModule.As<MetaDataImport>();

            //DacpMethodTableFieldData counts how many static fields exist under the specific MethodTable we asked for, and all instance
            //fields under the MethodTable and all of its parent types. As such, we must use a global counter when counting instance fields,
            //and start probing fields from the top-most type downwards
            while (numInstanceFieldsSeen < mtFieldData.wNumInstanceFields || numStaticFieldsSeen < mtFieldData.wNumStaticFields)
            {
                var fieldData = sos.GetFieldDescData(fieldAddress);

                if (fieldData.bIsStatic)
                {
                    numStaticFieldsSeen++;

                    results.Add(new SOSFieldDesc(fieldAddress, methodTable, fieldData, sos, import));
                }
                else
                {
                    numInstanceFieldsSeen++;

                    results.Add(new SOSFieldDesc(fieldAddress, methodTable, fieldData, sos, import));
                }

                fieldAddress = fieldData.NextField;
            }

            return results.ToArray();
        }

        public static SOSFieldDesc GetFieldDesc(CLRDATA_ADDRESS address, SOSDacInterface sos)
        {
            if (sos.TryGetFieldDescData(address, out var data) != HRESULT.S_OK)
                return null;

            var methodTable = SOSMethodTable.GetMethodTable(data.MTOfEnclosingClass, sos);
            var xclrDataModule = sos.GetModule(methodTable.Module.Address);
            var import = xclrDataModule.As<MetaDataImport>();

            return new SOSFieldDesc(address, methodTable, data, sos, import);
        }

        public string Name { get; }
        public CLRDATA_ADDRESS Address { get; }
        public SOSMethodTable ParentMethodTable { get; }

        public CorElementType Type { get; }
        public CorElementType sigType { get; }
        public CLRDATA_ADDRESS MTOfType { get; }
        public CLRDATA_ADDRESS ModuleOfType { get; }
        public mdTypeDef TypeToken { get; }
        public mdFieldDef mb { get; }
        public CLRDATA_ADDRESS MTOfEnclosingClass { get; }
        public int dwOffset { get; }
        public bool bIsThreadLocal { get; }
        public bool bIsContextLocal { get; }
        public bool bIsStatic { get; }
        public CLRDATA_ADDRESS NextField { get; }

        public SOSFieldDesc(CLRDATA_ADDRESS address, SOSMethodTable methodTable, DacpFieldDescData data, SOSDacInterface sos, MetaDataImport import)
        {
            Address = address;
            ParentMethodTable = methodTable;

            var field = import.GetFieldProps(data.mb);

            Name = field.szField;

            Type = data.Type;
            sigType = data.sigType;
            MTOfType = data.MTOfType;
            ModuleOfType = data.ModuleOfType;
            TypeToken = data.TokenOfType;
            mb = data.mb;
            MTOfEnclosingClass = data.MTOfEnclosingClass;
            dwOffset = data.dwOffset;
            bIsThreadLocal = data.bIsThreadLocal;
            bIsContextLocal = data.bIsContextLocal;
            bIsStatic = data.bIsStatic;
            NextField = data.NextField;
    }

        public override string ToString()
        {
            return Name;
        }
    }
}
