using System;
using System.Collections;

namespace WPFComputationalGeometry.Source.Common.Extensions.Collections
{
    public static class IListExtensions
    {
        public static void AddRange(this IList list, IEnumerable items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            foreach (var current in items)
                list.Add(current);
        }
    }
}
