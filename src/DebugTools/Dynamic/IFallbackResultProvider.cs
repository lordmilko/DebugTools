using System.Dynamic;
using System.Linq.Expressions;

namespace DebugTools.Dynamic
{
    interface IFallbackResultProvider
    {
        DynamicMetaObject GetFallbackResult(DynamicMetaObjectBinder binder, Expression instance, Expression[] args, BindingRestrictions restrictions);
    }
}
