using System;
using UnityEngine;
using JetBrains.Annotations;
using System.Collections.Generic;

namespace Sisus.HierarchyFolders
{
	[Serializable]
	public class Icon
	{
		[CanBeNull]
		public Texture closed;

		[CanBeNull]
		public Texture open;

		public override bool Equals(object obj)
		{
			var other = obj as Icon;
			if(ReferenceEquals(other, null))
			{
				return false;
			}
			return closed == other.closed && open == other.open;
		}

		public override int GetHashCode()
		{
			var hashCode = -2017190570;
			hashCode = hashCode * -1521134295 + EqualityComparer<Texture>.Default.GetHashCode(closed);
			hashCode = hashCode * -1521134295 + EqualityComparer<Texture>.Default.GetHashCode(open);
			return hashCode;
		}
	}
}