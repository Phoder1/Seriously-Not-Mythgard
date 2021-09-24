using UnityEditor;

namespace Sisus.HierarchyFolders
{
	[CustomEditor(typeof(HierarchyFolder)), CanEditMultipleObjects]
	internal class HierarchyFolderInspector : Editor
	{
		public override void OnInspectorGUI() { }
	}
}