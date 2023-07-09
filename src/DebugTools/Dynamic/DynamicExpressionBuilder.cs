using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DebugTools.Dynamic
{
    delegate DynamicMetaObject Fallback(DynamicMetaObject errorSuggestion);

    //This is based on mono
    //https://github.com/mono/mono/blob/main/mcs/class/referencesource/System.Core/Microsoft/Scripting/Actions/DynamicObject.cs
    class DynamicExpressionBuilder<T> where T : IDynamicMetaObjectProvider
    {
        public static readonly Expression[] NoArgs = new Expression[0]; //Used in reference comparison, requires unique object identity

        public DynamicMetaObject<T> MetaObject { get; set; }

        public DynamicMetaObjectBinder Binder { get; set; }

        public Expression[] Args { get; set; }

        public Fallback Fallback { get; set; }

        public DynamicExpressionBuilder(DynamicMetaObject<T> metaObject, DynamicMetaObjectBinder binder, Expression[] args,
            Fallback fallback)
        {
            MetaObject = metaObject;
            Binder = binder;
            Args = args;
            Fallback = fallback;
        }

        public DynamicMetaObject CallMethodWithResult(string methodName)
        {
            //Step 1: have the CLR try and bind the member. This will either be an expression that binds the member, or throws an exception that the member doesn't exist

            /* If the member exists:
             *     Convert($arg0).foo
             * If the member doesn't exist:
             *     throw new RuntimeBinderException("Foo doesn't exist") */
            var fallbackResult = GetFallbackResult();

            //Step 2: Try and bind the member ourselves. If the member is not one the special members our dynamic implementation supports,
            //fall back to the CLR's original binding attempt - which will either access the member successfully or throw an exception

            var callDynamic = BuildCallMethodWithResult(methodName, fallbackResult);

            //Rather than do Fallback(callDynamic) to decide whether we even need to defer to our DynamicProxy to bind the member,
            //in this implementation we ALWAYS defer to our proxy
            return callDynamic;
        }

        internal DynamicMetaObject BuildCallMethodWithResult(string methodName, DynamicMetaObject fallbackResult)
        {
            /* Build a new expression like:
                   object result;
                   TryGetMember(payload, out result) ? fallbackInvoke(result) : fallbackResult */
            var testResult = Expression.Parameter(typeof(object), null);
            var callArgs = Expression.Parameter(typeof(object[]), null);
            var callArgsValue = GetConvertedArgs(Args);

            var methodResult = GetConvertFailedExpression(testResult);

            var callDynamic = new DynamicMetaObject(
                Expression.Block(
                    new[] { testResult, callArgs },
                    Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                    GetTernaryExpression(
                        methodName,
                        callArgs,
                        testResult,
                        methodResult.Expression,
                        fallbackResult.Expression,
                        Binder.ReturnType
                    )
                ),
                GetRestrictions().Merge(methodResult.Restrictions).Merge(fallbackResult.Restrictions)
            );

            return callDynamic;
        }

        public DynamicMetaObject CallMethodReturnLast(string methodName, Expression value)
        {
            /* Build an expression like
                   object result;
                   TrySetMember(payload, result = value) ? result : fallbackResult */

            DynamicMetaObject fallbackResult = GetFallbackResult();                                          //What the binder will do if we don't bind
            
            var trueResult = Expression.Parameter(typeof(object), null);                                //Create an anonymous local variable for storing the result when true
            var testResult = Expression.Assign(trueResult, Expression.Convert(value, typeof (object)));
            var callArgs = Expression.Parameter(typeof(object[]), null);                                //Will eventually store the array of arguments passed to our proxy's Try* Member methods
            var callArgsValue = GetConvertedArgs(Args);

            var callDynamic = new DynamicMetaObject(
                Expression.Block(
                    new[] { trueResult, callArgs },
                    Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                    GetTernaryExpression(
                        methodName,
                        callArgs,
                        testResult,
                        trueResult,
                        fallbackResult.Expression,
                        typeof(object)
                    )
                ),
                GetRestrictions().Merge(fallbackResult.Restrictions)
            );

            //Rather than do Fallback(callDynamic) to decide whether we even need to defer to our DynamicProxy to bind the member,
            //in this implementation we ALWAYS defer to our proxy
            return callDynamic;
        }

        public DynamicMetaObject CallMethodNoResult(string methodName)
        {
            DynamicMetaObject fallbackResult = GetFallbackResult();                                          //What the binder will do if we don't bind

            var callArgs = Expression.Parameter(typeof(object[]), null);                                //Will eventually store the array of arguments passed to our proxy's Try* Member methods
            var callArgsValue = GetConvertedArgs(Args);

            var callDynamic = new DynamicMetaObject(
                Expression.Block(
                    new[] { callArgs },
                    Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                    GetTernaryExpression(
                        methodName,
                        callArgs,
                        null,
                        Expression.Empty(),
                        fallbackResult.Expression,
                        typeof(void)
                    )
                ),
                GetRestrictions().Merge(fallbackResult.Restrictions)
            );

            //Rather than do Fallback(callDynamic) to decide whether we even need to defer to our DynamicProxy to bind the member,
            //in this implementation we ALWAYS defer to our proxy
            return callDynamic;
        }

        private DynamicMetaObject GetFallbackResult()
        {
            if (MetaObject.Proxy is IFallbackResultProvider p)
                return p.GetFallbackResult(Binder, GetLimitedSelf(), Args, GetRestrictions());

            return Fallback(null);
        }

        #region Ternary Expressions

        private Expression GetTernaryExpression(string methodName,
            ParameterExpression callArgs, Expression testResult, Expression trueResult, Expression fallback, Type returnType)
        {
            //Construct an If/Else statement that attempts to perform our custom operation. If it fails,
            //we invoke the default behavior of the binder that would have run had we never been here

            var expr = Expression.Condition(
                GetTernaryTest(methodName, callArgs, testResult), //Test
                GetTernaryTrue(callArgs, trueResult),             //True
                fallback,                                         //False
                returnType                                        //Return Type
            );

            return expr;
        }

        private Expression GetTernaryTest(string methodName, ParameterExpression callArgs, Expression result)
        {
            var source = MetaObject.Proxy;
            var methodInfo = source.GetType().GetMethod(methodName);

            if (methodInfo == null)
                throw new ArgumentException($"Could not find method '{methodName}' on {source.GetType().Name}.");

            var args = BuildCallArgs(callArgs, result);

            var method = Expression.Call(
                Expression.Constant(MetaObject.Proxy), //Instance
                methodInfo,                            //Method
                args                                   //Arguments
            );

            return method;
        }

        private Expression GetTernaryTrue(ParameterExpression callArgs, Expression result)
        {
            var expr = Expression.Block(
                ReferenceArgAssign(callArgs, Args),
                result
            );

            return expr;
        }

        #endregion

        [ExcludeFromCodeCoverage]
        private DynamicMetaObject GetConvertFailedExpression(ParameterExpression result)
        {
            var resultMO = new DynamicMetaObject(result, BindingRestrictions.Empty);

            if (Binder.ReturnType != typeof(object))
                throw new NotSupportedException($"Binder {Binder.GetType().Name} is not currently supported.");

            return resultMO;
        }

        #region Expression Helpers

        private Expression GetLimitedSelf()
        {
            // Convert to DynamicObject rather than LimitType, because
            // the limit type might be non-public.
            if (AreEquivalent(MetaObject.Expression.Type, MetaObject.ValueType))
            {
                return MetaObject.Expression;
            }
            return Expression.Convert(MetaObject.Expression, MetaObject.ValueType);
        }

        public static bool AreEquivalent(Type t1, Type t2)
        {
            return t1 == t2 || t1.IsEquivalentTo(t2);
        }

        private static Expression[] GetConvertedArgs(params Expression[] args)
        {
            var paramArgs = new Expression[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                paramArgs[i] = Expression.Convert(args[i], typeof(object));
            }

            return paramArgs;
        }

        private Expression[] BuildCallArgs(Expression arg0, Expression arg1)
        {
            var list = new List<Expression>
            {
                GetLimitedSelf(),
                Constant(Binder)
            };

            if (!ReferenceEquals(Args, NoArgs))
                list.Add(arg0);

            if (arg1 != null)
                list.Add(arg1);

            return list.ToArray();
        }

        private static ConstantExpression Constant(DynamicMetaObjectBinder binder)
        {
            Type t = binder.GetType();

            while (!t.IsVisible)
                t = t.BaseType;

            return Expression.Constant(binder, t);
        }

        [ExcludeFromCodeCoverage]
        private static Expression ReferenceArgAssign(Expression callArgs, Expression[] args)
        {
            ReadOnlyCollectionBuilder<Expression> block = null;

            for (int i = 0; i < args.Length; i++)
            {
                ParameterExpression variable = args[i] as ParameterExpression;
                Requires(variable != null);

                if (variable.IsByRef)
                {
                    if (block == null)
                        block = new ReadOnlyCollectionBuilder<Expression>();

                    block.Add(
                        Expression.Assign(
                            variable,
                            Expression.Convert(
                                Expression.ArrayIndex(
                                    callArgs,
                                    Expression.Constant(i)
                                ),
                                variable.Type
                            )
                        )
                    );
                }
            }

            if (block != null)
                return Expression.Block(block);

            return Expression.Empty();
        }

        [ExcludeFromCodeCoverage]
        private static void Requires(bool precondition)
        {
            if (!precondition)
                throw new ArgumentException("Method precondition violated.");
        }

        private BindingRestrictions GetRestrictions()
        {
            Debug.Assert(MetaObject.Restrictions == BindingRestrictions.Empty, "We don't merge, restrictions are always empty.");

            return GetTypeRestriction(MetaObject);
        }

        private static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj)
        {
            if (obj.Value == null && obj.HasValue)
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, null);

            return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
        }

        #endregion
    }
}
