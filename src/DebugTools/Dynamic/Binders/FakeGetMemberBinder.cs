using System;
using System.Dynamic;

namespace DebugTools.Dynamic
{
    class FakeGetMemberBinder : GetMemberBinder
    {
        public FakeGetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}
