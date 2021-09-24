using JetBrains.Annotations;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor;

namespace Sisus.HierarchyFolders
{
	public static class HierarchyFolderBuildSceneRemover
	{
		private static bool warnedAboutRemoveFromBuildDisabled;

		[PostProcessScene(0), UsedImplicitly]
		private static void OnPostProcessScene()
		{
			// This will also get called when entering Playmode, when SceneManager.LoadScene is called,
			// but we only want to do stripping just after building the Scene.
			if(Application.isPlaying)
			{
				return;
			}

			var preferences = HierarchyFolderPreferences.Get();
			if(preferences == null)
			{
				Debug.LogWarning("Failed to find Hierarchy Folder Preferences asset; will not strip hierarchy folders from build.");
				return;
			}

			if(!preferences.removeFromScenes)
			{
				if(!preferences.warnWhenNotRemovedFromBuild || warnedAboutRemoveFromBuildDisabled)
				{
					return;
				}

				warnedAboutRemoveFromBuildDisabled = true;
				if(EditorUtility.DisplayDialog("Warning: Hierarchy Folder Stripping Disabled", "This is a reminder that you have disabled stripping of hierarchy folders from builds. This will result in suboptimal performance and is not recommended when making a release build.", "Continue Anyway", "Enable Stripping"))
				{
					return;
				}
			}

			HierarchyFolderUtility.ApplyStrippingTypeToAllLoadedScenes(StrippingType.FlattenHierarchyAndRemoveGameObject);
		}
	}
}