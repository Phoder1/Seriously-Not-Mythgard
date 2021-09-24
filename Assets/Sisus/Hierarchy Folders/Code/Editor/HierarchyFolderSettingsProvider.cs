using System;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.HierarchyFolders
{
	/// <summary>
	/// Handles drawing preferences view for Hierarchy Folders in window that can be opened via Edit > Preferences.
	/// </summary>
	public static class HierarchyFolderSettingsProvider
	{
		private const int ScriptExecutionOrder = -10000;

		private static Editor defaultEditor;

		private static bool setupDone;
		private static bool unappliedChanges;
		private static bool defaultState;

		private static readonly Color applyColor = new Color32(60, 226, 65, 255);
		private static readonly Color discardColor = new Color32(229, 99, 99, 255);

		private static Vector2 scrollPosition = Vector2.zero;

		#if UNITY_2019_1_OR_NEWER
		[SettingsProvider, UsedImplicitly]
		private static SettingsProvider CreateHierarchyFolderDrawer()
		{
			var provider = new SettingsProvider("Preferences/Hierarchy Folders", SettingsScope.User)
			{
				label = "Hierarchy Folders",
				guiHandler = DrawPreferencesGUI,

				// Populate the search keywords to enable smart search filtering and label highlighting
				keywords = new System.Collections.Generic.HashSet<string>(new[]
				{
					"defaultName",
					"namePrefix",
					"nameSuffix",
					"removeWhenEnterPlayMode",
					"removeWhenMakingBuild",
					"infoBoxText"
				})
			};

			return provider;
		}
		
		private static void DrawPreferencesGUI(string searchContext)
		{
			DrawPreferencesGUI();
		}
		#endif

		#if !UNITY_2019_1_OR_NEWER
		[PreferenceItem("Hierarchy Folders"), UsedImplicitly]
		#endif
		private static void DrawPreferencesGUI()
		{
			var labelWidthWas = EditorGUIUtility.labelWidth;
			var indentLevelWas = EditorGUI.indentLevel;
			EditorGUIUtility.labelWidth = 255f;
			EditorGUI.indentLevel = 1;

			var preferences = HierarchyFolderPreferences.Get();

			if(!setupDone)
			{
				setupDone = true;
				defaultState = preferences.HasDefaultState();
			}

			Editor.CreateCachedEditor(preferences, null, ref defaultEditor);

			string previousNamePrefix = preferences.namePrefix;
			string previousNameSuffix = preferences.nameSuffix;

			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			EditorGUI.BeginChangeCheck();

			// hide the script field
			GUILayout.Space(-20f);

			defaultEditor.OnInspectorGUI();

			if(EditorGUI.EndChangeCheck())
			{
				if(!string.Equals(previousNamePrefix, preferences.namePrefix, StringComparison.OrdinalIgnoreCase))
				{
					preferences.previousNamePrefix = previousNamePrefix;
				}

				if(!string.Equals(previousNameSuffix, preferences.nameSuffix, StringComparison.OrdinalIgnoreCase))
				{
					preferences.previousNameSuffix = previousNameSuffix;
				}

				unappliedChanges = preferences.HasUnappliedChanges();
				defaultState = preferences.HasDefaultState();
			}

			GUILayout.EndScrollView();

			GUILayout.Space(15f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(15f);

				const float buttonHeight = 25f;

				if(unappliedChanges)
				{
					var guiColorWas = GUI.color;
					GUI.color = applyColor;
				
					if(GUILayout.Button("Apply Changes", GUILayout.Height(buttonHeight)))
					{
						preferences.SaveState();
						unappliedChanges = false;
					}

					GUILayout.Space(15f);

					GUI.color = discardColor;

					if(GUILayout.Button("Discard Changes", GUILayout.Height(buttonHeight)))
					{
						preferences.DiscardChanges();
						unappliedChanges = false;
					}

					GUI.color = guiColorWas;
				}

				if(!defaultState)
				{
					if(unappliedChanges)
					{
						GUILayout.Space(15f);
					}

					if(GUILayout.Button("Reset To Defaults", GUILayout.Height(buttonHeight)))
					{
						preferences.ResetToDefaults();
						unappliedChanges = false;
					}
				}

				GUILayout.Space(15f);
			}
			GUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = labelWidthWas;
			EditorGUI.indentLevel = indentLevelWas;
		}
	}
}