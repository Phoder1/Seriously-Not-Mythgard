using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus.HierarchyFolders
{
	public static class HierarchyOrderUtility
	{
		private readonly static Stack<Transform> aParents = new Stack<Transform>(3);
		private readonly static Stack<Transform> bParents = new Stack<Transform>(3);

		public static int CompareHierarchyOrder([CanBeNull]Transform a, [CanBeNull]Transform b)
		{
			if(a == b)
			{
				return 0;
			}
			if(a == null)
			{
				return -1;
			}
			if(b == null)
			{
				return 1;
			}

			for(var current = a; current != null; current = current.parent)
			{
				aParents.Push(current);
			}
			for(var current = b; current != null; current = current.parent)
			{
				bParents.Push(current);
			}

			int aCount = aParents.Count;
			int bCount = bParents.Count;
			int minCount = Mathf.Min(aCount, bCount); 
			for(int n = minCount; n > 0; n--)
			{
				var aParent = aParents.Pop();
				var bParent = bParents.Pop();
				if(aParent != bParent)
				{
					return aParent.GetSiblingIndex().CompareTo(bParent.GetSiblingIndex());
				}
			}

			return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
		}
	}
}