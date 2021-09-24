#if POWER_INSPECTOR
using UnityEditor;

namespace Sisus.HierarchyFolders
{
	[InitializeOnLoad]
	public static class HierarchyFolderDrawerEnabler
	{
		static HierarchyFolderDrawerEnabler()
		{
			HandleEnableHierarchyFolderDrawer();
		}

		private static void HandleEnableHierarchyFolderDrawer()
		{
			if(BuildPipeline.isBuildingPlayer)
			{
				return;
			}

			if(EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				EditorApplication.delayCall += HandleEnableHierarchyFolderDrawer;
				return;
			}

			var drawerPath = FileUtility.FindScriptAssetByName("HierarchyFolderDrawer");
			if(drawerPath.Length == 0)
			{
				var installerPath = FileUtility.FindAssetByNameAndExtension("PowerInspectorIntegrationPackage", ".unitypackage");
				if(installerPath.Length > 0)
				{
					#if DEV_MODE
					UnityEngine.Debug.Log("Installing Power Inspector integration package @ "+installerPath); 
					#endif
					AssetDatabase.ImportPackage(installerPath, false);
				}
				else { UnityEngine.Debug.LogWarning("Failed to find \"PowerInspectorIntegrationPackage.unitypackage\"."); }
			}
		}
		
	}
}
#endif