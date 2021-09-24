// PreferencesApplier will make changes to this region based on preferences
#region ApplyPreferences
#define ENABLE_HIERARCHY_FOLDER_EDITOR_TOOL
#endregion

#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Sisus.HierarchyFolders
{
	#if ENABLE_HIERARCHY_FOLDER_EDITOR_TOOL
	[EditorTool("Hierarchy Folder")]
	#endif
	public class HierarchyFolderEditorTool : EditorTool
	{
		private GUIContent icon;

		public override GUIContent toolbarIcon
		{
			get { return icon; }
		}

		public override bool IsAvailable()
		{
			return true;
		}

		private void OnEnable()
		{
			icon = new GUIContent(HierarchyFolderPreferences.Get().GetDefaultIcon(18).closed, "Create Hierarchy Folder");
			EditorApplication.update += Update;
		}

		private void Update()
		{
			#if UNITY_2020_2_OR_NEWER
			if(ToolManager.IsActiveTool(this))
			#else
			if(EditorTools.activeToolType == typeof(HierarchyFolderEditorTool))
			#endif
			{
				OnActivate();
			}
		}

		// Called when an EditorTool is made the active tool.
		private void OnActivate()
		{
			RestorePreviousTool();

			if(!HierarchyFolderMenuItems.CreateHierarchyFolder())
            {
				return;
            }

			var window = HierarchyWindowUtility.GetHierarchyWindow();
			if(window != null)
			{
				window.Focus();
				EditorApplication.delayCall += StartRenamingCreatedHierarchyFolder;
			}
		}

		private void StartRenamingCreatedHierarchyFolder()
		{
			var window = HierarchyWindowUtility.GetHierarchyWindow();
			if(EditorWindow.focusedWindow != window)
			{
				window.Focus();
				EditorApplication.delayCall += StartRenamingCreatedHierarchyFolder;
				return;
			}

			var sendEvent = Event.KeyboardEvent("F2");
			Event.current = sendEvent;
			window.SendEvent(sendEvent);

			EditorApplication.delayCall += EnsureRenamingCreatedHierarchyFolder;
		}

		private void EnsureRenamingCreatedHierarchyFolder()
		{
			var window = HierarchyWindowUtility.GetHierarchyWindow();
			if(EditorWindow.focusedWindow != window)
			{
				window.Focus();
				StartRenamingCreatedHierarchyFolder();
				return;
			}
		}

		private void RestorePreviousTool()
		{
			#if UNITY_2020_2_OR_NEWER
			ToolManager.RestorePreviousTool();
			#else
			EditorTools.RestorePreviousTool();
			#endif
		}
	}
}
#endif