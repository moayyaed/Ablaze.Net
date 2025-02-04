#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System.Collections.Generic;

namespace System.Reflection {
	internal static class ReflectionUtils {
		private static readonly Type GenericDictionaryType = typeof(IDictionary<,>);

		public static bool IsInstantiatableType(this Type t) {
			return !(t.IsAbstract || t.IsInterface || t.IsArray) && HasDefaultConstructor(t);
		}

		public static bool HasDefaultConstructor(this Type t) {
			return t.GetConstructor(BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
		}

		public static bool IsAssignable(this Type to, Type from) {
			if (to.IsAssignableFrom(from))
				return true;
			else if (to.IsGenericType && from.IsGenericTypeDefinition)
				return to.IsAssignableFrom(from.MakeGenericType(to.GetGenericArguments()));
			else
				return false;
		}

		public static bool IsSubClass(this Type type, Type check) {
			if (!(type == null || check == null))
				return false;
			if (type == check)
				return true;

			if (check.IsInterface) {
				foreach (Type t in type.GetInterfaces()) {
					if (IsSubClass(t, check))
						return true;
				}
			}
			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				if (IsSubClass(type.GetGenericTypeDefinition(), check))
					return true;
			}
			return IsSubClass(type.BaseType, check);
		}

		/// <summary>
		/// Gets the type of the typed list's items.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The type of the typed list's items.</returns>
		public static Type GetTypedListItemType(this Type type) {
			if (type.IsArray)
				return type.GetElementType();
			else if (type.IsGenericType && typeof(List<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
				return type.GetGenericArguments()[0];
			else
				return null;
		}

		public static Type GetTypedDictionaryValueType(this Type type) {
			Type genDictType = GetGenericDictionary(type);

			if (genDictType != null)
				return genDictType.GetGenericArguments()[1];
			else
				return null;
		}

		public static Type GetGenericDictionary(this Type type) {
			if (type.IsGenericType && GenericDictionaryType.IsAssignableFrom(type.GetGenericTypeDefinition()))
				return type;
			Type[] ifaces = type.GetInterfaces();
			if (ifaces != null)
				for (int i = 0; i < ifaces.Length; i++) {
					Type current = GetGenericDictionary(ifaces[i]);
					if (current != null)
						return current;
				}

			return null;
		}

		public static Type GetMemberUnderlyingType(this MemberInfo member) {
			switch (member.MemberType) {
				case MemberTypes.Field:
					return ((FieldInfo) member).FieldType;
				case MemberTypes.Property:
					return ((PropertyInfo) member).PropertyType;
				case MemberTypes.Event:
					return ((EventInfo) member).EventHandlerType;
				default:
					return null;
			}
		}

		/// <summary>
		/// Determines whether the member is an indexed property.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <returns>
		/// 	<c>true</c> if the member is an indexed property; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsIndexedProperty(this MemberInfo member) {
			PropertyInfo propertyInfo = member as PropertyInfo;
			return propertyInfo == null ? false : IsIndexedProperty(propertyInfo);
		}

		/// <summary>
		/// Determines whether the property is an indexed property.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		/// 	<c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsIndexedProperty(this PropertyInfo property) {
			return property != null && (property.GetIndexParameters().Length > 0);
		}

		/// <summary>
		/// Gets the member's value on the object.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="target">The target object.</param>
		/// <returns>The member's value on the object.</returns>
		public static object GetMemberValue(this MemberInfo member, object target) {
			if (member.MemberType == MemberTypes.Field)
				return ((FieldInfo) member).GetValue(target);
			else {
				try {
					return ((PropertyInfo) member).GetValue(target, null);
				} catch (TargetParameterCountException e) {
					throw new ArgumentException("MemberInfo has index parameters", nameof(member), e);
				}
			}
		}

		/// <summary>
		/// Sets the member's value on the target object.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="target">The target.</param>
		/// <param name="value">The value.</param>
		public static void SetMemberValue(this MemberInfo member, object target, object value) {
			switch (member.MemberType) {
				case MemberTypes.Field:
					((FieldInfo) member).SetValue(target, value);
					break;
				case MemberTypes.Property:
					((PropertyInfo) member).SetValue(target, value, null);
					break;
			}
		}

		/// <summary>
		/// Determines whether the specified MemberInfo can be read.
		/// </summary>
		/// <param name="member">The MemberInfo to determine whether can be read.</param>
		/// <returns>
		/// 	<c>true</c> if the specified MemberInfo can be read; otherwise, <c>false</c>.
		/// </returns>
		public static bool CanReadMemberValue(this MemberInfo member) {
			switch (member.MemberType) {
				case MemberTypes.Field:
					return true;
				case MemberTypes.Property:
					return ((PropertyInfo) member).CanRead;
				default:
					return false;
			}
		}

		/// <summary>
		/// Determines whether the specified MemberInfo can be set.
		/// </summary>
		/// <param name="member">The MemberInfo to determine whether can be set.</param>
		/// <returns>
		/// 	<c>true</c> if the specified MemberInfo can be set; otherwise, <c>false</c>.
		/// </returns>
		public static bool CanSetMemberValue(this MemberInfo member) {
			switch (member.MemberType) {
				case MemberTypes.Field:
					return true;
				case MemberTypes.Property:
					return ((PropertyInfo) member).CanWrite;
				default:
					return false;
			}
		}

		public static IEnumerable<MemberInfo> GetFieldsAndProperties(this Type type, BindingFlags bindingAttr) {
			MemberInfo[] members = type.GetFields(bindingAttr);
			for (int i = 0; i < members.Length; i++)
				yield return members[i];
			members = type.GetProperties(bindingAttr);
			for (int i = 0; i < members.Length; i++)
				yield return members[i];
		}
	}
}