using System;
using System.Reflection;

namespace Opencoding.Shared.Utils
{
	public static class TypeUtils
	{
		public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
		{
			while (toCheck != typeof(object))
			{
				if (toCheck == null)
					break;

				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (generic == cur)
				{
					return true;
				}
				toCheck = toCheck.BaseType;
			}
			return false;
		}
    
		public static PropertyInfo GetStaticPrivatePropertyInType(Type type, string name)
		{
			var propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Static);
			if (propertyInfo != null)
				return propertyInfo;

			if (type.BaseType != null)
				return GetStaticPrivatePropertyInType(type.BaseType, name);

			return null;
		}

		public static FieldInfo GetStaticPrivateFieldInType(Type type, string name)
		{
			var fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
			if (fieldInfo != null)
				return fieldInfo;

			if (type.BaseType != null)
				return GetStaticPrivateFieldInType(type.BaseType, name);

			return null;
		}
	}
}