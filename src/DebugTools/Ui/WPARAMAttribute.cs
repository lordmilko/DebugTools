using System;

namespace DebugTools.Ui
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    class WPARAMAttribute : WMParamAttribute
    {
        public WPARAMAttribute(string name, Type type, bool isStructPointer = true) : base(name, type, isStructPointer)
        {
        }

        public WPARAMAttribute(string name, Type type, ParamPartKind kind, ParamPartMethodKind partMethodKind) : base(name, type, kind, partMethodKind)
        {
        }
    }
}
