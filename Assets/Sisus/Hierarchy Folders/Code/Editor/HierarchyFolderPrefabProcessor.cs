using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Sisus.HierarchyFolders
{
	[InitializeOnLoad]
	public class HierarchyFolderPrefabProcessor : AssetPostprocessor
	{
		private static readonly HashSet<string> processing = new HashSet<string>();

		private static void ProcessPrefabAtPath(string prefabPath)
		{
			var preferences = HierarchyFolderPreferences.Get();
			if(preferences == null)
			{
				EditorApplication.delayCall += ()=> ProcessPrefabAtPath(prefabPath);
				return;
			}

			if(preferences.foldersInPrefabs != HierachyFoldersInPrefabs.NotAllowed)
			{
				return;
			}

			var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			if(gameObject == null)
			{
				return;
			}

			if(!ContainsHierarchyFoldersInChildren(gameObject.transform))
			{
				return;
			}

			UnmakeHierarchyFoldersInChildren(gameObject.transform);
		}

		private static bool ContainsHierarchyFoldersInChildren(Transform target)
		{
			if(target.gameObject.IsHierarchyFolder())
			{
				return true;
			}

			for(int i = target.childCount - 1; i >= 0; i--)
			{
				if(ContainsHierarchyFoldersInChildren(target.GetChild(i)))
				{
					return true;
				}
			}

			return false;
		}

		private static void UnmakeHierarchyFoldersInChildren(Transform target)
		{
			if(target.gameObject.IsHierarchyFolder())
			{
				UnmakeHierarchyFolder(target.gameObject);
			}

			for(int i = target.childCount - 1; i >= 0; i--)
			{
				UnmakeHierarchyFoldersInChildren(target.GetChild(i));
			}
		}

		private static void UnmakeHierarchyFolder(GameObject gameObject)
		{
			Debug.LogWarning(HierarchyFolderMessages.PrefabNotAllowed, gameObject);
			HierarchyFolderUtility.UnmakeHierarchyFolder(gameObject, gameObject.GetComponent<HierarchyFolder>());
		}

		void OnPreprocessAsset()
		{
			#if UNITY_2019_1_OR_NEWER
			if(!string.Equals(assetImporter.GetType().Name, "PrefabImporter"))
			#else
			string importer = assetImporter.GetType().Name;
			if(!string.Equals(importer, "AssetImporter") && !string.Equals(importer, "PrefabImporter"))
			#endif
			{
				return;
			}

			if(!processing.Add(assetImporter.assetPath))
			{
				return;
			}

			// Can't use AssetDatabase.LoadAssetAtPath without waiting a bit first
			EditorApplication.delayCall += ProcessPrefab;
		}

		private void ProcessPrefab()
		{
			string prefabPath = assetImporter.assetPath;

			processing.Remove(prefabPath);

			ProcessPrefabAtPath(prefabPath);
		}
	}
}