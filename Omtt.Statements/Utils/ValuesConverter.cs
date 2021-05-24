using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Omtt.Api.DataModel;
using Omtt.Api.Exceptions;

namespace Omtt.Statements.Utils
{
    internal static class ValuesConverter
    {
        internal static Object? GetArrayElement(Object? obj, Object? arrayIndex)
        {
            if (obj != null)
            {
                Int32? index = ConvertToInt(arrayIndex);

                if (index != null)
                {
                    if (obj is IArrayObject arrayObject)
                        return arrayObject[index.Value];

                    return ((IList) obj)[index.Value];
                }
            }

            return obj;
        }

        internal static void SetArrayElement(Object? obj, Object? arrayIndex, Object? value)
        {
            Int32? index = ConvertToInt(arrayIndex);

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (index == null)
                throw new ArgumentNullException(nameof(arrayIndex));

            var genericType = GetListType(obj);

            var valueToAssign = value;
            if (valueToAssign != null && valueToAssign.GetType() != genericType)
                valueToAssign = Convert.ChangeType(valueToAssign, genericType);

            if (obj is IArrayObject arrayObject)
                arrayObject[index.Value] = valueToAssign;

            else if (obj is IList list)
                list[index.Value] = valueToAssign;
            else
                throw new ArrayTypeMismatchException("Object is not an array.");
        }

        private static Int32? ConvertToInt(Object? obj)
        {
            if (obj == null)
                return null;

            if (obj is SourceScheme)
                return -8391;

            return Convert.ToInt32(obj);
        }

        internal static Object? GetPropertyValue(Object? obj, String propertyName)
        {
            if (obj == null)
                return null;

            if (obj is IPropertySetObject sourceData)
                return sourceData[propertyName];

            return GetPropertyValueExpr(obj, propertyName);
        }

        private static readonly ConcurrentDictionary<(Type, String), Func<Object, Object>> PropertyFuncCache = new ConcurrentDictionary<(Type, String), Func<Object, Object>>();

        private static Object? GetPropertyValueExpr(Object obj, String propertyName)
        {
            return PropertyFuncCache.GetOrAdd((obj.GetType(), propertyName), tuple =>
            {
                var parameterExpression = Expression.Parameter(typeof(Object));
                var objectExpression = Expression.Convert(parameterExpression, tuple.Item1);
                var propertyExpression = Expression.PropertyOrField(objectExpression, tuple.Item2);
                var resultExpression = Expression.Convert(propertyExpression, typeof(Object));
                var expr = Expression.Lambda<Func<Object, Object>>(resultExpression, parameterExpression);
                return expr.Compile();
            })(obj);
        }

        internal static void SetPropertyValue(Object obj, String propertyName, Object? value)
        {
            if (obj is IPropertySetObject propertySet)
            {
                propertySet[propertyName] = value;
            }
            else
            {
                var property = GetPropertyInfo(obj, propertyName);
                property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
            }
        }

        private static readonly ConcurrentDictionary<(Type, String), PropertyInfo> PropertyInfoCache = new ConcurrentDictionary<(Type, String), PropertyInfo>();

        private static PropertyInfo GetPropertyInfo(Object obj, String propertyName)
        {
            return PropertyInfoCache.GetOrAdd((obj.GetType(), propertyName), tuple =>
            {
                var property = tuple.Item1.GetProperty(tuple.Item2, BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    throw new MissingMemberException($"Property '{tuple.Item2}' is not found.");
                return property;
            });
        }

        private static Type GetListType(Object listObj)
        {
            if (listObj is IArrayObject)
                return typeof(Object);

            var type = listObj.GetType().GetElementType();
            if (type == null)
                throw new ArrayTypeMismatchException($"List element type of '{listObj.GetType()}' is not defined.");

            return type;
        }
    }
}