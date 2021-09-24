#if UNITY_EDITOR
namespace Sisus.HierarchyFolders
{
	public static class HierarchyFolderMessages
	{
		public const string PrefabInstanceNotAllowed = "Hierarchy Folders are not allowed inside prefab instances with current preferences. You need to unpack the prefab instance first.\nYou can also turn on prefab support in Preferences > Hierarchy Folders.";
		public const string PrefabNotAllowed = "Hierarchy Folders are not allowed inside prefabs with current preferences.\nYou can turn on prefab support in Preferences > Hierarchy Folders.";
	}
}
#endif