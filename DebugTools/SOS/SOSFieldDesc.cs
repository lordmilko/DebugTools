using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
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
        public SOSMethodTable MethodTable { get; }

        public CorElementType Type => data.Type;
        public CorElementType sigType => data.sigType;
        public CLRDATA_ADDRESS MTOfType => data.MTOfType;
        public CLRDATA_ADDRESS ModuleOfType => data.ModuleOfType;
        public mdTypeDef TypeToken => data.TypeToken;
        public mdFieldDef mb => data.mb;
        public CLRDATA_ADDRESS MTOfEnclosingClass => data.MTOfEnclosingClass;
        public int dwOffset => data.dwOffset;
        public bool bIsThreadLocal => data.bIsThreadLocal;
        public bool bIsContextLocal => data.bIsContextLocal;
        public bool bIsStatic => data.bIsStatic;
        public CLRDATA_ADDRESS NextField => data.NextField;

        private readonly DacpFieldDescData data;

        public SOSFieldDesc(CLRDATA_ADDRESS address, SOSMethodTable methodTable, DacpFieldDescData data, SOSDacInterface sos)
        {
            Address = address;
            MethodTable = methodTable;
            this.data = data;

            var import = methodTable.Module.GetImport(sos);

            var field = import.GetFieldProps(mb);

            Name = field.szField;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
