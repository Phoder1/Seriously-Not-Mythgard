namespace Sisus.HierarchyFolders
{
	/// <summary>
	/// Specifies how Hierarchy Folders inside prefabs and prefabs instances should be handled.
	/// </summary>
	public enum HierachyFoldersInPrefabs
	{
		NotAllowed = 0,
		StrippedAtRuntime = 1,
		StrippedAtBuildTime = 2,
		NotStripped = 3
	}
}