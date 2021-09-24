#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using System.Diagnostics;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace Sisus.HierarchyFolders
{
    /// <summary>
    /// Contains user preferences for Hierarchy Folders and handles saving and loading them to EditorPrefs.
    /// </summary>
    public class HierarchyFolderPreferences : ScriptableObject, ISerializationCallbackReceiver
	{
		private const string EditorPrefsKey = "HierarchyFolderPreferences";

		private static HierarchyFolderPreferences instance;
		private static HierarchyFolderPreferences asset;

		public Action<HierarchyFolderPreferences> onPreferencesChanged;

		[Header("Naming")]
		public bool enableNamingRules = false;
		public string defaultName = "New Folder";
		[Tooltip("If true then default GameObject names will be replaced with HierarchyFolderPreferences.defaultName when the HierarchyFolder component is added to an existing GameObject.")]
		public bool autoNameOnAdd = true;
		public string namePrefix = "";
		public string nameSuffix = "";
		public bool forceNamesUpperCase = false;

		[Header("Build Stripping")]
		[Tooltip("If true then all hierarchy folders in scenes will be removed during build post processing.\n\nAny child GameObjects will be moved upwards the parent chain."), FormerlySerializedAs("removeFromBuild")]
		public bool removeFromScenes = true;
		[Tooltip(
		"Not Allowed: Hierarchy Folders are not allowed to exist inside prefabs or prefab instances at all.\n\n" +
		"Stripped At Runtime: Hierarchy Folders inside prefabs will be stripped at runtime during instantiation. This takes place before any other code on the prefabs executes.\n\n" +
		"Stripped At Build Time: Hierarchy Folders inside prefabs will be stripped at build time. Note that this can make the build process take longer because all prefabs in the project need to be checked. Additionally hierarchy folders at the root of a prefab can not be stripped at build time, so runtime stripping will still be utilized for them even when this setting is selected.\n\n" +
		"Not Stripped: Hierarchy Folders can be placed inside prefabs yet they will not be stripped at any point. Note that this will result in sub-optimal performance and is not generally recommended.")]
		public HierachyFoldersInPrefabs foldersInPrefabs = HierachyFoldersInPrefabs.NotAllowed;
		public bool warnWhenNotRemovedFromBuild = true;

		[Header("Play Mode Stripping")]
		[Tooltip("If true then hierarchy folders will be removed from loaded scenes when their Awake method is called.\n\nAny members of the HierarchyGroup will be moved upwards the parent chain.")]
		public StrippingType playModeBehaviour = StrippingType.None;
		[Tooltip("Entire Scene Immediate : All Hierarchy Folders are stripped at the very beginning the scene loading process.\n\nIndividuallyDuringAwake: Hierarchy Folders are stripped in the order that Awake is called for them.\n\nAll Hierarchy Folders are stripped for a scene once it has fully finished loading and all scene scene objects have been initialized. WARNING: This means that Awake and OnEnable methods will get called for scene objects before stripping takes place!\n\nIf you encounter ArgumentException: The scene is not loaded then switch to using a stripping method other than Entire Scene Immediate.")]
		public PlayModeStrippingMethod playModeStrippingMethod = PlayModeStrippingMethod.EntireSceneImmediate;

		[Header("Drawer")]
		[TextArea(2, 8)]
		public string infoBoxText = "This is a hierarchy folder and can be used for organizing objects in the hierarchy.\n\nWhen a build is being made all members will be moved up the parent chain and the folder itself will be removed.";
		[TextArea(2, 8)]
		public string prefabInfoBoxText = "This is a hierarchy folder and can be used for organizing objects inside the prefab.\n\nWhen the prefab is instantiated at runtime all members of the hierarchy folder will be moved up the parent chain and the folder itself will be removed.";

		[Header("Hierarchy View")]
		[Tooltip("Enable folder icons in the hierarchy view.")]
		public bool enableHierarchyIcons = true;
		public bool doubleClickSelectsChildrens = true;

		[Header("Editor Integration")]
		[Tooltip("Allow hierarchy folders to be set inactive?\n\nWhen false, the active control for hierarchy folders will be changed to control the active state for children of the hierarchy folders instead.\n\nWhen true, hierarchy folders themselves can be set inactive. Note that this will have no practical effect in builds, as inactive hierarchy folders will still be stripped from builds - unless you have disabled build stripping.")]
		public bool allowInactiveHierarchyFolders = false;

		[Tooltip("Specify whether components are allowed on hierarchy folders.\n\nNone: No components (besides the mandatory Transform or RectTransform) are allowed on hierarchy folders.\n\nAll: Components can be added to hierarchy folders without restrictions. Only components that enhance the editor workflow should be used, since hierarchy folders along with all their components are stripped from builds. Whitelisted Only: Only components specified in the 'Allowed Components' whitelist can be added to hierarchy folders.")]
		public AllowedComponents allowComponentsOnHierarchyFolders = AllowedComponents.None;
		[Tooltip("Whitelist of components that are allowed to be added to Hierarchy Folders.\n\nUse the name of the class with or without namespace.")]
		public string[] whitelistedComponents = new string[0];

		[NonSerialized]
		public HashSet<string> allowComponentsByNameOnHierarchyFolders = null;

		[NonSerialized]
		public HashSet<Type> allowComponentsByTypeOnHierarchyFolders = null;

		[SerializeField, HideInInspector]
		public bool askAboutAllowInactiveHierarchyFolders = true;

		[Tooltip("Enable menu items GameObject > Hierarchy Folder and GameObject > Hierarchy Folder Parent.")]
		public bool enableMenuItems = true;
		[Tooltip("Enable custom Hierarchy Folder tool in the toolbar.")]
		public bool enableToolbarIcon = true;

		[Header("Scripting")]
		[Tooltip("Should extension methods such as GameObject.IsHierarchyFolder and Transform.GetParent always be shown, or only when you add \"using Sisus.HierarchyFolders;\" at the top of your class?\n\nNOTE: you will also need an Assembly Definition File with a reference to Sisus.HierarchyFolders for the methods to show up.")]
		public bool extensionMethodsInGlobalNamespace = true;

		[Header("Icons")]
		public Icon modernLight = new Icon();
		public Icon modernDark = new Icon();
		public Icon classicLight = new Icon();
		public Icon classicDark = new Icon();
		public Icon prefabModernLight = new Icon();
		public Icon prefabModernDark = new Icon();
		public Icon prefabClassicLight = new Icon();
		public Icon prefabClassicDark = new Icon();
		public Icon prefabAdditionModernLight = new Icon();
		public Icon prefabAdditionModernDark = new Icon();
		public Icon prefabAdditionClassicLight = new Icon();
		public Icon prefabAdditionClassicDark = new Icon();
		public Icon gameObjectAdditionModernLight = new Icon();
		public Icon gameObjectAdditionModernDark = new Icon();
		public Icon gameObjectAdditionClassicLight = new Icon();
		public Icon gameObjectAdditionClassicDark = new Icon();
		public Icon prefabVariantModernLight = new Icon();
		public Icon prefabVariantModernDark = new Icon();
		public Icon prefabVariantClassicLight = new Icon();
		public Icon prefabVariantClassicDark = new Icon();
		public Icon prefabVariantAdditionModernLight = new Icon();
		public Icon prefabVariantAdditionModernDark = new Icon();
		public Icon prefabVariantAdditionClassicLight = new Icon();
		public Icon prefabVariantAdditionClassicDark = new Icon();

		[SerializeField, HideInInspector]
		public string previousNamePrefix = "";
		[SerializeField, HideInInspector]
		public string previousNameSuffix = "";

		#if !DEV_MODE
		[SerializeField, HideInInspector]
		#endif
		private bool defaultIconsSuccessfullyLoaded = false;

		public static bool FlattenHierarchy
		{
			get
			{
				return EditorApplication.isPlayingOrWillChangePlaymode && Get().playModeBehaviour == StrippingType.FlattenHierarchy;
			}
		}

		[NotNull]
		public static HierarchyFolderPreferences Get()
		{
			if(instance == null)
			{
				var asset = GetAsset();
				if(asset != null)
				{
					#if DEV_MODE && DEBUG_GET
					UnityEngine.Debug.Log("HierarchyFolderPreferences.Get - Instantiating asset...");
					#endif
					instance = Instantiate(asset);
				}
				else
				{
					#if DEV_MODE && DEBUG_GET
					UnityEngine.Debug.Log("HierarchyFolderPreferences.Get - Loading from EditorPrefs...");
					#endif
					instance = CreateInstance<HierarchyFolderPreferences>();
					instance.LoadStateFromEditorPrefs();
				}
			}
			return instance;
		}

		[CanBeNull]
		private static HierarchyFolderPreferences GetAsset()
		{
			if(asset == null)
			{
				var assetPath = GetProjectSettingsAssetPath(false);
				asset = AssetDatabase.LoadAssetAtPath<HierarchyFolderPreferences>(assetPath);
			}
			return asset;
		}

		[NotNull]
		public Icon Icon(HierarchyIconType iconType)
		{
			if(!defaultIconsSuccessfullyLoaded)
			{
				OnEnable();
			}

			Icon icon;
			switch(iconType)
			{
				case HierarchyIconType.Default:
				default:
					#if UNITY_2019_3_OR_NEWER
					icon =  EditorGUIUtility.isProSkin ? modernDark : modernLight;
					#else
					icon =  EditorGUIUtility.isProSkin ? classicDark : classicLight;
					#endif
					break;
				case HierarchyIconType.PrefabRoot:
					#if UNITY_2019_3_OR_NEWER
					icon =  EditorGUIUtility.isProSkin ? prefabModernDark : prefabModernLight;
					#else
					icon =  EditorGUIUtility.isProSkin ? prefabClassicDark : prefabClassicLight;
					#endif
					break;
				case HierarchyIconType.PrefabAddition:
					#if UNITY_2019_3_OR_NEWER
					icon =  EditorGUIUtility.isProSkin ? prefabAdditionModernDark : prefabAdditionModernLight;
					#else
					icon =  EditorGUIUtility.isProSkin ? prefabAdditionClassicDark : prefabAdditionClassicLight;
					#endif
					break;
				case HierarchyIconType.GameObjectAddition:
					#if UNITY_2019_3_OR_NEWER
					icon =  EditorGUIUtility.isProSkin ? gameObjectAdditionModernDark : gameObjectAdditionModernLight;
					#else
					icon =  EditorGUIUtility.isProSkin ? gameObjectAdditionClassicDark : gameObjectAdditionClassicLight;
					#endif
					break;
				case HierarchyIconType.PrefabVariantRoot:
					#if UNITY_2019_3_OR_NEWER
					icon =  EditorGUIUtility.isProSkin ? prefabVariantModernDark : prefabVariantModernLight;
					#else
					icon =  EditorGUIUtility.isProSkin ? prefabVariantClassicDark : prefabVariantClassicLight;
					#endif
					break;
				case HierarchyIconType.PrefabVariantAddition:
					#if UNITY_2019_3_OR_NEWER
					icon =  EditorGUIUtility.isProSkin ? prefabVariantAdditionModernDark : prefabVariantAdditionModernLight;
					#else
					icon =  EditorGUIUtility.isProSkin ? prefabVariantAdditionClassicDark : prefabVariantAdditionClassicLight;
					#endif
					break;				
			}

			if(icon.closed == null)
			{
				icon = GetDefaultIcon();
			}

			return icon;
		}

		[NotNull]
		public Icon GetDefaultIcon(int size = 16)
		{
			var iconSizeWas = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(size, size));
			var icon = new Icon();
			icon.open = EditorGUIUtility.IconContent("Folder Icon").image;
			icon.closed = icon.open;
			EditorGUIUtility.SetIconSize(iconSizeWas);
			return icon;
		}

		private void OnValidate()
        {
            allowComponentsByNameOnHierarchyFolders = new HashSet<string>(whitelistedComponents);
			allowComponentsByTypeOnHierarchyFolders = new HashSet<Type>();
			allowComponentsByTypeOnHierarchyFolders.Add(typeof(HierarchyFolder));

			if(whitelistedComponents.Length > 0 && allowComponentsOnHierarchyFolders == AllowedComponents.None)
            {
				allowComponentsOnHierarchyFolders = AllowedComponents.WhitelistedOnly;
			}
		}

        private void OnEnable()
		{
			if(this == null || defaultIconsSuccessfullyLoaded)
			{
				return;
			}

			if(prefabModernDark.closed != null)
            {
				defaultIconsSuccessfullyLoaded = true;
				return;
			}

			var defaultIconGuids = AssetDatabase.FindAssets("hierarchy-folder-");

			#if DEV_MODE
			UnityEngine.Debug.Log("HierarchyFolderPreferences.OnEnable with iconsLoaded="+ defaultIconsSuccessfullyLoaded + ", EditorApplication.isUpdating=" + EditorApplication.isUpdating + ", defaultIconGuids: " + defaultIconGuids.Length, this);
			#endif

			for(int n = defaultIconGuids.Length - 1; n >= 0; n--)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(defaultIconGuids[n]);
				switch(Path.GetFileNameWithoutExtension(assetPath))
				{
					case "hierarchy-folder-icon-modern-dark-closed":
						modernDark.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-icon-modern-dark-open":
						modernDark.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-icon-modern-light-closed":
						modernLight.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-icon-modern-light-open":
						modernLight.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-gameobject-addition-icon-modern-dark-closed":
						gameObjectAdditionModernDark.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-gameobject-addition-icon-modern-dark-open":
						gameObjectAdditionModernDark.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-gameobject-addition-icon-modern-light-closed":
						gameObjectAdditionModernLight.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-gameobject-addition-icon-modern-light-open":
						gameObjectAdditionModernLight.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-addition-icon-modern-dark-closed":
						prefabAdditionModernDark.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-addition-icon-modern-dark-open":
						prefabAdditionModernDark.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-addition-icon-modern-light-closed":
						prefabAdditionModernLight.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-addition-icon-modern-light-open":
						prefabAdditionModernLight.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-icon-modern-dark-closed":
						prefabModernDark.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-icon-modern-dark-open":
						prefabModernDark.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-icon-modern-light-closed":
						prefabModernLight.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-icon-modern-light-open":
						prefabModernLight.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-addition-icon-modern-dark-closed":
						prefabVariantAdditionModernDark.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-addition-icon-modern-dark-open":
						prefabVariantAdditionModernDark.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-addition-icon-modern-light-closed":
						prefabVariantAdditionModernLight.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-addition-icon-modern-light-open":
						prefabVariantAdditionModernLight.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-icon-modern-dark-closed":
						prefabVariantModernDark.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-icon-modern-dark-open":
						prefabVariantModernDark.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-icon-modern-light-closed":
						prefabVariantModernLight.closed = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
					case "hierarchy-folder-prefab-variant-icon-modern-light-open":
						prefabVariantModernLight.open = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
						break;
				}
			}

			if(modernDark.closed == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("HierarchyFolderPreferences.OnEnable - loading default icons failed with EditorApplication.isUpdating=" + EditorApplication.isUpdating+ ", defaultIconGuids:" + defaultIconGuids.Length + ". Retrying after a delay...");
				#endif

				EditorApplication.delayCall += OnEnable;
				return;
			}

			defaultIconsSuccessfullyLoaded = true;

			EditorUtility.SetDirty(this);

			if(onPreferencesChanged != null)
			{
				onPreferencesChanged(this);
			}
		}

		public void ResetToDefaults()
		{
			var newInstance = CreateInstance<HierarchyFolderPreferences>();
			string serializedState = EditorJsonUtility.ToJson(newInstance);
			EditorJsonUtility.FromJsonOverwrite(serializedState, this);

			ClearSavedState();

			if(onPreferencesChanged != null)
			{
				onPreferencesChanged(this);
			}
		}

		public bool HasUnappliedChanges()
		{
			var asset = GetAsset();
			if(asset == null)
			{
				if(EditorPrefs.HasKey(EditorPrefsKey))
				{
					string defaultState = EditorPrefs.GetString(EditorPrefsKey);
					return !string.Equals(defaultState, EditorJsonUtility.ToJson(this), StringComparison.Ordinal);
				}
				return !HasDefaultState();
			}

			string assetState = EditorJsonUtility.ToJson(asset);
			string nameWas = name;
			name = asset.name;
			string currentState = EditorJsonUtility.ToJson(this);
			name = nameWas;
			return !string.Equals(assetState, currentState, StringComparison.Ordinal);
		}

		public bool HasDefaultState()
		{
			var newInstance = CreateInstance<HierarchyFolderPreferences>();
			
			string nameWas = name;
			name = newInstance.name;

			string defaultState = EditorJsonUtility.ToJson(newInstance);
			
			DestroyImmediate(newInstance, false);

			string currentState = EditorJsonUtility.ToJson(this);
			name = nameWas;

			return string.Equals(defaultState, currentState, StringComparison.Ordinal);
		}

		public void SaveState()
		{
			if(IsDefaultState())
			{
				ClearSavedState();
			}
			else
			{
				if(forceNamesUpperCase)
				{
					defaultName = defaultName.ToUpper();
				}

				if(!defaultName.StartsWith(namePrefix, StringComparison.Ordinal))
				{
					for(int c = namePrefix.Length - 1; c >= 0 && !defaultName.StartsWith(namePrefix, StringComparison.Ordinal); c--)
					{
						defaultName = namePrefix[c] + defaultName;
					}
				}

				if(!defaultName.EndsWith(nameSuffix, StringComparison.Ordinal))
				{
					for(int c = 0, count = nameSuffix.Length; c < count && !defaultName.EndsWith(nameSuffix, StringComparison.Ordinal); c++)
					{
						defaultName += nameSuffix[c];
					}
				}

				string serializedState = EditorJsonUtility.ToJson(this);
				EditorPrefs.SetString(EditorPrefsKey, serializedState);

				var assetPath = GetProjectSettingsAssetPath(true);

				asset = null;
				instance = null;
				AssetDatabase.CreateAsset(Instantiate(this), assetPath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			if(onPreferencesChanged != null)
			{
				onPreferencesChanged(this);
			}
		}

		public void DiscardChanges()
		{
			var asset = GetAsset();
			if(asset != null)
			{
				string assetState = EditorJsonUtility.ToJson(asset);
				EditorJsonUtility.FromJsonOverwrite(assetState, this);
			}
			else
			{
				var newInstance = CreateInstance<HierarchyFolderPreferences>();
				string defaultState = EditorJsonUtility.ToJson(newInstance);
				DestroyImmediate(newInstance, false);
				EditorJsonUtility.FromJsonOverwrite(defaultState, this);
				LoadStateFromEditorPrefs();
			}
		}

		public void LoadStateFromEditorPrefs()
		{
			#if DEV_MODE
			UnityEngine.Debug.Log("LoadStateFromEditorPrefs with HasKey="+ EditorPrefs.HasKey(EditorPrefsKey));
			#endif

			if(!EditorPrefs.HasKey(EditorPrefsKey))
			{
				return;
			}

			string serializedState = EditorPrefs.GetString(EditorPrefsKey);
			EditorJsonUtility.FromJsonOverwrite(serializedState, this);
			OnEnable();
		}

		public bool IsDefaultState()
		{
			var newInstance = CreateInstance<HierarchyFolderPreferences>();

			var fields = GetType().GetFields();
			foreach(var field in fields)
			{
				if(string.Equals(field.Name, "onPreferencesChanged"))
				{
					continue;
				}

				var currentValue = field.GetValue(this);
				var defaultValue = field.GetValue(newInstance);
				if(currentValue == null)
				{
					if(defaultValue == null)
					{
						return false;
					}
				}
				else if(!currentValue.Equals(defaultValue))
				{
					return false;
				}
			}
			return true;
		}

		public static void ClearSavedState()
		{
			#if DEV_MODE
			UnityEngine.Debug.Log("ClearSavedState");
			#endif

			var assetPath = GetProjectSettingsAssetPath(false);
			AssetDatabase.DeleteAsset(assetPath);
			EditorPrefs.DeleteKey(EditorPrefsKey);
		}

		/// <summary>
		/// Gets local asset path to project settings
		/// </summary>
		/// <returns> "Assets/Sisus/Hierarchy Folders/Project Settings/HierarchyFoldersSettings.asset" </returns>
		private static string GetProjectSettingsAssetPath(bool createFolderIfMissing)
		{
			var path = new StackTrace(true).GetFrame(0).GetFileName();
			path = Path.GetDirectoryName(path);
			path = Path.GetDirectoryName(path);
			path = Path.Combine(path, "Project Settings");

			if(createFolderIfMissing && !Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			path = Path.Combine(path, "HierarchyFoldersSettings.asset");

			path = "Assets" + path.Substring(Application.dataPath.Length);

			return path;
		}

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
			allowComponentsByNameOnHierarchyFolders = new HashSet<string>(whitelistedComponents);
			allowComponentsByTypeOnHierarchyFolders = new HashSet<Type>();
			allowComponentsByTypeOnHierarchyFolders.Add(typeof(HierarchyFolder));
        }
    }
}
#endif