using System.Linq;

namespace WPFComputationalGeometry.Source.Common.Utils.TypeUtils
{
    public static class ArrayUtils
    {
        public static T[] ConcatMany<T>(params T[][] arrays) => arrays.SelectMany(x => x).ToArray();
    }
}