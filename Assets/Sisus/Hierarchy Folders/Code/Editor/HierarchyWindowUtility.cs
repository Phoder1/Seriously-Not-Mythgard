using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.HierarchyFolders
{
	public static class HierarchyWindowUtility
	{
		private static Type sceneHierarchyWindowType;
		private static object treeViewState;
		private static PropertyInfo expandedIDsProperty;
		private static PropertyInfo lastInteractedHierarchyWindowProperty;

		private static EditorWindow hierarchyWindow;
		private static bool setupDone;
		

		[CanBeNull]
		public static EditorWindow GetHierarchyWindow()
		{
			if(hierarchyWindow == null)
			{
				if(!setupDone)
				{
					Setup();
				}

				if(lastInteractedHierarchyWindowProperty != null)
				{
					var hierarchyWindow = lastInteractedHierarchyWindowProperty.GetValue(null, null);
					if(hierarchyWindow != null)
					{
						return hierarchyWindow as EditorWindow;
					}
				}

				var hierarchyWindows = Resources.FindObjectsOfTypeAll(sceneHierarchyWindowType);
				if(hierarchyWindows.Length > 0)
				{
					return hierarchyWindows[0] as EditorWindow;
				}
			}
			else if(lastInteractedHierarchyWindowProperty != null)
			{
				var lastInteractedHierarchyWindow = lastInteractedHierarchyWindowProperty.GetValue(null, null) as EditorWindow;
				if(hierarchyWindow != lastInteractedHierarchyWindow && lastInteractedHierarchyWindow != null)
				{
					hierarchyWindow = lastInteractedHierarchyWindow;
				}
			}

			return hierarchyWindow;
		}
		
		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static void GetExpandedIDs([CanBeNull]ref List<int> expandedIDs)
		{
			if(!setupDone)
			{
				Setup();
			}

			if(expandedIDsProperty != null)
			{
				expandedIDs = (List<int>)expandedIDsProperty.GetValue(treeViewState, null);
			}
		}

		private static void Setup()
		{
			setupDone = true;

			var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
			sceneHierarchyWindowType = unityEditorAssembly.GetType("UnityEditor.SceneHierarchyWindow");

			if(lastInteractedHierarchyWindowProperty == null)
			{
				lastInteractedHierarchyWindowProperty = sceneHierarchyWindowType.GetProperty("lastInteractedHierarchyWindow", BindingFlags.Public | BindingFlags.Static);
			}

			var sceneHierarchyProperty = sceneHierarchyWindowType.GetProperty("sceneHierarchy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(sceneHierarchyProperty == null)
			{
				return;
			}

			hierarchyWindow = GetHierarchyWindow();
			if(hierarchyWindow != null)
			{
				SetupForHierarchyWindow();
			}
		}

		private static void SetupForHierarchyWindow()
		{
			var sceneHierarchyProperty = hierarchyWindow.GetType().GetProperty("sceneHierarchy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(sceneHierarchyProperty == null)
			{
				return;
			}

			var sceneHierarchy = sceneHierarchyProperty.GetValue(hierarchyWindow, null);


			var treeViewStateProperty = sceneHierarchy.GetType().GetProperty("treeViewState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(treeViewStateProperty == null)
			{
				return;
			}
			treeViewState = treeViewStateProperty.GetValue(sceneHierarchy, null);

			expandedIDsProperty = treeViewState.GetType().GetProperty("expandedIDs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}
}