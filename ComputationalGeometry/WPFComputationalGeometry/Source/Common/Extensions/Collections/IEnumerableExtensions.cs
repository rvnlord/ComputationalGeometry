using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Common.Extensions.Collections
{
    public static class IEnumerableExtensions
    {
        public static string JoinAsString<T>(this IEnumerable<T> enumerable, string strBetween = "")
        {
            return string.Join(strBetween, enumerable);
        }
    }
}
