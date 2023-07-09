using System;
using System.Dynamic;

namespace DebugTools.Dynamic
{
    class FakeSetMemberBinder : SetMemberBinder
    {
        public FakeSetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase)
        {
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value,
            DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}