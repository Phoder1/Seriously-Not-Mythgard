#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
#if UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Sisus.HierarchyFolders.Prefabs
{
    public static class GameObjectExtensions
	{
		public static bool IsPrefabAsset(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfPrefabAsset(gameObject);
			#else
			return PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab;
			#endif
		}

		public static bool IsPrefabAssetOrOpenInPrefabStage(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfPrefabAsset(gameObject) || PrefabStageUtility.GetPrefabStage(gameObject) != null;
			#else
			return PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab;
			#endif
		}

		public static bool IsPrefabAssetOrInstance(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab;
			#else
			var prefabType = PrefabUtility.GetPrefabType(gameObject);
			return prefabType == PrefabType.Prefab || prefabType == PrefabType.PrefabInstance;
			#endif
		}

		public static bool IsConnectedPrefabInstance(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Connected;
			#else
			return PrefabUtility.GetPrefabType(gameObject) == PrefabType.PrefabInstance;
			#endif
		}

		public static bool IsDisconnectedPrefabInstance(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Disconnected;
			#else
			return PrefabUtility.GetPrefabType(gameObject) == PrefabType.DisconnectedPrefabInstance;
			#endif
		}

		public static bool IsPartOfInstantiatedPrefabInstance(this GameObject gameObject)
		{
			for(var transform = gameObject.transform; transform != null; transform = transform.parent)
			{
				if(transform.name.EndsWith("(Clone)", StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsPartOfPrefabVariant(this GameObject gameObject)
        {
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfVariantPrefab(gameObject);
			#else
			return false;
			#endif
		}

		public static bool IsConnectedOrDisconnectedPrefabInstance(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
			return prefabStatus == PrefabInstanceStatus.Connected || prefabStatus == PrefabInstanceStatus.Disconnected;
			#else
			var prefabType = PrefabUtility.GetPrefabType(gameObject);
			return prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance;
			#endif
		}

		public static bool IsPrefabInstanceRoot(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsAnyPrefabInstanceRoot(gameObject);
			#else
			return PrefabUtility.GetPrefabType(gameObject) == PrefabType.PrefabInstance && PrefabUtility.FindPrefabRoot(gameObject) == gameObject;
			#endif
		}

		public static GameObject GetOutermostPrefabInstanceRoot(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
			#else
			return PrefabUtility.FindPrefabRoot(gameObject);
			#endif
		}

		public static bool IsOpenInPrefabStage(this GameObject gameObject)
		{
			#if UNITY_2018_3_OR_NEWER
			return PrefabStageUtility.GetPrefabStage(gameObject) != null;
			#else
			return false;
			#endif
		}

		public static void SetTagForAllChildren(this GameObject gameObject, string tag)
		{
			var transform = gameObject.transform;
			for(int i = transform.childCount - 1; i >= 0; i--)
			{
				bool skipHierarchyFolders = !string.Equals(tag, "Untagged");
				SetTagForTransformAndAllChildren(transform.GetChild(i), tag, skipHierarchyFolders);
			}
		}

		private static void SetTagForTransformAndAllChildren(Transform transform, string tag, bool skipHierarchyFolders)
		{
			if(!skipHierarchyFolders || !transform.gameObject.IsHierarchyFolder())
			{
				transform.gameObject.tag = tag;
			}

			for(int i = transform.childCount - 1; i >= 0; i--)
			{
				SetTagForTransformAndAllChildren(transform.GetChild(i), tag, skipHierarchyFolders);
			}
		}
	}
}
#endif