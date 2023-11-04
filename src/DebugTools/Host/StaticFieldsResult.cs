using System;

namespace DebugTools.Host
{
    [Serializable]
    public struct StaticFieldsResult
    {
        public StaticFieldInfo[] Fields;
        public string[] Warnings;

        public StaticFieldsResult(StaticFieldInfo[] fields, string[] warnings)
        {
            Fields = fields;
            Warnings = warnings;
        }
    }
}
