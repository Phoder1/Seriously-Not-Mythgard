// PreferencesApplier can make changes to this region based on preferences
#region ApplyPreferences
#define HIERARCHY_FOLDER_EXTENSIONS_IN_GLOBAL_NAMESPACE
#endregion

using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if HIERARCHY_FOLDER_EXTENSIONS_IN_GLOBAL_NAMESPACE
using Sisus.HierarchyFolders;
#endif

#if !HIERARCHY_FOLDER_EXTENSIONS_IN_GLOBAL_NAMESPACE
namespace Sisus.HierarchyFolders
{
#endif
/// <summary>
/// Can add convenience methods related to Hierarchy Folders to GameObjects.
/// </summary>
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class HierarchyFolderExtensions
{
	private static readonly Stack<Transform> ReusableParentsStack = new Stack<Transform>(10);
	private static readonly List<Transform> ReusableTransformList = new List<Transform>(20);
	private static readonly List<GameObject> ReusableGameObjectList = new List<GameObject>(20);

	/// <summary>
	/// Determines whether or not the given <see cref="GameObject"/> is a Hierarchy Folder or not.
	/// </summary>
	/// <param name="gameObject"> <see cref="GameObject"/> to check. </param>
	/// <returns> <see langword="true"/> if <paramref name="gameObject"/> is a Hierarchy Folder; otherwise, <see langword="false"/>. </returns>
	public static bool IsHierarchyFolder([NotNull]this GameObject gameObject)
	{
		#if UNITY_2019_2_OR_NEWER
		HierarchyFolder hierarchyFolder;
		return gameObject.TryGetComponent(out hierarchyFolder);
		#else
		return gameObject.GetComponent<HierarchyFolder>() != null;
		#endif
	}

	/// <summary>
	/// Gets next Transform up the parent chain that is not a Hierarchy Folder.
	/// 
	/// In builds Hierarchy Folders will not be skipped, because we assume that all of them have been stripped
	/// from the build, or if not, that there is no reason to skip them.
	/// </summary>
	/// <param name="transform"> Transform whose parent chain to check. </param>
	/// <returns> First parent that doesn't contain the HierarchyFolder component, or null if none found. </returns>
	[CanBeNull]
	public static Transform GetParent([NotNull]this Transform transform)
	{
		#if UNITY_EDITOR
		return transform.GetParent(true);
		#else
		return transform.parent;
		#endif
	}

	/// <summary>
	/// Gets next Transform up the parent chain that is not a Hierarchy Folder.
	/// </summary>
	/// <param name="transform"> GameObject whose parent chain to check. </param>
	/// <param name="skipHierarchyFolders"> If true then Hierarchy Folders will be skipped when finding the result. </param>
	/// <returns> First parent that doesn't contain the HierarchyFolder component, or null if none found. </returns>
	[CanBeNull]
	public static Transform GetParent([NotNull]this Transform transform, bool skipHierarchyFolders)
	{
		var parent = transform.parent;
		if(skipHierarchyFolders && parent != null && parent.gameObject.IsHierarchyFolder())
		{
			return parent.GetParent(true);
		}
		return parent;
	}

	/// <summary>
	/// Gets next GameObject up the parent chain that is not a Hierarchy Folder.
	/// 
	/// In builds Hierarchy Folders will not be skipped, because we assume that all of them have been stripped
	/// from the build, or if not, that there is no reason to skip them.
	/// </summary>
	/// <param name="gameObject"> GameObject whose parent chain to check. </param>
	/// <returns> First parent that doesn't contain the HierarchyFolder component, or null if none found. </returns>
	[CanBeNull]
	public static GameObject GetParent([NotNull]this GameObject gameObject)
	{
		#if UNITY_EDITOR
		return gameObject.GetParent(true);
		#else
		var parentTransform = gameObject.transform.parent;
		return parentTransform != null ? parentTransform.gameObject : null;
		#endif
	}

	/// <summary>
	/// Gets next GameObject up the parent chain, with option to skip Hierarchy Folders.
	/// </summary>
	/// <param name="gameObject"> GameObject whose parent chain to check. </param>
	/// <param name="skipHierarchyFolders"> If true then Hierarchy Folders will be skipped when finding the result. </param>
	/// <returns> First parent that doesn't contain the HierarchyFolder component, or null if none found. </returns>
	[CanBeNull]
	public static GameObject GetParent([NotNull]this GameObject gameObject, bool skipHierarchyFolders)
	{
		var parent = gameObject.transform.parent;
		if(parent == null)
		{
			return null;
		}
		var parentGameObject = parent.gameObject;
		if(skipHierarchyFolders && parentGameObject.IsHierarchyFolder())
		{
			return parentGameObject.GetParent(true);
		}
		return parentGameObject;
	}

	/// <summary>
	/// Gets GameObject up the parent chain that is closest to the hierarchy root, skipping Hierarchy Folders.
	/// 
	/// In builds Hierarchy Folders will not be skipped, because we assume that all of them have been stripped
	/// from the build, or if not, that there is no reason to skip them.
	/// </summary>
	/// <param name="gameObject"> GameObject whose parent chain to check. </param>
	/// <returns> Valid parent closest to root, or null if none found. </returns>
	#if UNITY_EDITOR
	[CanBeNull]
	#else
	[NotNull]
	#endif
	public static GameObject GetRoot([NotNull]this GameObject gameObject)
	{
		#if UNITY_EDITOR
		return gameObject.GetRoot(true);
		#else
		return gameObject.transform.root.gameObject;
		#endif
	}

	/// <summary>
	/// Gets GameObject up the parent chain that is closest to the hierarchy root, with option to skip Hierarchy Folders.
	/// </summary>
	/// <param name="gameObject"> GameObject whose parent chain to check. </param>
	/// <param name="skipHierarchyFolders"> If true then Hierarchy Folders will be skipped when finding the result. </param>
	/// <returns> Valid parent closest to root, or null if none found. </returns>
	[CanBeNull]
	public static GameObject GetRoot([NotNull]this GameObject gameObject, bool skipHierarchyFolders)
	{
		var transform = gameObject.transform;
		if(!skipHierarchyFolders)
		{
			var root = transform.root;
			return root != null ? root.gameObject : null;
		}

		GetParents(transform, ReusableParentsStack);

		for(int n = ReusableParentsStack.Count - 1; n >= 0; n--)
		{
			var parent = ReusableParentsStack.Pop();
			var parentGameObject = parent.gameObject;
			if(!parentGameObject.IsHierarchyFolder())
			{
				ReusableParentsStack.Clear();
				return parentGameObject;
			}
		}
		ReusableParentsStack.Clear();
		return null;
	}

	/// <summary>
	/// Gets Transform up the parent chain that is closest to the hierarchy root, skipping Hierarchy Folders.
	/// 
	/// In builds Hierarchy Folders will not be skipped, because we assume that all of them have been stripped
	/// from the build, or if not, that there is no reason to skip them.
	/// </summary>
	/// <param name="transform"> Transform whose parent chain to check. </param>
	/// <returns> Valid parent closest to root, or null if none found. </returns>
	#if UNITY_EDITOR
	[CanBeNull]
	#else
	[NotNull]
	#endif
	public static Transform GetRoot([NotNull]this Transform transform)
	{
		#if UNITY_EDITOR
		return transform.GetRoot(true);
		#else
		return transform.root;
		#endif
	}

	/// <summary>
	/// Gets Transform up the parent chain that is closest to the hierarchy root, with option to skip Hierarchy Folders.
	/// </summary>
	/// <param name="transform"> Transform whose parent chain to check. </param>
	/// <param name="skipHierarchyFolders"> If true then Hierarchy Folders will be skipped when finding the result. </param>
	/// <returns> Valid parent closest to root, or null if none found. </returns>
	[CanBeNull]
	public static Transform GetRoot([NotNull]this Transform transform, bool skipHierarchyFolders)
	{
		if(!skipHierarchyFolders)
		{
			return transform.root;
		}

		GetParents(transform, ReusableParentsStack);

		for(int n = ReusableParentsStack.Count - 1; n >= 0; n--)
		{
			var parent = ReusableParentsStack.Pop();
			if(!parent.gameObject.IsHierarchyFolder())
			{
				ReusableParentsStack.Clear();
				return parent;
			}
		}
		ReusableParentsStack.Clear();
		return null;
	}

	/// <summary>
	/// Sets parent Transform of child, with option to skip Hierarchy Folders.
	/// When <paramref name="skipHierarchyFolders"/> is false will also automatically handle Flatten Hierarchy play mode behaviour (when enabled in preferences).
	/// </summary>
	/// <param name="child"> Child whose parent is being set. </param>
	/// <param name="parent"> New parent for child. </param>
	/// <param name="worldPositionStays"></param>
	/// <param name="skipHierarchyFolders">
	/// If true then Hierarchy Folders will be skipped when determining the parent. If <paramref name="parent"/> and all parents it might have are hierarchy folders,
	/// then <paramref name="child"/> will be moved to hierarchy root.
	/// </param>
	public static void SetParent([NotNull]this Transform child, [CanBeNull]Transform parent, bool worldPositionStays, bool skipHierarchyFolders)
	{
		#if UNITY_EDITOR
		if(parent != null && parent.gameObject.IsHierarchyFolder())
		{
			if(skipHierarchyFolders)
			{
				child.SetParent(parent.GetParent(true), worldPositionStays);
			}
			else if(HierarchyFolderPreferences.FlattenHierarchy)
			{
				int moveToIndex = HierarchyFolderUtility.GetLastChildIndexInFlatMode(parent.gameObject);
				var parentOfFolder = parent.parent;
				child.transform.SetParent(parentOfFolder, worldPositionStays);
				if(moveToIndex < 0)
				{
					#if DEV_MODE
					Debug.LogWarning("GetLastChildIndexInFlatMode result < 0");
					#endif
					return;
				}
				child.transform.SetSiblingIndex(moveToIndex);
				return;
			}
		}
		#endif

		child.transform.SetParent(parent, worldPositionStays);
	}

	/// <summary>
	/// Sets parent Transform of child, with option to skip Hierarchy Folders.
	/// When <paramref name="skipHierarchyFolders"/> is false will also automatically handle Flatten Hierarchy play mode behaviour (when enabled in preferences).
	/// </summary>
	/// <param name="child"> Child whose parent is being set. </param>
	/// <param name="parent"> New parent for child. </param>
	/// <param name="worldPositionStays"></param>
	/// <param name="skipHierarchyFolders">
	/// If true then Hierarchy Folders will be skipped when determining the parent. If <paramref name="parent"/> and all parents it might have are hierarchy folders,
	/// then <paramref name="child"/> will be moved to hierarchy root.
	/// </param>
	public static void UndoableSetParent([NotNull]this Transform child, [CanBeNull]Transform parent, string undoName, bool skipHierarchyFolders = false)
	{
		#if UNITY_EDITOR
		if(parent != null && parent.gameObject.IsHierarchyFolder())
		{
			if(skipHierarchyFolders)
			{
				if(!Application.isPlaying)
				{
					Undo.SetTransformParent(child, parent.GetParent(true), undoName); //NOTE: Doesn't support worldPositionStays=false
				}
				else
				{
					child.SetParent(parent.GetParent(true), true);
				}
			}
			else if(HierarchyFolderPreferences.FlattenHierarchy)
			{
				int moveToIndex = HierarchyFolderUtility.GetLastChildIndexInFlatMode(parent.gameObject);
				var parentOfFolder = parent.parent;

				if(!Application.isPlaying)
				{
					Undo.SetTransformParent(child, parentOfFolder, undoName); //NOTE: Doesn't support worldPositionStays=false
				}
				else
				{
					child.transform.SetParent(parentOfFolder, true);
				}

				if(moveToIndex < 0)
				{
					#if DEV_MODE
					Debug.LogWarning("GetLastChildIndexInFlatMode result < 0");
					#endif
					return;
				}
				child.transform.SetSiblingIndex(moveToIndex);
				return;
			}
		}

		if(!Application.isPlaying)
		{
			Undo.SetTransformParent(child, parent, undoName);
			return;
		}
		#endif
		
		child.transform.SetParent(parent, true);
	}

	/// <summary>
	/// Returns first transform in children with option to skip past any children that are Hierarchy Folders.
	/// <param name="transform"> Transform whose children to check. </param>
	/// <returns> First child transform that optionally doesn't contain the HierarchyFolder component, or null if has no such children. </returns>
	[CanBeNull]
	public static Transform GetFirstChild([NotNull]this Transform transform, bool skipHierarchyFolders)
	{
		#if UNITY_EDITOR
		if(!skipHierarchyFolders)
		{
			return transform.childCount == 0 ? null : transform.GetChild(0);
		}

		for(int n = 0, count = transform.childCount; n < count; n++)
		{
			var child = transform.GetChild(n);
			if(!child.gameObject.IsHierarchyFolder())
			{
				return child;
			}
			var nested = child.GetFirstChild(true);
			if(nested != null)
			{
				return nested;
			}
		}

		return null;
		#else
		return transform.childCount == 0 ? null : transform.GetChild(0);
		#endif
	}

	/// <summary>
	/// Gets all immediate children of the Transform.
	/// </summary>
	/// <param name="transform"> Transform whose children should be returned. </param>
	/// <param name="skipHierarchyFolders"> If true, any children that are Hierarchy Folders are skipped, and the children of those Hierarchy Folders are returned instead. </param>
	/// <returns> Array containing immediate children of Transform. </returns>
	[NotNull]
	public static Transform[] GetChildren([NotNull]this Transform transform, bool skipHierarchyFolders)
	{
		transform.GetChildren(ReusableTransformList, skipHierarchyFolders);
		var result = ReusableTransformList.ToArray();
		ReusableTransformList.Clear();
		return result;
	}

	/// <summary>
	/// Adds all immediate children of the Transform to the list.
	/// </summary>
	/// <param name="transform"> Transform whose children should be fetched. </param>
	/// <param name="list"> List into which children should be added. </param>
	/// <param name="skipHierarchyFolders"> If true, any children that are Hierarchy Folders are skipped, and the children of those Hierarchy Folders are fetched instead. </param>
	public static void GetChildren([NotNull]this Transform transform, [NotNull]List<Transform> list, bool skipHierarchyFolders)
	{
		#if UNITY_EDITOR
		if(HierarchyFolderPreferences.FlattenHierarchy && transform.gameObject.IsHierarchyFolder())
		{
			int myIndex = transform.GetSiblingIndex();
			#if DEV_MODE
			Debug.Assert(myIndex >= 0, myIndex.ToString());
			#endif

			var parent = transform.parent;
			if(parent == null)
			{
				var scene = transform.gameObject.scene;
				var root = scene.GetRootGameObjects();
				int rootCount = root.Length;
				for(int n = myIndex + 1; n < rootCount; n++)
				{
					var rootObject = root[n];
					if(rootObject.IsHierarchyFolder())
					{
						return;
					}
					list.Add(rootObject.transform);
				}
				return;
			}
			for(int n = myIndex + 1; n < parent.childCount; n++)
			{
				var child = parent.GetChild(n);
				if(child.gameObject.IsHierarchyFolder())
				{
					return;
				}
				list.Add(child);
			}
			return;
		}
		
		if(skipHierarchyFolders)
		{
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				var child = transform.GetChild(n);
				if(child.gameObject.IsHierarchyFolder())
				{
					child.GetChildren(list, true);
				}
				else
				{
					list.Add(child);
				}
			}
			return;
		}
		#endif

		for(int n = 0, count = transform.childCount; n < count; n++)
		{
			list.Add(transform.GetChild(n));
		}
	}

	/// <summary>
	/// Gets all immediate children of the GameObject.
	/// </summary>
	/// <param name="gameObject"> GameObject whose children should be returned. </param>
	/// <param name="skipHierarchyFolders"> If true, any children that are Hierarchy Folders are skipped, and the children of those Hierarchy Folders are returned instead. </param>
	/// <returns> Array containing immediate children of GameObject. </returns>
	[NotNull]
	public static GameObject[] GetChildren([NotNull]this GameObject gameObject, bool skipHierarchyFolders)
	{
		gameObject.GetChildren(ReusableGameObjectList, skipHierarchyFolders);
		var result = ReusableGameObjectList.ToArray();
		ReusableGameObjectList.Clear();
		return result;
	}

	/// <summary>
	/// Adds all immediate children of the GameObject to the list.
	/// </summary>
	/// <param name="gameObject"> GameObject whose children should be fetched. </param>
	/// <param name="list"> List into which children should be added. </param>
	/// <param name="skipHierarchyFolders"> If true, any children that are Hierarchy Folders are skipped, and the children of those Hierarchy Folders are fetched instead. </param>
	[CanBeNull]
	public static void GetChildren([NotNull]this GameObject gameObject, [NotNull]List<GameObject> list, bool skipHierarchyFolders)
	{
		var transform = gameObject.transform;

		#if UNITY_EDITOR
		if(HierarchyFolderPreferences.FlattenHierarchy && gameObject.IsHierarchyFolder())
		{
			int myIndex = transform.GetSiblingIndex();
			#if DEV_MODE
			Debug.Assert(myIndex >= 0, myIndex.ToString());
			#endif

			var parent = transform.parent;
			if(parent == null)
			{
				var scene = gameObject.scene;
				var root = scene.GetRootGameObjects();
				int rootCount = root.Length;
				for(int n = myIndex + 1; n < rootCount; n++)
				{
					var rootObject = root[n];
					if(rootObject.IsHierarchyFolder())
					{
						return;
					}
					list.Add(rootObject);
				}
				return;
			}
			for(int n = myIndex + 1; n < parent.childCount; n++)
			{
				var child = parent.GetChild(n).gameObject;
				if(child.IsHierarchyFolder())
				{
					return;
				}
				list.Add(child);
			}
			return;
		}
		
		if(skipHierarchyFolders)
		{
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				var child = transform.GetChild(n).gameObject;
				if(child.IsHierarchyFolder())
				{
					child.GetChildren(list, true);
				}
				else
				{
					list.Add(child);
				}
			}
			return;
		}
		#endif

		for(int n = 0, count = transform.childCount; n < count; n++)
		{
			list.Add(transform.GetChild(n).gameObject);
		}
	}

	/// <summary>
	/// Adds full parent hierarchy of transform to stack.
	/// </summary>
	/// <param name="transform"> Transform whose parent chain to fetch. </param>
	/// <param name="parentStack"> Stack into which parent hieararchy is pushed, starting from immediate parent. This should not be null. </param>
	private static void GetParents([NotNull]Transform transform, [NotNull]Stack<Transform> parentStack)
	{
		var parent = transform.parent;
		while(parent != null)
		{
			parentStack.Push(parent);
			parent = parent.parent;
		}
	}
}
#if !HIERARCHY_FOLDER_EXTENSIONS_IN_GLOBAL_NAMESPACE
}
#endif