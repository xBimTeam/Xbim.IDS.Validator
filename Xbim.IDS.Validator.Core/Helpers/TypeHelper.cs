using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.IDS.Validator.Core.Helpers
{

    public static class TypeHelper
    {
        /// <summary>
        /// Determine if a type is a value type.
        /// </summary>
        /// <param name="clrType">The type to test.</param>
        /// <returns>True if the type is a value type; false otherwise.</returns>
        public static bool IsValueType(Type clrType)
        {
            return clrType.IsValueType;
        }

        /// <summary>
        /// Determine if a type is a generic type.
        /// </summary>
        /// <param name="clrType">The type to test.</param>
        /// <returns>True if the type is a generic type; false otherwise.</returns>
        public static bool IsGenericType(this Type clrType)
        {
            return clrType.IsGenericType;
        }

        /// <summary>
        /// Determine if a type is an interface.
        /// </summary>
        /// <param name="clrType">The type to test.</param>
        /// <returns>True if the type is an interface; false otherwise.</returns>
        public static bool IsInterface(Type clrType)
        {
            return clrType.IsInterface;
        }

        public static bool IsNullable(Type clrType)
        {
            if (TypeHelper.IsValueType(clrType))
            {
                // value types are only nullable if they are Nullable<T>
                return TypeHelper.IsGenericType(clrType) && clrType.GetGenericTypeDefinition() == typeof(Nullable<>);
            }
            else
            {
                // reference types are always nullable
                return true;
            }
        }

        /// <summary>
        /// Convert the type to a nullable type.
        /// </summary>
        /// <param name="clrType">The type to convert.</param>
        /// <returns>The type from a nullable type.</returns>
        public static Type ToNullable(Type clrType)
        {
            if (TypeHelper.IsNullable(clrType))
            {
                return clrType;
            }
            else
            {
                return typeof(Nullable<>).MakeGenericType(clrType);
            }
        }

        /// <summary>
        /// Determine if a type is a collection.
        /// </summary>
        /// <param name="clrType">The type to test.</param>
        /// <returns>True if the type is an enumeration; false otherwise.</returns>
        public static bool IsCollection(Type clrType)
        {
            Type elementType;
            return TypeHelper.IsCollection(clrType, out elementType);
        }

        /// <summary>
        /// Determine if a type is a collection.
        /// </summary>
        /// <param name="clrType">The type to test.</param>
        /// <param name="elementType">out: the element type of the collection.</param>
        /// <returns>True if the type is an enumeration; false otherwise.</returns>
        public static bool IsCollection(Type clrType, out Type elementType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException("clrType");
            }

            elementType = clrType;

     

            Type collectionInterface
                = clrType.GetInterfaces()
                    .Union(new[] { clrType })
                    .FirstOrDefault(
                        t => TypeHelper.IsGenericType(t)
                             && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (collectionInterface != null)
            {
                elementType = collectionInterface.GetGenericArguments().Single();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns type of T if the type implements IEnumerable of T, otherwise, return null.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Type GetImplementedIEnumerableType(Type type)
        {
            // get inner type from Task<T>
            if (TypeHelper.IsGenericType(type) && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                type = type.GetGenericArguments().First();
            }

            if (TypeHelper.IsGenericType(type) && TypeHelper.IsInterface(type) &&
                (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 type.GetGenericTypeDefinition() == typeof(IQueryable<>)))
            {
                // special case the IEnumerable<T>
                return GetInnerGenericType(type);
            }
            else
            {
                // for the rest of interfaces and strongly Type collections
                Type[] interfaces = type.GetInterfaces();
                foreach (Type interfaceType in interfaces)
                {
                    if (TypeHelper.IsGenericType(interfaceType) &&
                        (interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                         interfaceType.GetGenericTypeDefinition() == typeof(IQueryable<>)))
                    {
                        // special case the IEnumerable<T>
                        return GetInnerGenericType(interfaceType);
                    }
                }
            }

            return null;
        }

        private static Type GetInnerGenericType(Type interfaceType)
        {
            // Getting the type T definition if the returning type implements IEnumerable<T>
            Type[] parameterTypes = interfaceType.GetGenericArguments();

            if (parameterTypes.Length == 1)
            {
                return parameterTypes[0];
            }

            return null;
        }

    }
}
