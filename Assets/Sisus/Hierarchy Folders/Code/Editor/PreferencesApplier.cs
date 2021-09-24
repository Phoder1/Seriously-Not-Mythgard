using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus.HierarchyFolders
{
	/// <summary>
	/// Applies preferences by enabling or disabling defines inside a script asset.
	/// </summary>
	public static class PreferencesApplier
	{
		/// <summary>
		/// This is initialized on load due to the usage of the InitializeOnLoad attribute.
		/// </summary>
		static PreferencesApplier()
		{
			EditorApplication.delayCall += ApplyPreferencesWhenAssetDatabaseReady;
		}

		private static void ApplyPreferencesWhenAssetDatabaseReady()
		{
			if(!ReadyToApplyPreferences())
			{
				EditorApplication.delayCall += ApplyPreferencesWhenAssetDatabaseReady;
				return;
			}

			bool scriptChanged = false;
			var preferences = HierarchyFolderPreferences.Get();

			ApplyPreferences(typeof(HierarchyFolderExtensions),
			new[] { "#define HIERARCHY_FOLDER_EXTENSIONS_IN_GLOBAL_NAMESPACE" },
			new[] { preferences.extensionMethodsInGlobalNamespace },
			ref scriptChanged);


			bool menuItemsEnabled = preferences.enableMenuItems;
			ApplyPreferences(typeof(HierarchyFolderMenuItems),
			new[] { "#define ENABLE_HIERARCHY_FOLDER_MENU_ITEMS" },
			new[] { menuItemsEnabled },
			ref scriptChanged);
			preferences.onPreferencesChanged += (changedPreferences) =>
			{
				if(changedPreferences.enableMenuItems != menuItemsEnabled)
				{
					var script = FindScriptFile(typeof(HierarchyFolderMenuItems));
					if(script != null)
					{
						AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(script));
					}
					#if DEV_MODE
					else { Debug.LogWarning("Could not find script asset " + typeof(HierarchyFolderMenuItems).Name + ".cs"); }
					#endif
				}
			};

			#if UNITY_2019_1_OR_NEWER
			bool toolbarIconEnabled = preferences.enableToolbarIcon;
			ApplyPreferences(typeof(HierarchyFolderEditorTool),
			new[] { "#define ENABLE_HIERARCHY_FOLDER_EDITOR_TOOL" },
			new[] { toolbarIconEnabled },
			ref scriptChanged);
			preferences.onPreferencesChanged += (changedPreferences)=>
			{
				if(changedPreferences.enableToolbarIcon != toolbarIconEnabled)
				{
					var script = FindScriptFile(typeof(HierarchyFolderEditorTool));
					if(script != null)
					{
						AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(script));
					}
					#if DEV_MODE
					else { Debug.LogWarning("Could not find script asset " + typeof(HierarchyFolderEditorTool).Name + ".cs"); }
					#endif
				}
			};
			#endif

			if(scriptChanged)
			{
				AssetDatabase.Refresh();
			}
		}

		/// <summary>
		/// Is it safe to call ApplyPreferences at this moment?
		/// </summary>
		/// <returns> True if can call ApplyPreferences. </returns>
		public static bool ReadyToApplyPreferences()
		{
			return !EditorApplication.isUpdating;
		}

		/// <summary>
		/// Apply preferences by enabling or disabling defines inside the script asset for the type.
		/// </summary>
		/// <param name="classType"> Type of class whose script asset should be modified. </param>
		/// <param name="defines"> List of defines to enable or disable. All entries should start with "#define ". </param>
		/// <param name="setEnabled"> List booleans specifying whether each define should be enabled or disabled. </param>
		public static void ApplyPreferences(Type classType, string[] defines, bool[] setEnabled, ref bool scriptChanged)
		{
			var scriptFile = FindScriptFile(classType);
			if(scriptFile == null)
			{
				Debug.LogError("PreferencesUtility.ApplyPreferences failed to find script asset " + classType.Name + ".cs");
				return;
			}
			
			var scriptText = scriptFile.text;
			
			int preferencesStart = scriptText.IndexOf("#region ApplyPreferences", StringComparison.Ordinal) + 24;
			if(preferencesStart == -1)
			{
				throw new Exception("#region ApplyPreferences missing from "+ classType.Name + ".cs");
			}
			int preferencesEnd = scriptText.IndexOf("#endregion", preferencesStart, StringComparison.Ordinal);
			if(preferencesEnd == -1)
			{
				throw new Exception("#endregion missing from "+ classType.Name + ".cs");
			}
			string beforePreferences = scriptText.Substring(0, preferencesStart);
			string afterPreferences = scriptText.Substring(preferencesEnd);
			string menuPreferences = scriptText.Substring(preferencesStart , preferencesEnd - preferencesStart);

			for(int n = defines.Length - 1; n >= 0; n--)
			{
				SetContextMenuItemEnabled(ref menuPreferences, setEnabled[n], defines[n], ref scriptChanged);
			}

			if(scriptChanged)
			{
				string localPath = AssetDatabase.GetAssetPath(scriptFile);
				string fullPath = LocalToFullPath(localPath);
				File.WriteAllText(fullPath, beforePreferences + menuPreferences + afterPreferences);
				EditorUtility.SetDirty(scriptFile);
			}
		}

		[CanBeNull]
		public static MonoScript FindScriptFile([NotNull]Type classType)
		{
			string name = classType.Name;

			if(classType.IsGenericType)
			{
				// Parse out generic type information from generic type name
				int i = name.IndexOf('`');
				if(i != -1)
				{
					name = name.Substring(0, i);
				}

				// Additionally, convert generic types to their generic type defitions.
				// E.g. List<string> to List<>.
				if(!classType.IsGenericTypeDefinition)
				{
					classType = classType.GetGenericTypeDefinition();
				}
			}

			var guids = AssetDatabase.FindAssets(name + " t:MonoScript");

			int count = guids.Length;
			if(count == 0)
			{
				return null;
			}

			MonoScript fallback = null;

			for(int n = count - 1; n >= 0; n--)
			{
				var guid = guids[n];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var filename = Path.GetFileNameWithoutExtension(path);
				if(string.Equals(filename, name, StringComparison.OrdinalIgnoreCase))
				{
					var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
					var scriptClassType = scriptAsset.GetClass();
					if(scriptClassType == classType)
					{
						return scriptAsset;
					}

					if(scriptClassType == null)
					{
						fallback = scriptAsset;
					}
					#if DEV_MODE
					else
					{
						Debug.LogWarning("FindScriptFile(" + classType.Name + ") ignoring file @ \"" + path + "\" because MonoScript.GetClass() result " + scriptClassType.Name + " did not match classType.");
					}
					#endif
				}
			}

			// Second pass: test files where filename is only a partial match for class name.
			// E.g. class Header could be defined in file HeaderAttribute.cs.
			if(count > 1)
			{
				for(int n = count - 1; n >= 0; n--)
				{
					var guid = guids[n];
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var filename = Path.GetFileNameWithoutExtension(path);
					if(!string.Equals(filename, name, StringComparison.OrdinalIgnoreCase))
					{
						var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
						var scriptClassType = scriptAsset.GetClass();
						if(scriptClassType == classType)
						{
							return scriptAsset;
						}

						if(fallback == null && scriptClassType == null) 
						{
							fallback = scriptAsset;
						}

						#if DEV_MODE
						Debug.LogWarning("FindScriptFile("+classType.Name+") second pass: ignoring file @ \""+path+"\" because MonoScript.GetClass() result "+ scriptClassType.Name +" did not match classType.");
						#endif
					}
				}
			}

			// If was unable to verify correct script class type using MonoScript.GetClass()
			// but there was a probable match whose GetClass() returned null (seems to happen
			// with all generic types), then return that.
			if(fallback != null)
			{
				#if DEV_MODE
				//Debug.LogWarning("FindScriptFile(" + classType.Name + ") returning fallback result @ \""+AssetDatabase.GetAssetPath(fallback)+"\".");
				#endif
				return fallback;
			}

			#if DEV_MODE
			Debug.LogWarning("FindScriptFile(" + classType.Name + ") - AssetDatabase.FindAssets(\"" + name + " t:MonoScript\") returned " + count + " results.");
			#endif

			return null;
		}

		private static void SetContextMenuItemEnabled(ref string scriptText, bool setEnabled, string define, ref bool changed)
		{
			int isDisabled = scriptText.IndexOf("//" + define, StringComparison.Ordinal);
			
			//if menu item should be set to enabled
			if(setEnabled)
			{
				// if menu item is currently disabled
				if(isDisabled != -1)
				{
					//enable menu item by removing comment characters from in front of define
					scriptText = scriptText.Substring(0, isDisabled) + scriptText.Substring(isDisabled + 2);
					changed = true;
				}
			}
			// if menu item is currently enabled
			else if(isDisabled == -1)
			{
				//disable menu item by commenting define out
				int i = scriptText.IndexOf(define, StringComparison.Ordinal);
				if(i != -1)
				{
					scriptText = scriptText.Substring(0, i) + "//" + scriptText.Substring(i);
					changed = true;
				}
				else
				{
					Debug.LogError("Failed to find \"" + define + "\".");
				}
			}
		}

		private static string LocalToFullPath(string localPath)
		{
			return Path.Combine(Application.dataPath, localPath.Substring(7)).Replace("/", "\\");
		}
	}
}