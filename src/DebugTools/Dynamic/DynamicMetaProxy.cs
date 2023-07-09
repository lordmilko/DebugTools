using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;

namespace DebugTools.Dynamic
{
    //Note: its very important that types derived from DynamicMetaProxy do NOT store any state in them. Once the information about a given
    //dynamic call site has been retrieved, it is cached in a compiler generated static class. The only way to pass "state" to our proxy is
    //by passing the instance object to it that the dynamic operation is being invoked upon

    [ExcludeFromCodeCoverage]
    internal abstract class DynamicMetaProxy<T> where T : IDynamicMetaObjectProvider
    {
        internal static string Convert => nameof(TryConvert);
        
        internal static string GetIndex => nameof(TryGetIndex);
        internal static string SetIndex => nameof(TrySetIndex);
        internal static string DeleteIndex => nameof(TryDeleteIndex);

        internal static string GetMember => nameof(TryGetMember);
        internal static string SetMember => nameof(TrySetMember);
        internal static string DeleteMember => nameof(TryDeleteMember);

        internal static string InvokeMember => nameof(TryInvokeMember);

        internal static string BinaryOperation => nameof(TryBinaryOperation);
        internal static string UnaryOperation => nameof(TryUnaryOperation);

        internal object lockObject = new object();

        public virtual bool TryConvert(T instance, ConvertBinder binder, out object value)
        {
            lock (lockObject)
            {
                value = null;

                return false;
            }
        }

        #region Index

        public virtual bool TryGetIndex(T instance, GetIndexBinder binder, object[] indexes, out object value)
        {
            lock (lockObject)
            {
                value = null;

                return false;
            }
        }

        public virtual bool TrySetIndex(T instance, SetIndexBinder binder, object[] indexes, object value)
        {
            lock (lockObject)
            {
                return false;
            }
        }

        public virtual bool TryDeleteIndex(T instance, DeleteIndexBinder binder, object[] indexes)
        {
            lock (lockObject)
            {
                return false;
            }
        }

        #endregion
        #region Member

        /// <summary>
        /// Retrieves the value of a property from a dynamic object.
        /// </summary>
        /// <param name="instance">The dynamic object to retrieve the property from.</param>
        /// <param name="binder">The binder that specifies the property to access.</param>
        /// <param name="value">Returns the value of the property set by this method.</param>
        /// <returns>True if the member was successfully retrieved. Otherwise, false.</returns>
        public virtual bool TryGetMember(T instance, GetMemberBinder binder, out object value)
        {
            lock (lockObject)
            {
                value = null;

                return false;
            }
        }

        /// <summary>
        /// Sets the value of a property on a dynamic object.
        /// </summary>
        /// <param name="instance">The dynamic object to set a property on.</param>
        /// <param name="binder">The binder that specifies the property to modify.</param>
        /// <param name="value">The value to set the property to.</param>
        /// <returns>True if the member was successfully set. Otherwise, false.</returns>
        public virtual bool TrySetMember(T instance, SetMemberBinder binder, object value)
        {
            return false;
        }

        public virtual bool TryDeleteMember(T instance, DeleteMemberBinder binder)
        {
            return false;
        }

        public virtual bool TryInvokeMember(T instance, InvokeMemberBinder binder, object[] args, out object value)
        {
            value = null;
            return false;
        }

        #endregion
        #region Binary/Unary

        public virtual bool TryBinaryOperation(BinaryOperationBinder binder, out object value)
        {
            lock (lockObject)
            {
                value = null;

                return false;
            }
        }

        public virtual bool TryUnaryOperation(UnaryOperationBinder binder, out object value)
        {
            lock (lockObject)
            {
                value = null;

                return false;
            }
        }

        #endregion

        /// <summary>
        /// Retrieves a list of all dynamic properties defined on a dynamic object.
        /// </summary>
        /// <param name="instance">The dynamic object to list the dynamic properties of.</param>
        /// <returns>A list of all dynamic properties defined on the specified object.</returns>
        public virtual IEnumerable<string> GetDynamicMemberNames(T instance)
        {
            return Enumerable.Empty<string>();
        }
    }
}
