using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus.HierarchyFolders.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="List{T}"/>.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Inserts an element into the sorted list  at the right index
        /// to keep the contents sorted.
        /// </summary>
        /// <typeparam name="T"> The type of elements in the list. </typeparam>
        /// <param name="sortedList"> A list with all elements sorted. </param>
        /// <param name="item">
        /// The element to insert. The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use to sort elements.
        /// </param>
		public static void AddSorted<T>([NotNull] this List<T> sortedList, [CanBeNull] T item, [CanBeNull] IComparer<T> comparer)
        {
            int index = sortedList.BinarySearch(item, comparer);
            sortedList.Insert((index >= 0) ? index : ~index, item);
        }
	}
}