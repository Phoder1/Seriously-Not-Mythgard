#define DISABLE_TRANSFORM_GIZMOS // Disallow moving, rotating or scaling hierarchy folders using the editor tools

//#define DEBUG_ON_VALIDATE
//#define DEBUG_HIERARCHY_CHANGED
//#define DEBUG_ON_SCENE_GUI
//#define DEBUG_AWAKE

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using Sisus.HierarchyFolders.Prefabs;
using Sisus.HierarchyFolders.Extensions;
using Object = UnityEngine.Object;

namespace Sisus.HierarchyFolders
{
    public sealed class HierarchyFolderManager
	{
		private static Tool editorToolTempDisabled = Tool.None;

		private readonly Predicate<HierarchyFolder> isNullHierarchyFolder = IsNull;
		private readonly IComparer<HierarchyFolder> hierarchyFolderDepthComparer = new HierarchyDepthComparer();
		private readonly List<HierarchyFolder> hierarchyFolders = new List<HierarchyFolder>();
		private readonly HashSet<HierarchyFolder> destroying = new HashSet<HierarchyFolder>();
		private HierarchyFolderPreferences preferences;

		private bool initialized = false;

		public HierarchyFolderManager()
		{
			EditorApplication.delayCall += Initialize;
		}

		internal void OnReset(HierarchyFolder hierarchyFolder)
		{
			if(HierarchyFolderUtility.HasSupernumeraryComponents(hierarchyFolder))
			{
				Debug.LogWarning("Can't convert GameObject with extraneous components into a Hierarchy Folder.\nThis is not supported since Hierarchy Folders are stripped from builds.\nIf you want to be have components of this type on Hierarchy Folders for their editor only functionality, you can whitelist the type in Edit > Preferences > Hierarchy Folders > Whitelisted Components.", hierarchyFolder.gameObject);

				TurnIntoNormalGameObject(hierarchyFolder);
				return;
			}

			var gameObject = hierarchyFolder.gameObject;

			if(HierarchyFolderPreferences.Get().foldersInPrefabs == HierachyFoldersInPrefabs.NotAllowed)
			{
				bool isPrefabInstance = gameObject.IsConnectedPrefabInstance();
				if(isPrefabInstance || gameObject.IsPrefabAssetOrOpenInPrefabStage())
				{
					OnHierarchyFolderDetectedOnAPrefabAndNotAllowed(hierarchyFolder, isPrefabInstance);
					return;
				}
			}

			var transform = hierarchyFolder.transform;
			HierarchyFolderUtility.ResetTransformStateWithoutAffectingChildren(transform);

			// Don't hide transform in prefabs or prefab instances to avoid internal Unity exceptions
			if(!gameObject.IsPrefabAssetOrInstance())
			{
				transform.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			}
			else
            {
				transform.hideFlags = HideFlags.NotEditable;
            }
			hierarchyFolder.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			EditorUtility.SetDirty(transform);
			gameObject.isStatic = true;
			EditorUtility.SetDirty(hierarchyFolder);
			var preferences = HierarchyFolderPreferences.Get();
			if(preferences.autoNameOnAdd)
			{
				if(gameObject.name.Equals("GameObject", StringComparison.Ordinal) || gameObject.name.StartsWith("GameObject (", StringComparison.Ordinal))
				{
					gameObject.name = preferences.defaultName;
				}
				else
				{
					ApplyNamingPattern(hierarchyFolder);
				}
			}

			EditorUtility.SetDirty(gameObject);
		}

		internal void OnValidate(HierarchyFolder hierarchyFolder)
		{
			#if DEV_MODE && DEBUG_ON_VALIDATE
			Debug.Log("OnValidate", hierarchyFolder); 
			#endif

			if(!initialized)
			{
				Initialize();
			}

			if(HierarchyFolder.gameObjects.Add(hierarchyFolder.gameObject))
			{
				Register(hierarchyFolder);
			}
		}

		internal void OnAwake(HierarchyFolder hierarchyFolder)
		{
			#if DEV_MODE && DEBUG_AWAKE
			Debug.Log("Awake", hierarchyFolder);
			#endif

			if(!initialized)
			{
				Initialize();
			}

			bool unregistered = HierarchyFolder.gameObjects.Add(hierarchyFolder.gameObject);

			if(EditorApplication.isPlayingOrWillChangePlaymode && !hierarchyFolder.gameObject.IsPrefabAssetOrOpenInPrefabStage())
			{
				PlayModeStripper.OnSceneObjectAwake(hierarchyFolder.gameObject);
				return;
			}

			// Hide flags are reset for all scene instances on scene reload, so we need to reapply them.
			hierarchyFolder.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			var transform = hierarchyFolder.transform;

			if(transform.hideFlags == HideFlags.None)
			{
				// Don't hide transform in prefabs or prefab instances to avoid internal Unity exceptions
				if(!transform.gameObject.IsPrefabAssetOrInstance())
				{
					transform.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
				}
				else
				{
					transform.hideFlags = HideFlags.NotEditable;
				}
			}

			if(unregistered)
			{
				Register(hierarchyFolder);
			}
		}

		internal void OnDestroy(HierarchyFolder hierarchyFolder)
		{
			if(!initialized)
			{
				Initialize();
			}

			Deregister(hierarchyFolder);
		}

		private void Register(HierarchyFolder hierarchyFolder)
		{
			if(hierarchyFolder == null)
			{
				#if DEV_MODE
				Debug.LogWarning("HierarchyFolderController.Register called with null hierarchy folder.");
				#endif
				return;
			}

			hierarchyFolders.RemoveAll(isNullHierarchyFolder);
			hierarchyFolders.AddSorted(hierarchyFolder, hierarchyFolderDepthComparer);

			#if DEV_MODE && DEBUG_REGISTER
			Debug.Log("Register \"" + hierarchyFolder.name+ "\"\nTotal Count: " + hierarchyFolders.Count, hierarchyFolder);
			#endif
		}

		private void Deregister(HierarchyFolder hierarchyFolder)
		{
			#if UNITY_2020_1_OR_NEWER
			if(hierarchyFolder is null)
			#else
			if(hierarchyFolder as object == null)
			#endif
            {
				return;
            }

			HierarchyFolder.gameObjects.Remove(hierarchyFolder.gameObject);

			hierarchyFolders.Remove(hierarchyFolder);

			#if DEV_MODE && DEBUG_DEREGISTER
			Debug.Log("Deregister: " + (hierarchyFolder == null ? "null" : "\""+hierarchyFolder.name+"\"") + "\nTotal Count: " + hierarchyFolders.Count, hierarchyFolder);
			#endif
		}

		private void Initialize()
		{
			#if DEV_MODE && DEBUG_INIT
			Debug.Log("HierarchyFolderManager.Initialize() with HierarchyFolder.Manager "+ (HierarchyFolder.Manager == this ? "==" : "!=") + " this");
			#endif

			var preferences = HierarchyFolderPreferences.Get();
			if(preferences == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Preferences null. Can't initialize yet.");
				#endif
				EditorApplication.delayCall += Initialize;
				return;
			}

			initialized = true;

			if(HierarchyFolder.Manager != this)
			{
				UnsubscribeFromEvents(preferences);
				return;
			}

			ResubscribeToEvents(preferences);
		}

		private void ResubscribeToEvents(HierarchyFolderPreferences preferences)
		{
			#if DEV_MODE && DEBUG_SUBSCRIBE_TO_EVENTS
			Debug.Log("HierarchyFolderManager.ResubscribeToEvents");
			#endif

			if(this.preferences != preferences)
            {
				this.preferences = preferences;
				UnsubscribeFromEvents(this.preferences);
			}
			else
			{
				UnsubscribeFromEvents(preferences);
			}
			
			preferences.onPreferencesChanged += OnPreferencesChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui += OnSceneGUI;
			#else
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			#endif

			if(!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				EditorApplication.hierarchyChanged += OnHierarchyChangedInEditMode;
			}
			else
			{
				if(preferences.playModeBehaviour == StrippingType.FlattenHierarchy)
				{
					EditorApplication.hierarchyChanged += OnHierarchyChangedInPlayModeFlattened;
				}
				else
				{
					EditorApplication.hierarchyChanged += OnHierarchyChangedInPlayModeGrouped;
				}
			}
		}

		private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
			#if DEV_MODE && DEBUG_PLAY_MODE_CHANGED
			Debug.Log("HierarchyFolderManager.OnPlayModeStateChanged");
			#endif

			initialized = false;
			UnsubscribeFromEvents(preferences);
        }

        private void UnsubscribeFromEvents(HierarchyFolderPreferences preferences)
		{
			#if DEV_MODE && DEBUG_SUBSCRIBE_TO_EVENTS
			Debug.Log("HierarchyFolderManager.UnsubscribeToEvents");
			#endif

			preferences.onPreferencesChanged -= OnPreferencesChanged;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

			EditorApplication.hierarchyChanged -= OnHierarchyChangedInEditMode;
			EditorApplication.hierarchyChanged -= OnHierarchyChangedInPlayModeFlattened;
			EditorApplication.hierarchyChanged -= OnHierarchyChangedInPlayModeGrouped;

			#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= OnSceneGUI;
			#else
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			#endif
		}

		private void OnPreferencesChanged(HierarchyFolderPreferences preferences)
        {
			ResubscribeToEvents(preferences);
        }

		private void OnSceneGUI(SceneView sceneView)
		{
			var eventType = Event.current.type;

			// Reset hierarchy folder transform states immediately on mouse up in case user
			// just finished moving a hierarchy folder transform in the hierarchy view.
			switch(eventType)
			{
				case EventType.MouseDown:
				#if DISABLE_TRANSFORM_GIZMOS
					if(Tools.current != Tool.Move && Tools.current != Tool.Transform && Tools.current != Tool.Rotate && Tools.current != Tool.Scale && Tools.current != Tool.Rect)
					{
						break;
					}
					var selected = Selection.gameObjects;
					for(int n = selected.Length - 1; n >= 0; n--)
					{
						if(selected[n].IsHierarchyFolder())
						{
							editorToolTempDisabled = Tools.current;
							Tools.current = Tool.View;
							break;
						}
					}
					break;
				#endif
				case EventType.MouseUp:
				case EventType.MouseLeaveWindow:
				#if DISABLE_TRANSFORM_GIZMOS
					if(editorToolTempDisabled != Tool.None)
					{
						Tools.current = editorToolTempDisabled;
						editorToolTempDisabled = Tool.None;
					}
					break;
				#endif
				case EventType.DragExited:
				case EventType.DragPerform:
				case EventType.Ignore:
				case EventType.Used:
				case EventType.KeyDown:
				case EventType.KeyUp:
					#if DEV_MODE && DEBUG_ON_SCENE_GUI
					Debug.Log("HierarchyFolderManager.OnSceneGUI(" + Event.current.type + ")");
					#endif
					break;
				default:
					return;
			}

			hierarchyFolders.RemoveAll(isNullHierarchyFolder);

			for(int n = hierarchyFolders.Count - 1; n >= 0; n--)
			{
				HierarchyFolderUtility.ResetTransformStateWithoutAffectingChildren(hierarchyFolders[n].transform);
			}
		}

		private void OnHierarchyChangedInEditMode()
		{
			hierarchyFolders.RemoveAll(isNullHierarchyFolder);
			hierarchyFolders.Sort(hierarchyFolderDepthComparer);

			#if DEV_MODE && DEBUG_HIERARCHY_CHANGED
			Debug.Log("OnHierarchyChangedInEditMode with hierarchyFolders="+ hierarchyFolders.Count);
			#endif

			var preferences = HierarchyFolderPreferences.Get();

			bool prefabsNotAllowed = preferences.foldersInPrefabs == HierachyFoldersInPrefabs.NotAllowed;

			for(int n = 0, count = hierarchyFolders.Count - 1; n < count; n++)
			{
				var hierarchyFolder = hierarchyFolders[n];

				// Only process scene objects, not prefabs.
				if(!hierarchyFolder.gameObject.scene.IsValid())
                {
					continue;
                }

				if(prefabsNotAllowed && hierarchyFolder.gameObject.IsConnectedPrefabInstance())
				{
					OnHierarchyFolderDetectedOnAPrefabAndNotAllowed(hierarchyFolder, true);
					count = hierarchyFolders.Count;
					continue;
				}

				// If has RectTransform child convert Transform component into RectTransform 
				// to avoid child RectTransform values being affected by the parent hierarchy folders.
				// For performance reasons only first child is checked.
				var transform = hierarchyFolder.transform;
				if(transform.GetFirstChild(true) is RectTransform && !(transform is RectTransform))
				{
					#if DEV_MODE
					Debug.LogWarning("Converting Hierarchy Folder " + hierarchyFolder.name + " Transform into RectTransform because it had a RectTransform child.", hierarchyFolder);
					#endif

					HierarchyFolderUtility.ForceResetTransformStateWithoutAffectingChildren(transform, true);
				}

				ApplyNamingPattern(hierarchyFolder);

				OnHierarchyChangedShared(hierarchyFolder, preferences);
			}
		}

		private void OnHierarchyFolderDetectedOnAPrefabAndNotAllowed(HierarchyFolder hierarchyFolder, bool isInstance)
		{
			// Prevent warning message being logged multiple times.
			if(hierarchyFolder == null || !destroying.Add(hierarchyFolder))
			{
				return;
			}

			if(isInstance)
			{
				Debug.LogWarning(HierarchyFolderMessages.PrefabInstanceNotAllowed, hierarchyFolder.gameObject);
			}
			else
			{
				Debug.LogWarning(HierarchyFolderMessages.PrefabNotAllowed, hierarchyFolder.gameObject);
			}

			TurnIntoNormalGameObject(hierarchyFolder);
		}

		private void TurnIntoNormalGameObject(HierarchyFolder hierarchyFolder)
		{
			// Can help avoid NullReferenceExceptions via hierarchyChanged callback
			// by adding a delay between the unsubscribing and the destroying of the HierarchyFolder component
			EditorApplication.delayCall += ()=>UnmakeHierarchyFolder(hierarchyFolder);
		}

		private void UnmakeHierarchyFolder([CanBeNull]HierarchyFolder hierarchyFolder)
		{
			// If this hierarchy folder has already been destroyed we should abort.
			if(hierarchyFolder == null)
			{
				return;
			}

			destroying.Remove(hierarchyFolder);

			HierarchyFolderUtility.UnmakeHierarchyFolder(hierarchyFolder.gameObject, hierarchyFolder);
		}

		private void OnHierarchyChangedInPlayModeFlattened()
		{
			hierarchyFolders.RemoveAll(isNullHierarchyFolder);
			hierarchyFolders.Sort(hierarchyFolderDepthComparer);

			var preferences = HierarchyFolderPreferences.Get();

			for(int n = 0, count = hierarchyFolders.Count - 1; n < count; n++)
			{
				var hierarchyFolder = hierarchyFolders[n];

				// Only process scene objects, not prefabs.
				if(!hierarchyFolder.gameObject.scene.IsValid())
                {
					continue;
                }

				OnHierarchyChangedShared(hierarchyFolder, preferences);

				var transform = hierarchyFolder.transform;

				#if DEV_MODE
				if(transform.childCount > 0)
				{
					if(HierarchyFolderUtility.NowStripping) { Debug.LogWarning(hierarchyFolder.name + " child count is "+ transform.childCount+" but won't flatten because HierarchyFolderUtility.NowStripping already true.", hierarchyFolder); }
					else { Debug.Log(hierarchyFolder.name + " child count " + transform.childCount+". Flattening now...", hierarchyFolder); }
				}
				#endif

				if(transform.childCount > 0 && !HierarchyFolderUtility.NowStripping)
				{
					// todo: should we keep track of flattened children in play mode? Or only for prefabs?
					// Would there be benefits to keeping track of them?
					// Could select them when hierarchy folder is double clicked in hierarchy view?
					// Could draw some sort of bounds around the children in the hierarchy?!!!
					int moveToIndex = HierarchyFolderUtility.GetLastChildIndexInFlatMode(transform.gameObject);
					for(int c = transform.childCount - 1; c >= 0; c--)
					{
						var child = transform.GetChild(c);
						child.SetParent(null, true);
						child.SetSiblingIndex(moveToIndex);
					}
				}
			}

			hierarchyFolders.RemoveAll(isNullHierarchyFolder);
		}

		private void OnHierarchyChangedInPlayModeGrouped()
		{
			hierarchyFolders.RemoveAll(isNullHierarchyFolder);
			hierarchyFolders.Sort(hierarchyFolderDepthComparer);

			var preferences = HierarchyFolderPreferences.Get();

			for(int n = 0, count = hierarchyFolders.Count - 1; n < count; n++)
			{
				var hierarchyFolder = hierarchyFolders[n];

				// Only process scene objects, not prefabs.
				if(!hierarchyFolder.gameObject.scene.IsValid())
                {
					continue;
                }

				OnHierarchyChangedShared(hierarchyFolder, preferences);
			}
		}

		internal void OnHierarchyChangedShared(HierarchyFolder hierarchyFolder, HierarchyFolderPreferences preferences)
		{
			if(HierarchyFolderUtility.HasSupernumeraryComponents(hierarchyFolder, preferences))
			{
				// Prevent warning message being logged multiple times.
				if(!destroying.Add(hierarchyFolder))
				{
					#if DEV_MODE && DEBUG_SUPERNUMERARY_COMPONENTS
					Debug.LogWarning("Hierarchy Folder \"" + hierarchyFolder.name + "\" contained extraneous components by already destroying...");
					#endif
					return;
				}

				Debug.LogWarning("Hierarchy Folder \"" + hierarchyFolder.name + "\" contained extraneous components.\nThis is not supported since Hierarchy Folders are stripped from builds. Converting into a normal GameObject now.\nIf you want to be able to add components of this type to Hierarchy Folders for their editor only functionality, you can whitelist the type in Edit > Preferences > Hierarchy Folders > Whitelisted Components.", hierarchyFolder.gameObject);

				TurnIntoNormalGameObject(hierarchyFolder);
			}

			HierarchyFolderUtility.ResetTransformStateWithoutAffectingChildren(hierarchyFolder.transform);
		}

		private void ApplyNamingPattern(HierarchyFolder hierarchyFolder)
		{
			var preferences = HierarchyFolderPreferences.Get();
			if(!preferences.enableNamingRules)
			{
				return;
			}

			var gameObject = hierarchyFolder.gameObject;
			string setName = gameObject.name;
			bool possiblyChanged = false;

			if(preferences.forceNamesUpperCase)
			{
				setName = setName.ToUpper();
				possiblyChanged = true;
			}

			string prefix = preferences.namePrefix;
			if(!setName.StartsWith(prefix, StringComparison.Ordinal))
			{
				possiblyChanged = true;

				if(setName.StartsWith(preferences.previousNamePrefix, StringComparison.Ordinal))
				{
					setName = setName.Substring(preferences.previousNamePrefix.Length);
				}

				for(int c = prefix.Length - 1; c >= 0 && !setName.StartsWith(prefix, StringComparison.Ordinal); c--)
				{
					setName = prefix[c] + setName;
				}
			}

			string suffix = preferences.nameSuffix;
			if(!setName.EndsWith(suffix, StringComparison.Ordinal))
			{
				possiblyChanged = true;

				// Handle situation where a hierarchy folder has been duplicated and a string like "(1)"
				// has been added to the end of the name.
				if(setName.EndsWith(")", StringComparison.Ordinal))
				{
					int openParenthesis = setName.LastIndexOf(" (", StringComparison.Ordinal);
					if(openParenthesis != -1)
					{
						string ending = setName.Substring(openParenthesis);
						if(ending.Length <= 5 && setName.EndsWith(suffix + ending, StringComparison.Ordinal))
						{
							int from = openParenthesis + 2;
							int to = setName.Length - 1;
							string nthString = setName.Substring(from, to - from);
							int nthInt;
							if(int.TryParse(nthString, out nthInt))
							{
								setName = setName.Substring(0, openParenthesis - suffix.Length) + suffix;
							}
						}
					}
				}

				if(setName.EndsWith(preferences.previousNameSuffix, StringComparison.Ordinal))
				{
					setName = setName.Substring(0, setName.Length - preferences.previousNameSuffix.Length);
				}

				for(int c = 0, count = suffix.Length; c < count && !setName.EndsWith(suffix, StringComparison.Ordinal); c++)
				{
					setName += suffix[c];
				}
			}

			if(possiblyChanged && !string.Equals(setName, gameObject.name))
			{
				gameObject.name = setName;
			}
		}

		private static bool IsNull(HierarchyFolder hierarchyFolder)
		{
			return hierarchyFolder == null;
		}

        private static int CompareHierarchyDepth([NotNull] HierarchyFolder x, [NotNull] HierarchyFolder y)
		{
			return GetDepth(x).CompareTo(GetDepth(y));
		}

		private static int GetDepth([NotNull] HierarchyFolder hierarchyFolder)
		{
			int depth = 0;
			for(var parent = hierarchyFolder.transform.parent; parent != null; parent = parent.parent)
			{
				depth++;
			}
			return depth;
		}

		~HierarchyFolderManager()
		{
			EditorApplication.delayCall -= Initialize;
			if(preferences != null)
            {
				UnsubscribeFromEvents(preferences);
            }
			else
            {
				EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
				EditorApplication.hierarchyChanged -= OnHierarchyChangedInEditMode;
				EditorApplication.hierarchyChanged -= OnHierarchyChangedInPlayModeFlattened;
				EditorApplication.hierarchyChanged -= OnHierarchyChangedInPlayModeGrouped;
			}
		}
    }
}
#endif