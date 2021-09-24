// PreferencesApplier can make changes to this region based on preferences

#define DEBUG_FLATTEN
#define DEBUG_UNFLATTEN

#define DEBUG_REMOVE_HIERARCHY_FOLDER
#define DEBUG_STRIP_SCENE
#define DEBUG_STRIP_FOLDER
#define DEBUG_UNPACK_PREFAB
#define DEBUG_UNMAKE_HIERARCHY_FOLDER

#define ASSERT_COMPONENT_COUNT
//#define ASSERT_CHILD_COUNT

#if UNITY_EDITOR
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnityEditor;
using Sisus.HierarchyFolders.Prefabs;
using System.Runtime.CompilerServices;

namespace Sisus.HierarchyFolders
{
	public static class HierarchyFolderUtility
	{
		private const int AggressiveInlining = 256; //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later

		public static bool NowStripping
		{
			get;
			private set;
		}

		// When inactive hierarchy folders have active children, we only want to set them active once all hierarchy folders have been stripped.
		// Otherwise their Awake and OnEnable methods would get triggered immediately when they are unparented from the inactive hierarchy folder,
		// which would mean that they could still find unstripped hierarchy folders in the scene.
		private static readonly List<GameObject> SetChildrenActiveDelayed = new List<GameObject>(0);
		private static readonly List<GameObject> RootGameObjects = new List<GameObject>(100);
		private static readonly List<GameObject> ResetTransformStateReusableGameObjectsList = new List<GameObject>(16);
		private static readonly List<Component> ReusableComponentsList = new List<Component>(2);

		/// <summary>
		/// Determines whether or not the given gameObject is a Hierarchy Folder or not.
		/// <para>
		/// This is faster than <see cref="GameObject.IsHierarchyFolder()"/> but slightly less
		/// reliable and might give inaccurate results during loading and unloading of scene objects
		/// and prefabs. This is because it relies on Hierarchy Folders registering themselves
		/// during the Awake and OnValidate methods and deregistering themselves during the
		/// OnDestroy event.
		/// </para>
		/// </summary>
		/// <param name="gameObject"> GameObject to check. </param>
		/// <returns> <see langword="true"/> if gameObject is a Hierarchy Folder; otherwise, <see langword="false"/>. </returns>
		[MethodImpl(AggressiveInlining)]
		public static bool IsRegisteredHierarchyFolder([NotNull]GameObject gameObject)
		{
			return HierarchyFolder.gameObjects.Contains(gameObject);
		}

		/// <summary>
		/// Applites provided stripping type to all hierarchy folders in all currently loaded scenes.
		/// </summary>
		/// <param name="strippingType"></param>
		public static void ApplyStrippingTypeToAllLoadedScenes(StrippingType strippingType)
		{
			for(int s = 0, scount = SceneManager.sceneCount; s < scount; s++)
			{
				ApplyStrippingType(SceneManager.GetSceneAt(s), strippingType);
			}
		}

		public static void ApplyStrippingType(Scene scene, StrippingType strippingType)
		{
			#if DEV_MODE && DEBUG_STRIP_SCENE
			Debug.Assert(RootGameObjects.Count == 0);
			#endif

			scene.GetRootGameObjects(RootGameObjects);

			#if DEV_MODE && DEBUG_STRIP_SCENE
			Debug.Log("Stripping " + RootGameObjects.Count + " root objects in scene " + scene.name + "...");
			#endif

			for(int n = 0, count = RootGameObjects.Count; n < count; n++)
			{
				CheckForAndRemoveHierarchyFoldersInChildren(RootGameObjects[n].transform, strippingType, true, SetChildrenActiveDelayed);
			}
			RootGameObjects.Clear();

			for(int n = 0, count = SetChildrenActiveDelayed.Count; n < count; n++)
			{
				var child = SetChildrenActiveDelayed[n];
				if(child == null)
				{
					continue;
				}
				child.SetActive(true);
			}
			SetChildrenActiveDelayed.Clear();

			#if DEV_MODE && DEBUG_STRIP_SCENE
			Debug.Log("Stripping scene "+scene.name+" done.");
			#endif
		}

		public static void CheckForAndRemoveHierarchyFoldersInChildren([NotNull]Transform transform, StrippingType strippingType, bool destroyImmediate)
		{
			CheckForAndRemoveHierarchyFoldersInChildren(transform, strippingType, destroyImmediate, SetChildrenActiveDelayed);

			for(int n = 0, count = SetChildrenActiveDelayed.Count; n < count; n++)
			{
				var child = SetChildrenActiveDelayed[n];
				if(child == null)
				{
					continue;
				}
				child.SetActive(true);
			}
			SetChildrenActiveDelayed.Clear();
		}

		public static bool HasSupernumeraryComponents([NotNull] HierarchyFolder hierarchyFolder)
        {
			return HasSupernumeraryComponents(hierarchyFolder, HierarchyFolderPreferences.Get());
		}

		internal static bool HasSupernumeraryComponents([NotNull]HierarchyFolder hierarchyFolder, HierarchyFolderPreferences preferences)
		{
			if(preferences.allowComponentsOnHierarchyFolders == AllowedComponents.All)
            {
				return false;
            }

			var components = ReusableComponentsList;
			hierarchyFolder.GetComponents(components);

			// A hierarchy folder should only have Transform (or RectTransform) and HierarchyFolder components.
			bool tooManyComponents = components.Count > 2;

			if(!tooManyComponents)
			{
				components.Clear();
				return false;
			}

			if(preferences.allowComponentsOnHierarchyFolders == AllowedComponents.None)
            {
				components.Clear();
				return true;
			}

			var allowedTypes = preferences.allowComponentsByTypeOnHierarchyFolders;
			for(int i = components.Count - 1; i >= 1; i--) // skipping Transform component
			{
				var component = components[i];
				if(component == null)
				{
					continue;
				}

				var componentType = component.GetType();
				if(allowedTypes.Contains(componentType))
				{
					continue;
				}

				var allowedNames = preferences.allowComponentsByNameOnHierarchyFolders;
				if(allowedNames.Contains(componentType.Name) || allowedNames.Contains(componentType.FullName))
				{
					allowedTypes.Add(componentType);
					continue;
				}

				#if DEV_MODE && DEBUG_SUPERNUMERARY_COMPONENTS
				Debug.Log($"{componentType.FullName} not found among {preferences.AllowComponentsByNameOnHierarchyFolders.Count} allowed names nor {allowedTypes.Count} names: [{string.Join(", ", preferences.AllowComponentsByNameOnHierarchyFolders)}] types: [{string.Join(", ", preferences.AllowComponentsByTypeOnHierarchyFolders)}].");
				#endif

				ReusableComponentsList.Clear();
				return true;
			}

			ReusableComponentsList.Clear();
			return false;
		}

		private static void CheckForAndRemoveHierarchyFoldersInChildren([NotNull]Transform transform, StrippingType strippingType, bool destroyImmediate, List<GameObject> setChildrenActiveDelayed)
		{
			if(transform.gameObject.IsConnectedPrefabInstance())
			{
				#if DEV_MODE && DEBUG_UNPACK_PREFAB
				Debug.Log("Unpacking GameObject " + transform.name + " on asset "+ PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) + " with IsPrefabAsset=" + transform.gameObject.IsPrefabAsset() + ", IsConnectedPrefabInstance=" + transform.gameObject.IsConnectedPrefabInstance()+ ", IsDisconnectedPrefabInstance=" + transform.gameObject.IsDisconnectedPrefabInstance(), transform.root);
				#endif

				#if UNITY_2018_3_OR_NEWER
				PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				#else
				PrefabUtility.DisconnectPrefabInstance(transform.gameObject);
				#endif
            }

			bool wasStripping = NowStripping;
			NowStripping = true;

			int childCount = transform.childCount;
			var children = new Transform[childCount];
			for(int n = 0; n < childCount; n++)
			{
				children[n] = transform.GetChild(n);
			}

			var hierarchyFolder = transform.GetComponent<HierarchyFolder>();
			if(hierarchyFolder != null)
			{
				var hierarchyFolderParent = transform.parent;
				int setSiblingIndex = transform.GetSiblingIndex() + 1;

				#if DEV_MODE && DEBUG_STRIP_FOLDER
				Debug.Log("Stripping "+transform.name+" @ " + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) + " with " + childCount + " children...", transform);
				#endif

				switch(strippingType)
				{
					case StrippingType.FlattenHierarchyAndRemoveGameObject:
					case StrippingType.FlattenHierarchy:
					case StrippingType.FlattenHierarchyAndRemoveComponent:
					case StrippingType.FlattenHierarchyAndDisableComponent:
						for(int n = 0; n < childCount; n++)
						{
							var child = children[n];

							if(child.gameObject.IsConnectedPrefabInstance())
                            {
								#if DEV_MODE && DEBUG_UNPACK_PREFAB
								Debug.Log("Unpacking child " + child.name + " on asset " + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) + " with IsPrefabAsset=" + child.gameObject.IsPrefabAsset() + ", IsConnectedPrefabInstance=" + child.gameObject.IsConnectedPrefabInstance()+ ", IsDisconnectedPrefabInstance=" + child.gameObject.IsDisconnectedPrefabInstance(), transform.root);
								#endif
								
								#if UNITY_2018_3_OR_NEWER
								PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
								#else
								PrefabUtility.DisconnectPrefabInstance(child.gameObject);
								#endif
                            }

							if(!transform.gameObject.activeSelf && child.gameObject.activeSelf && Application.isPlaying)
							{
								child.gameObject.SetActive(false);
								setChildrenActiveDelayed.Add(child.gameObject);
							}

							child.SetParent(hierarchyFolderParent, true);

							child.SetSiblingIndex(setSiblingIndex);
							setSiblingIndex++;
						}
						for(int n = 0; n < childCount; n++)
						{
							CheckForAndRemoveHierarchyFoldersInChildren(children[n], strippingType, destroyImmediate, setChildrenActiveDelayed);
						}
						break;
					default:
						for(int n = 0; n < childCount; n++)
						{
							var child = children[n];
							CheckForAndRemoveHierarchyFoldersInChildren(child, strippingType, destroyImmediate, setChildrenActiveDelayed);
						}
						break;
				}

				#if ASSERT_COMPONENT_COUNT
				var componentCount = transform.GetComponents<Component>().Length;
				if(componentCount != 2)
				{
					Debug.LogError("HierarchyFolder " + transform.name + " contained " + componentCount + " components! All components will be destroyed along with the Hierarchy Folder.");
				}
				#endif

				#if ASSERT_CHILD_COUNT
				Debug.Assert(transform.childCount == 0);
				#endif

				#if DEV_MODE && DEBUG_REMOVE_HIERARCHY_FOLDER
				Debug.Log("Destroying HierarchyFolder: " + transform.name +" using "+(destroyImmediate ? "DestroyImmediate" : "Destroy")+ "\nstrippingType="+strippingType+", prefabInstanceStatus="+ PrefabUtility.GetPrefabInstanceStatus(transform.gameObject), strippingType == StrippingType.FlattenHierarchyAndRemoveGameObject ? null : transform);
				#endif

				switch(strippingType)
				{
					case StrippingType.FlattenHierarchyAndRemoveGameObject:
						// During scene load using DestroyImmediate instead of Destroy even in play mode so that other scripts won't be aware of the hierarchy folders ever having existed.
						// With prefab instances being instantiated after initial scene loading we unfortunately can't use DestroyImmediate, because it would it will result in
						// "UnityException: Instantiate failed because the clone was destroyed during creation. This can happen if DestroyImmediate is called in MonoBehaviour.Awake."
						if(destroyImmediate)
						{
							Object.DestroyImmediate(transform.gameObject, true);
						}
						else
						{
							// Since Destroy will only remove the GameObject after a short delay, set the GameObject inactive
							// and move it to root to hide it as well as possible from other scripts. This way the GameObject
							// cannot be found using Transform.parent, Transform.GetChild, Transform.root nor FindObjectsOfType<Transform>().
							transform.gameObject.SetActive(false);
							transform.SetParent(null, false);
							Object.Destroy(transform.gameObject);
						}
						break;
					case StrippingType.FlattenHierarchyAndRemoveComponent:
					case StrippingType.RemoveComponent:
						Object.DestroyImmediate(hierarchyFolder, true);
						break;
					case StrippingType.FlattenHierarchyAndDisableComponent:
					case StrippingType.DisableComponent:
						hierarchyFolder.enabled = false;
						transform.hideFlags = HideFlags.None;
						hierarchyFolder.hideFlags = HideFlags.None;
						break;
				}
			}
			else
			{
				for(int n = 0; n < childCount; n++)
				{
					CheckForAndRemoveHierarchyFoldersInChildren(children[n], strippingType, true, setChildrenActiveDelayed);
				}
			}

			NowStripping = wasStripping;
		}

		public static void FlattenAndDestroyHierarchyFolder([NotNull]HierarchyFolder hierarchyFolder)
		{
			bool wasStripping = NowStripping;
			NowStripping = true;

			var transform = hierarchyFolder.transform;
			int childCount = transform.childCount;
			var children = new Transform[childCount];
			for(int n = 0; n < childCount; n++)
			{
				children[n] = transform.GetChild(n);
			}

			var hierarchyFolderParent = transform.parent;
			int setSiblingIndex = transform.GetSiblingIndex() + 1;

			#if DEV_MODE && DEBUG_STRIP_FOLDER
			Debug.Log("Stripping "+transform.name+" with "+childCount+" children...");
			#endif

			// Only set children active after hierarchy folder has been stripped,
			// so that they cannot get a reference to it before it has been destroyed
			List<GameObject> setChildrenActiveDelayed = null;

			for(int n = 0; n < childCount; n++)
			{
				var child = children[n];

				if(!transform.gameObject.activeSelf && child.gameObject.activeSelf && Application.isPlaying)
				{
					if(setChildrenActiveDelayed == null)
					{
						setChildrenActiveDelayed = new List<GameObject>();
					}
					child.gameObject.SetActive(false);
					setChildrenActiveDelayed.Add(child.gameObject);
				}

				child.SetParent(hierarchyFolderParent, true);

				child.SetSiblingIndex(setSiblingIndex);
				setSiblingIndex++;
			}

			// Using DestroyImmediate instead of Destroy even in builds so that other scripts won't be aware of the hierarchy folders ever having existed.
			Object.DestroyImmediate(transform.gameObject, true);

			if(setChildrenActiveDelayed != null)
			{
				for(int n = 0, count = setChildrenActiveDelayed.Count; n < count; n++)
				{
					setChildrenActiveDelayed[n].SetActive(true);
				}
			}

			NowStripping = wasStripping;
		}

		public static void UnmakeHierarchyFolder([NotNull]GameObject gameObject, [CanBeNull]HierarchyFolder hierarchyFolder)
		{
			#if DEV_MODE && DEBUG_UNMAKE_HIERARCHY_FOLDER
			Debug.Log("UnmakeHierarchyFolder(" + gameObject.name + ")", gameObject);
			#endif

			if(hierarchyFolder != null)
			{
				if(!Application.isPlaying || gameObject.IsPrefabAsset())
				{
					Object.DestroyImmediate(hierarchyFolder, true);
				}
				else
				{
					Object.Destroy(hierarchyFolder);
				}
			}

			var preferences = HierarchyFolderPreferences.Get();
			string setName = gameObject.name;
			string prefix = preferences.namePrefix;
			if(prefix.Length > 0 && setName.StartsWith(prefix, StringComparison.Ordinal))
			{
				setName = setName.Substring(0);
			}
			string suffix = preferences.nameSuffix;
			if(suffix.Length > 0 && setName.EndsWith(setName, StringComparison.Ordinal))
			{
				setName = setName.Substring(0, setName.Length - suffix.Length);
			}
			if(preferences.forceNamesUpperCase && setName.Length > 1)
			{
				setName = setName[0] + setName.Substring(1).ToLower();
			}

			if(!string.Equals(setName, gameObject.name))
			{
				gameObject.name = setName;
				EditorUtility.SetDirty(gameObject);
			}

			var transform = gameObject.transform;
			transform.hideFlags = HideFlags.None;

			EditorUtility.SetDirty(transform);
		}

		public static int GetLastChildIndexInFlatMode(GameObject hierarchyFolder)
		{
			var transform = hierarchyFolder.transform;
			int myIndex = transform.GetSiblingIndex();
			#if DEV_MODE
			Debug.Assert(myIndex >= 0, myIndex.ToString());
			#endif

			var parent = transform.parent;
			if(parent == null)
			{
				var scene = hierarchyFolder.scene;
				if(!scene.IsValid())
                {
					return 0;
                }

				var root = scene.GetRootGameObjects();
				int rootCount = root.Length;

				for(int n = myIndex + 1; n < rootCount; n++)
				{
					if(root[n].IsHierarchyFolder())
					{
						#if DEV_MODE
						Debug.Assert(n > 0, n.ToString());
						#endif
						return n;
					}
				}
				return rootCount;
			}

			for(int n = myIndex + 1; n < parent.childCount; n++)
			{
				if(parent.GetChild(n).gameObject.IsHierarchyFolder())
				{
					return n;
				}
			}
			return parent.childCount;
		}

		public static void SetParent([NotNull]GameObject child, [CanBeNull]GameObject parent, bool worldPositionStays)
		{
			if(parent == null)
			{ 
				child.transform.SetParent(null, worldPositionStays);
				return;
			}

			if(HierarchyFolderPreferences.FlattenHierarchy && parent.IsHierarchyFolder())
			{
				int moveToIndex = GetLastChildIndexInFlatMode(parent);
				var parentOfFolder = parent.transform.parent;
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

			child.transform.SetParent(parent.transform, worldPositionStays);
		}

		public static void ResetTransformStateWithoutAffectingChildren(Transform transform)
		{
			var gameObject = transform.gameObject;

			if(!gameObject.activeSelf)
			{
				var preferences = HierarchyFolderPreferences.Get();

				if(preferences.askAboutAllowInactiveHierarchyFolders)
				{
					if(EditorUtility.DisplayDialog("Hierarchy Folder Active Flag Behaviour", "What would you like to happen when the active flag of a Hierarchy Folder is modified?\n\nTarget Hierarchy Folder:\nAdjust the active state of the Hierarchy Folder itself. This will have no effect in the final build since all Hierarchy Folders will be stripped even if inactive.\n\nTarget Children:\nModify active state of all child Objects and keep the Hierarchy Folder itself always active.\nThis is the recommended behaviour.\n\n(You can change your choice at any time in the preferences.)", "Target Hierarchy Folder", "Target Children"))
					{
						preferences.allowInactiveHierarchyFolders = true;
					}
					else
					{
						preferences.allowInactiveHierarchyFolders = false;
					}
					preferences.askAboutAllowInactiveHierarchyFolders = false;
					preferences.SaveState();
				}

				if(!preferences.allowInactiveHierarchyFolders)
				{
					Undo.RegisterFullObjectHierarchyUndo(gameObject, "Toggle Hierarchy Folder Children Active");

					gameObject.SetActive(true);

					if(transform.childCount > 0)
					{
						var children = ResetTransformStateReusableGameObjectsList;
						gameObject.GetChildren(children, true);
						int childCount = children.Count;
						if(childCount > 0)
						{
							var firstChild = children[0];
							bool setActive = !firstChild.activeSelf;

							firstChild.SetActive(setActive);
							for(int n = 1; n < childCount; n++)
							{
								children[n].SetActive(setActive);
							}

							ResetTransformStateReusableGameObjectsList.Clear();
						}
					}
				}
			}

			var rectTransform = transform as RectTransform;
			if(transform.localPosition != Vector3.zero || transform.localEulerAngles != Vector3.zero || transform.localScale != Vector3.one || (rectTransform != null && (rectTransform.anchorMin != Vector2.zero || rectTransform.anchorMax != Vector2.one || rectTransform.pivot != new Vector2(0.5f, 0.5f) || rectTransform.offsetMin != Vector2.zero || rectTransform.offsetMax != Vector2.zero)))
			{
				Undo.RegisterFullObjectHierarchyUndo(gameObject, "Reset Hierarchy Folder Transform");
				ForceResetTransformStateWithoutAffectingChildren(transform, false);
			}
		}

		internal static void ForceResetTransformStateWithoutAffectingChildren(Transform transform, bool alsoConvertToRectTransform)
		{
			#if DEV_MODE && DEBUG_RESET_STATE
			Debug.Log("Reset State: \"" + transform.name + "\"", transform);
			#endif

			bool canModifyParents = !transform.gameObject.IsPrefabAssetOrInstance();

			// For non-prefab instances we can use a method where children are unparented temporarily.
			// This has the benefit the the world position of all children remains unchanged throughout the whole process.
			int childCount = transform.childCount;
			var children = new Transform[childCount];
			for(int n = childCount - 1; n >= 0; n--)
			{
				children[n] = transform.GetChild(n);
			}

			if(canModifyParents)
			{
				var parent = transform.parent;
				for(int n = childCount - 1; n >= 0; n--)
				{
					// NOTE: Using SetParent with worldPositionStays true is very important (even with RectTransforms).
					children[n].SetParent(parent, true);
				}
			}

			RectTransform rectTransform;
			if(alsoConvertToRectTransform)
			{
				rectTransform = transform.gameObject.AddComponent<RectTransform>();
				rectTransform.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
				transform = rectTransform;
			}
			else
			{
				rectTransform = transform as RectTransform;
			}

			if(rectTransform != null)
			{
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
			}

			transform.localPosition = Vector3.zero;
			transform.localEulerAngles = Vector3.zero;
			transform.localScale = Vector3.one;

			if(canModifyParents)
			{
				for(int n = 0; n < childCount; n++)
				{
					children[n].SetParent(transform, true);
					children[n].SetAsLastSibling();
				}
			}

			EditorUtility.SetDirty(transform);
		}
	}
}
#endif