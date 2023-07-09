using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace DebugTools.Dynamic
{
    class DynamicMetaObject<T> : DynamicMetaObject where T : IDynamicMetaObjectProvider
    {
        private new T Value => (T)base.Value;

        public Type ValueType => Value.GetType();

        internal DynamicMetaProxy<T> Proxy { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMetaObject{T}"/> class.
        /// </summary>
        /// <param name="expression">The expression representing this <see cref="DynamicMetaObject"/> during the dynamic binding process.</param>
        /// <param name="value">The dynamic value this object should represent. This value should be <see langword="this"/> in the object that has had <see cref="IDynamicMetaObjectProvider.GetMetaObject(Expression)"/> called.</param>
        /// <param name="proxy">The type of proxy this object should defer to for dynamically interacting with <paramref name="value"/>.</param>
        public DynamicMetaObject(Expression expression, T value, DynamicMetaProxy<T> proxy) : base(expression, BindingRestrictions.Empty, value)
        {
            Proxy = proxy;
        }

        #region Index

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, GetExpressions(indexes), e => binder.FallbackGetIndex(this, indexes, e));

            return builder.CallMethodWithResult(DynamicMetaProxy<T>.GetIndex);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, GetExpressions(indexes), e => binder.FallbackSetIndex(this, indexes, value, e));

            return builder.CallMethodReturnLast(DynamicMetaProxy<T>.SetIndex, value.Expression);
        }

        public override DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, GetExpressions(indexes), e => binder.FallbackDeleteIndex(this, indexes, e));

            return builder.CallMethodNoResult(DynamicMetaProxy<T>.DeleteIndex);
        }

        #endregion
        #region Member

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, DynamicExpressionBuilder<T>.NoArgs, e => binder.FallbackGetMember(this, e));

            return builder.CallMethodWithResult(DynamicMetaProxy<T>.GetMember);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, DynamicExpressionBuilder<T>.NoArgs, e => binder.FallbackSetMember(this, value, e));

            return builder.CallMethodReturnLast(DynamicMetaProxy<T>.SetMember, value.Expression);
        }

        public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, DynamicExpressionBuilder<T>.NoArgs, e => binder.FallbackDeleteMember(this, e));

            return builder.CallMethodNoResult(DynamicMetaProxy<T>.DeleteMember);
        }

        #endregion
        #region Binary/Unary

        public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, GetExpressions(arg), e => binder.FallbackBinaryOperation(this, e));

            return builder.CallMethodWithResult(DynamicMetaProxy<T>.BinaryOperation);
        }

        public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
        {
            var builder = new DynamicExpressionBuilder<T>(this, binder, DynamicExpressionBuilder<T>.NoArgs, e => binder.FallbackUnaryOperation(this, e));

            return builder.CallMethodWithResult(DynamicMetaProxy<T>.UnaryOperation);
        }

        #endregion

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            var builder = new DynamicExpressionBuilder<T>(
                this,
                binder,
                GetExpressions(args),
                e => binder.FallbackInvokeMember(this, args, e)
            );

            return builder.CallMethodWithResult(DynamicMetaProxy<T>.InvokeMember);
        }

        private static Expression[] GetExpressions(params DynamicMetaObject[] args) =>
            args.Select(a => a.Expression).ToArray();

        [ExcludeFromCodeCoverage]
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Proxy.GetDynamicMemberNames(Value);
        }
    }
}
