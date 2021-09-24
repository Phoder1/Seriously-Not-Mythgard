//#define DEBUG_RUNTIME_STRIPPING

using UnityEngine;
using Sisus.Attributes;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#else
using System.Collections.Generic;
#endif

namespace Sisus.HierarchyFolders
{
	#if UNITY_2018_3_OR_NEWER
	[ExecuteAlways]
	#else
	[ExecuteInEditMode]
	#endif
	#if UNITY_EDITOR
	[InitializeOnLoad, HideTransformInInspector, HideComponentInInspector, OnlyComponent, AddComponentMenu("Hierarchy/Hierarchy Folder")]
	#endif
	[DefaultExecutionOrder(-32000)]
	public sealed class HierarchyFolder : MonoBehaviour
	{
		#if UNITY_EDITOR
		internal static readonly HierarchyFolderManager Manager;
		internal static readonly HashSet<GameObject> gameObjects = new HashSet<GameObject>();

		static HierarchyFolder()
		{
			Manager = new HierarchyFolderManager();
		}

		private void Reset()
		{
			Manager.OnReset(this);
		}

		private void OnValidate()
		{
			if(this == null)
			{
				return;
			}

			Manager.OnValidate(this);
		}
		#endif

		private void Awake()
		{
			#if UNITY_EDITOR
			Manager.OnAwake(this);
			#else
			FlattenAndDestroy(transform);
			#endif
		}

		#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
		// If Enter Play Mode Options is enabled with Reload Scene disabled, then Awake does not get called when entering play mode.
		// In this situation will need to use OnEnable instead.
		private void OnEnable()
        {
			if(!EditorSettings.enterPlayModeOptionsEnabled || !EditorApplication.isPlayingOrWillChangePlaymode)
            {
				return;
            }

			bool sceneReloadingDisabled = (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) == EnterPlayModeOptions.DisableSceneReload;
			if(!sceneReloadingDisabled)
            {
				return;
            }

			Manager.OnAwake(this);
		}
		#endif

		#if !UNITY_EDITOR
		private static void FlattenAndDestroy(Transform transform)
		{
			#if DEV_MODE && DEBUG_RUNTIME_STRIPPING
			Debug.Log("FlattenAndDestroy("+transform.name+")", transform);
			#endif

			// Only set children active after hierarchy folder has been stripped,
			// so that they cannot get a reference to it before it has been destroyed.
			List<GameObject> setChildrenActiveDelayed = null;

			FlattenAndDestroy(transform, setChildrenActiveDelayed);

			if(setChildrenActiveDelayed != null)
			{
				for(int n = 0, count = setChildrenActiveDelayed.Count; n < count; n++)
				{
					var child = setChildrenActiveDelayed[n];
					if(child == null)
					{
						continue;
					}
					child.SetActive(true);
				}
			}
		}
		#endif

		#if !UNITY_EDITOR
		private static void FlattenAndDestroy(Transform transform, List<GameObject> setChildrenActiveDelayed)
		{
			#if DEV_MODE && DEBUG_BUILDS_STRIPPING
			Debug.Log("Stripping " + transform.name + " with  " +transform.childCount + " children...");
			#endif
			
			var hierarchyFolderParent = transform.parent;

			if(transform.gameObject.activeSelf)
			{
				if(hierarchyFolderParent == null)
				{
					transform.DetachChildren();
				}
				else
				{
					int childCount = transform.childCount;
					var children = new Transform[childCount];
					for(int n = 0; n < childCount; n++)
					{
						children[n] = transform.GetChild(n);
					}
					for(int n = 0; n < childCount; n++)
					{
						var child = children[n];
						child.SetParent(hierarchyFolderParent, true);
					}

					// Since Destroy will only remove the GameObject after a short delay, move it to root to hide it as
					// well as possible from other scripts. This way the GameObject cannot be found using Transform.parent,
					// Transform.root or Transform.GetChild for example.
					transform.SetParent(null, false);
				}
			}
			else
			{
				int childCount = transform.childCount;
				var children = new Transform[childCount];
				for(int n = 0; n < childCount; n++)
				{
					children[n] = transform.GetChild(n);
				}
				for(int n = 0; n < childCount; n++)
				{
					var child = children[n];

					if(child.gameObject.activeSelf)
					{
						if(setChildrenActiveDelayed == null)
						{
							setChildrenActiveDelayed = new List<GameObject>();
						}
						child.gameObject.SetActive(false);
						setChildrenActiveDelayed.Add(child.gameObject);
					}

					child.SetParent(hierarchyFolderParent, true);
				}

				// Since Destroy will only remove the GameObject after a short delay, move it to root to hide it as
				// well as possible from other scripts. This way the GameObject cannot be found using Transform.parent,
				// Transform.root or Transform.GetChild for example.
				transform.SetParent(null, false);
			}

			// Since Destroy will only remove the GameObject after a short delay, set the GameObject inactive
			// to hide it as well as possible from other scripts. This way the GameObject cannot be found using
			// FindObjectsOfType<Transform>() for example.
			transform.gameObject.SetActive(false);

			// We unfortunately can't use DestroyImmediate with prefab instances, because it would result in
			// "UnityException: Instantiate failed because the clone was destroyed during creation. This can happen if DestroyImmediate is called in MonoBehaviour.Awake."
			Destroy(transform.gameObject);
		}
		#endif

		#if UNITY_EDITOR
		private void OnDestroy()
		{
			gameObjects.Remove(gameObject);
			Manager.OnDestroy(this);
		}

		[ContextMenu("Turn Into Normal GameObject")]
		private void TurnIntoNormalGameObject()
		{
			HierarchyFolderUtility.UnmakeHierarchyFolder(gameObject, this);
		}
		#endif
	}
}