using System;

namespace DebugTools.Ui
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    class LPARAMAttribute : WMParamAttribute
    {
        public LPARAMAttribute(string name, Type type, bool isStructPointer = true) : base(name, type, isStructPointer)
        {
        }

        public LPARAMAttribute(string name, Type type, ParamPartKind kind, ParamPartMethodKind partMethodKind) : base(name, type, kind, partMethodKind)
        {
        }
    }
}
