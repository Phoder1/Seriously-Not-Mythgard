using UnityEngine;
using System.Collections.Generic;

namespace Sisus.HierarchyFolders
{
	public class SortTransformsByHierarchyOrder : IComparer<Transform>
	{
		int IComparer<Transform>.Compare(Transform a, Transform b)
		{
			return HierarchyOrderUtility.CompareHierarchyOrder(a, b);
		}
	}
}