#if UNITY_EDITOR
namespace Sisus.HierarchyFolders
{
    /// <summary>
    /// Specifies which components are allowed to exist on hierarchy folders.
    /// </summary>
    public enum AllowedComponents
    {
		None = 0,
		All = 1,
		WhitelistedOnly = 2
    }
}
#endif