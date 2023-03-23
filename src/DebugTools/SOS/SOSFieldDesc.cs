using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSFieldDesc
    {
        public static SOSFieldDesc[] GetFieldDescs(SOSMethodTable methodTable, SOSDacInterface sos)
        {
            var list = new List<SOSFieldDesc>();

            var mtFieldData = sos.GetMethodTableFieldData(methodTable.Address);

            var nextField = mtFieldData.FirstField;

            var numFields = mtFieldData.wNumInstanceFields + mtFieldData.wNumStaticFields;

            for (var i = 0; i < numFields; i++)
            {
                if (sos.TryGetFieldDescData(nextField, out var data) != HRESULT.S_OK)
                    return list.ToArray();

                list.Add(new SOSFieldDesc(nextField, methodTable, data, sos));

                nextField = data.NextField;
            }

            return list.ToArray();
        }

        public static SOSFieldDesc GetFieldDesc(CLRDATA_ADDRESS address, SOSDacInterface sos)
        {
            if (sos.TryGetFieldDescData(address, out var data) != HRESULT.S_OK)
                return null;

            var methodTable = SOSMethodTable.GetMethodTable(data.MTOfEnclosingClass, sos);

            return new SOSFieldDesc(address, methodTable, data, sos);
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

        public SOSFieldDesc(CLRDATA_ADDRESS address, SOSMethodTable methodTable, DacpFieldDescData data, SOSDacInterface sos)
        {
            Address = address;
            ParentMethodTable = methodTable;

            var import = methodTable.Module.GetImport(sos);

            var field = import.GetFieldProps(data.mb);

            Name = field.szField;

            Type = data.Type;
            sigType = data.sigType;
            MTOfType = data.MTOfType;
            ModuleOfType = data.ModuleOfType;
            TypeToken = data.TypeToken;
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
