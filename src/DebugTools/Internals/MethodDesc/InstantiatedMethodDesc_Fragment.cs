using System;

namespace DebugTools
{
    /// <summary>
    /// Represents the members of an InstantiatedMethodDesc that exist after the fields in the MethodDesc base class.
    /// </summary>
    [Serializable]
    internal struct InstantiatedMethodDesc_Fragment
    {
        internal readonly IntPtr m_pPerInstInfo;
        internal readonly short m_wFlags2;
        internal readonly short m_wNumGenericArgs;
    }
}
