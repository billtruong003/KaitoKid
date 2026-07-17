using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stratton.Core
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// return Attribute.IsDefined(m, typeof(T));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(this MemberInfo m) where T : Attribute
        {
#if UNITY_WSA && !UNITY_EDITOR && !ENABLE_IL2CPP
            return  m.CustomAttributes.Any(o => o.AttributeType.Equals(typeof (T)));
#else
            return Attribute.IsDefined(m, typeof(T));
#endif
        }

        /// <summary>
        /// return Attribute.IsDefined(m, typeof(T));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(this Type m) where T : Attribute
        {
#if UNITY_WSA && !UNITY_EDITOR && !ENABLE_IL2CPP
            return m.GetTypeInfo().CustomAttributes.Any(o => o.AttributeType == typeof(T));
#else
            return Attribute.IsDefined(m, typeof(T));
#endif
        }


#if UNITY_WSA && !UNITY_EDITOR && !ENABLE_IL2CPP
        /// <summary>
        /// return Attribute.IsDefined(m, typeof(T));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool HasAttribute<T>(this TypeInfo m) where T : Attribute
        {
            return m.CustomAttributes.Any(o => o.AttributeType == typeof(T));
        }

#endif

        /// <summary>
        ///  return m.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this MemberInfo m) where T : Attribute
        {
            return m.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
        }

        /// <summary>
        ///  return m.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this Type m) where T : Attribute
        {
#if UNITY_WSA && !UNITY_EDITOR && !ENABLE_IL2CPP
            return m.GetTypeInfo().GetCustomAttribute<T>();
#else
            return (T)Attribute.GetCustomAttribute(m, typeof(T));
#endif
        }

        /// <summary>
        /// Set the member's instances value
        /// </summary>
        /// <returns></returns>
        public static void SetMemberValue(this MemberInfo member, object instance, object value)
        {
            var method = member as MethodInfo;
            var property = member as PropertyInfo;
            var field = member as FieldInfo;
            if (method != null)
            {
                if (method.GetParameters().Any())
                {
                    method.Invoke(instance, new[] { value });
                }
                else
                {
                    method.Invoke(instance, null);
                }
            }
            else if (property != null)
            {
                property.SetValue(instance, value, null);
            }
            else if (field != null)
            {
                field.SetValue(instance, value);
            }
        }

        /// <summary>
        /// Gets value from non public property.
        /// </summary>
        /// <param name="type">Type of class, where property is declared.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="returnValueType">Type of expected value.</param>
        /// <param name="instance">Object instance to get value from. Set to NULL if property is static.</param>
        /// <returns>Property value.</returns>
        public static object GetPrivatePropertyValue(this Type type, string propertyName, Type returnValueType,
                                              object instance)
        {
            BindingFlags bindingFlags = BindingFlags.NonPublic |
                (instance != null ? BindingFlags.Instance : BindingFlags.Static);

            PropertyInfo property =
                type.GetProperty(propertyName, bindingFlags, null, returnValueType, new Type[] { }, null);

            if (property == null)
            {
                Log.Error(BaseLogChannel.Core,
                    $"Property not found! Name: {propertyName}, return value type: {returnValueType}, searched in class: {type}.");
                return null;
            }

            MethodInfo method = property.GetGetMethod(true);
            object value = method.Invoke(instance, null);
            return value;
        }

        /// <summary>
        /// Gets value from non public field.
        /// </summary>
        /// <param name="type">Type of class, where field is declared.</param>
        /// <param name="fieldName">Field name.</param>
        /// <param name="instance">Object instance to get value from. Set to NULL if field is static.</param>
        /// <returns>Property value.</returns>
        public static object GetPrivateFieldValue(this Type type, string fieldName, object instance)
        {
            BindingFlags bindingFlags = BindingFlags.NonPublic |
                (instance != null ? BindingFlags.Instance : BindingFlags.Static);

            FieldInfo field = type.GetField(fieldName, bindingFlags);

            if (field == null)
            {
                Log.Error(BaseLogChannel.Core,
                    $"Field not found! Name: {fieldName}, searched in class: {type}.");
                return null;
            }
           
            object retValue = field.GetValue(instance);
            return retValue;
        }
    }
}