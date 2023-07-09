using System.Dynamic;
using System.Linq.Expressions;

namespace DebugTools.Dynamic
{
    partial class ReflectionProxy : IDynamicMetaObjectProvider
    {
        private object Value { get; }

        public ReflectionProxy(object value)
        {
            Value = value;
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicMetaObject<ReflectionProxy>(parameter, this, new ReflectionMetaProxy());
        }
    }
}
