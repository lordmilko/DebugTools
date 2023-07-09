using System;
using System.Dynamic;

namespace DebugTools.Dynamic
{
    class FakeInvokeMemberBinder : InvokeMemberBinder
    {
        public FakeInvokeMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase, new CallInfo(0))
        {
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}
