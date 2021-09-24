//#define DEBUG_CUSTOM_EDITORS
//#define DEBUG_PROPERTY_DRAWERS
//#define DEBUG_SET_EDITING_TEXT_FIELD

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

#if DEV_MODE && DEBUG_CUSTOM_EDITORS
using System.Linq;
#endif

namespace Sisus.HierarchyFolders
{
	public static class CustomEditorUtility
	{
		private static Dictionary<Type, Type> customEditorsByType;
		private static Dictionary<Type, Type> propertyDrawersByType;
		private static Dictionary<Type, Type> decoratorDrawersByType;

		public static Dictionary<Type, Type> CustomEditorsByType
		{
			get
			{
				if(customEditorsByType == null)
				{
					customEditorsByType = new Dictionary<Type, Type>();

					var customEditorType = typeof(CustomEditor);
					var inspectedTypeField = customEditorType.GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance);
					var useForChildrenField = customEditorType.GetField("m_EditorForChildClasses", BindingFlags.NonPublic | BindingFlags.Instance);

					var editorType = typeof(Editor);
					var list = new List<Type>(100);

					var assemblies = AppDomain.CurrentDomain.GetAssemblies();
					for(int a = assemblies.Length - 1; a >= 0; a--)
					{
						var assembly = assemblies[a];
						var types = assembly.GetTypes();
						for(int t = types.Length - 1; t >= 0; t--)
						{
							var type = types[t];
							if(editorType.IsAssignableFrom(type))
							{
								list.Add(type);
							}
						}
					}

					GetDrawersByInspectedTypeFromAttributes<CustomEditor>(list, inspectedTypeField, ref customEditorsByType);
					
					//second pass: also apply for inheriting types if they don't already have more specific overrides
					GetDrawersByInheritedInspectedTypesFromAttributes<CustomEditor>(list, inspectedTypeField, useForChildrenField, ref customEditorsByType);
					
					#if DEV_MODE && DEBUG_CUSTOM_EDITORS
					var log = customEditorsByType.Where(pair => !Types.Component.IsAssignableFrom(pair.Key));
					Debug.Log("Non-Components with custom editors:\n"+StringUtils.ToString(log, "\n"));
					#endif
				}

				return customEditorsByType;
			}
		}
		
		public static bool TryGetCustomEditorType(Type targetType, out Type editorType)
		{
			return CustomEditorsByType.TryGetValue(targetType, out editorType);
		}

		/// <summary>
		/// Given an array of PropertyDrawer, DecoratorDrawers or Editors, gets their inspected types and adds them to drawersByInspectedType.
		/// </summary>
		/// <typeparam name="TAttribute"> Type of the attribute. </typeparam>
		/// <param name="drawerOrEditorTypes"> List of PropertyDrawer, DecoratorDrawer or Editor types. </param>
		/// <param name="targetTypeField"> FieldInfo for getting the inspected type. </param>
		/// <param name="drawersByInspectedType">
		/// [in,out] dictionary where drawer types will be added with their inspected types as the keys. </param>
		private static void GetDrawersByInspectedTypeFromAttributes<TAttribute>([NotNull]IList<Type> drawerOrEditorTypes, [NotNull]FieldInfo targetTypeField, [NotNull]ref Dictionary<Type,Type> drawersByInspectedType) where TAttribute : Attribute
		{
			var attType = typeof(TAttribute);
			
			for(int n = drawerOrEditorTypes.Count - 1; n >= 0; n--)
			{
				var drawerType = drawerOrEditorTypes[n];
				if(!drawerType.IsAbstract)
				{
					var attributes = drawerType.GetCustomAttributes(attType, true);
					for(int a = attributes.Length - 1; a >= 0; a--)
					{
						var attribute = attributes[a];
						var inspectedType = targetTypeField.GetValue(attribute) as Type;
						if(!inspectedType.IsAbstract)
						{
							drawersByInspectedType[inspectedType] = drawerType;
						}
					}
				}
			}
		}

		private static void GetDrawersByInheritedInspectedTypesFromAttributes<TAttribute>(IList<Type> drawerOrEditorTypes, FieldInfo targetTypeField, [CanBeNull]FieldInfo useForChildrenField, ref Dictionary<Type,Type> addEditorsByType) where TAttribute : Attribute
		{
			var attType = typeof(TAttribute);

			for(int n = drawerOrEditorTypes.Count- 1; n >= 0; n--)
			{
				var drawerType = drawerOrEditorTypes[n];

				if(!drawerType.IsAbstract)
				{
					var attributes = drawerType.GetCustomAttributes(attType, true);
					for(int a = attributes.Length - 1; a >= 0; a--)
					{
						var attribute = attributes[a];
						bool useForChildren = useForChildrenField == null ? true : (bool)useForChildrenField.GetValue(attribute);
						if(useForChildren)
						{
							var targetType = targetTypeField.GetValue(attribute) as Type;
							if(!targetType.IsClass)
							{
								//value types don't support inheritance
								continue;
							}

							var assemblies = AppDomain.CurrentDomain.GetAssemblies();
							for(int ass = assemblies.Length - 1; ass >= 0; ass--)
							{
								var assembly = assemblies[ass];
								var types = assembly.GetTypes();
								for(int t = types.Length - 1; t >= 0; t--)
								{
									var type = types[t];
									if(targetType.IsAssignableFrom(type))
									{
										if(!type.IsAbstract)
										{
											if(!addEditorsByType.ContainsKey(type))
											{
												addEditorsByType.Add(type, drawerType);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
#endif