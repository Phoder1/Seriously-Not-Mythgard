#define HF_DISABLE_GAMEOBJECT_EDITOR

#if !HF_DISABLE_GAMEOBJECT_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using Sisus.HierarchyFolders.Prefabs;

namespace Sisus.HierarchyFolders
{
	[CustomEditor(typeof(GameObject)), CanEditMultipleObjects]
	internal class GameObjectInspector : Editor
	{
		private static readonly GUIContent hierarchyFolderInfoLabel = new GUIContent("This is a hierarchy folder and can be used for organizing objects in the hierarchy.\n\nWhen a build is being made all members will be moved up the parent chain and the folder itself will be removed.");
		private static readonly GUIContent prefabHierarchyFolderInfoLabel = new GUIContent("This is a hierarchy folder and can be used for organizing objects inside the prefab.\n\nWhen the prefab is instantiated at runtime all members of the hierarchy folder will be moved up the parent chain and the folder itself will be removed.");
		private static Texture folderIcon;
		private static bool staticSetupDone = false;
		private static Type defaultEditorType;
		private static Color backgroundColor;
		private static MethodInfo defaultOnHeaderGUI;

		private Editor defaultEditor;

		private static void StaticSetup()
		{
			staticSetupDone = true;
			defaultEditorType = typeof(Editor).Assembly.GetType("UnityEditor.GameObjectInspector", false, false);
			hierarchyFolderInfoLabel.text = HierarchyFolderPreferences.Get().infoBoxText;
			prefabHierarchyFolderInfoLabel.text = HierarchyFolderPreferences.Get().prefabInfoBoxText;

			var iconSizeWas = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(30f, 30f));
			folderIcon = EditorGUIUtility.IconContent("Folder Icon").image;
			EditorGUIUtility.SetIconSize(iconSizeWas);

			defaultOnHeaderGUI = defaultEditorType.GetMethod("OnHeaderGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			#if UNITY_2019_1_OR_NEWER
			backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.23529412f, 0.23529412f, 0.23529412f, 1f) : new Color(0.79607844f, 0.79607844f, 0.79607844f, 1f);
			#else
			backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.24313726f, 0.24313726f, 0.24313726f, 1f) : new Color(0.85490197f, 0.85490197f, 0.85490197f, 1f);
			#endif
		}

		protected override void OnHeaderGUI()
		{
			if(!staticSetupDone)
			{
				StaticSetup();
			}

			if(defaultEditor == null)
			{
				defaultEditor = CreateEditor(targets, defaultEditorType);
				if(defaultEditor == null)
				{
					return;
				}
			}

			var gameObject = targets.Length == 0 ? null : targets[0] as GameObject;
			bool isHierarchyFolder;
			string tagWas;
			int layerWas;
			if(gameObject != null)
			{
				tagWas = gameObject.tag;
				layerWas = gameObject.layer;
				isHierarchyFolder = gameObject.IsHierarchyFolder();
			}
			else
			{
				isHierarchyFolder = false;
				tagWas = null;
				layerWas = 0;
			}

			defaultOnHeaderGUI.Invoke(defaultEditor, null);

			if(!isHierarchyFolder || gameObject == null)
			{
				return;
			}

			#if POWER_INSPECTOR
			if(InspectorUtility.NowDrawingInspectorPart != InspectorPart.None)
			{
				return;
			}
			#endif

			if(!gameObject.CompareTag(tagWas))
			{
				if(EditorUtility.DisplayDialog("Change Tag", "Do you want to set tag to " + gameObject.tag + " for all child objects?", "Yes, change children", "Cancel"))
				{
					gameObject.SetTagForAllChildren(gameObject.tag);
				}
				gameObject.tag = "Untagged";
			}

			if(gameObject.layer != layerWas)
			{
				gameObject.layer = layerWas;
			}

			bool isHierarchyFolderPrefab = gameObject.IsPrefabAssetOrOpenInPrefabStage();
			var label = isHierarchyFolderPrefab ? prefabHierarchyFolderInfoLabel : hierarchyFolderInfoLabel;
			EditorGUILayout.LabelField(label, EditorStyles.helpBox);

			// Ensure the transform state is kept reset if users somehow manage to modify it for example through a custom transform editor.
			// The transform should have at least the NotEditable flag though, so this is unlikely to occur.
			for(int i = targets.Length - 1; i >= 0; i--)
			{
				gameObject = targets[i] as GameObject;
				if(gameObject == null)
				{
					continue;
				}

				var transform = gameObject.transform;
				bool transformIsHidden = (transform.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector;
				if(transformIsHidden)
				{
					continue;
				}

				bool transformNotEditable = (transform.hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable;
				if(transformNotEditable)
				{
					continue;
				}

				HierarchyFolderUtility.ResetTransformStateWithoutAffectingChildren(transform);
			}

			if(isHierarchyFolderPrefab)
			{
				return;
			}

			var labelRect = GUILayoutUtility.GetLastRect();

			var backgroundRect = labelRect;
			backgroundRect.x += 0f;
			backgroundRect.y = 5f;
			backgroundRect.width = 36f;
			backgroundRect.height = 36f;
			EditorGUI.DrawRect(backgroundRect, backgroundColor);

			var folderIconRect = labelRect;
			folderIconRect.x += 3f;
			folderIconRect.y = 5f;
			folderIconRect.width = 30f;
			folderIconRect.height = 30f;
			GUI.DrawTexture(folderIconRect, folderIcon);
		}

		public override void OnInspectorGUI() { }

		private void OnDestroy()
		{
			if(defaultEditor != null)
			{
				DestroyImmediate(defaultEditor);
				defaultEditor = null;
			}
		}
	}
}
#endif