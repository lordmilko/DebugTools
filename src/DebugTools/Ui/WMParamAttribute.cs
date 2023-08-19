using System;

namespace DebugTools.Ui
{
    abstract class WMParamAttribute : Attribute
    {
        public string Name { get; }

        public Type Type { get; }

        public Either<bool, ParamPartKind> IsStructPointerOrPart { get; }

        public ParamPartMethodKind? PartMethodKind { get; }

        protected WMParamAttribute(string name, Type type, bool isStructPointer = true)
        {
            Name = name;
            Type = type;
            IsStructPointerOrPart = isStructPointer;
        }

        protected WMParamAttribute(string name, Type type, ParamPartKind kind, ParamPartMethodKind partMethodKind)
        {
            Name = name;
            Type = type;
            IsStructPointerOrPart = kind;
            PartMethodKind = partMethodKind;
        }
    }
}
