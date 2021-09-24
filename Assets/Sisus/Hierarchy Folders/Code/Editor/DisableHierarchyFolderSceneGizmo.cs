using UnityEngine;
using System;
using System.Reflection;
using UnityEditor.Callbacks;
using System.Collections;

namespace Sisus.HierarchyFolders
{
	public class DisableHierarchyFolderSceneGizmo
	{
		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			Type annotationUtilityType;
			if(!TryGetType("UnityEditor.AnnotationUtility, UnityEditor", out annotationUtilityType))
			{
				return;
			}
			MethodInfo getAnnotationsMethod;
			if(!TryGetStaticMethod(annotationUtilityType, "GetAnnotations", out getAnnotationsMethod))
			{
				return;
			}
			var annotations = getAnnotationsMethod.Invoke(null, null) as IEnumerable;
			if(annotations == null)
			{
				#if DEV_MODE
				Debug.Log("GetAnnotations return value as IEnumerable was null.");
				#endif
				return;
			}

			Type annotationType;
			if(!TryGetType("UnityEditor.Annotation, UnityEditor", out annotationType))
			{
				return;
			}
			FieldInfo scriptClassField;
			if(!TryGetField(annotationType, "scriptClass", out scriptClassField))
			{
				return;
			}

			foreach(var a in annotations)
			{
				string className = scriptClassField.GetValue(a) as string;
				if(!string.Equals(className, "HierarchyFolder"))
				{
					continue;
				}

				FieldInfo flagsField;
				if(!TryGetField(annotationType, "flags", out flagsField))
				{
					return;
				}
				int flags = (int)flagsField.GetValue(a);
				bool hasIcon = (flags & 1) == 1;
				if(!hasIcon)
				{
					return;
				}

				FieldInfo classIdField;
				if(!TryGetField(annotationType, "classID", out classIdField))
				{
					return;
				}
				int classId = (int)classIdField.GetValue(a);

				FieldInfo iconEnabledField;
				if(!TryGetField(annotationType, "iconEnabled", out iconEnabledField))
				{
					return;
				}
				int iconEnabled = (int)iconEnabledField.GetValue(a);
				if(iconEnabled == 0)
				{
					return;
				}

				#if DEV_MODE
				Debug.Log("Disabling HierarchyFolder scene gizmo...");
				#endif

				MethodInfo setIconEnabledMethod;
				if(!TryGetStaticMethod(annotationUtilityType, "SetIconEnabled", out setIconEnabledMethod))
				{
					return;
				}
				setIconEnabledMethod.Invoke(null, new object[] { classId, className, 0 });
				return;
			}
		}

		private static bool TryGetType(string typeName, out Type result)
		{
			result = Type.GetType(typeName, false);
			if(result == null)
			{
				#if DEV_MODE
				Debug.Log("Failed to find type " + typeName);
				#endif
				return false;
			}
			return true;
		}

		private static bool TryGetField(Type type, string fieldName, out FieldInfo result)
		{
			result = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(result == null)
			{
				#if DEV_MODE
				Debug.Log("Failed to find field " + type.Name + "." + fieldName);
				#endif
				return false;
			}
			return true;
		}
		private static bool TryGetStaticMethod(Type type, string methodName, out MethodInfo result)
		{
			result = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			if(result == null)
			{
				#if DEV_MODE
				Debug.Log("Failed to find method " + type.Name + "." + methodName);
				#endif
				return false;
			}
			return true;
		}
	}
}