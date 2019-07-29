using System;
using System.Globalization;
using System.Linq;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Common.Converters
{
    public static class DoubleConverter
    {
        public static double? ToDoubleN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToDouble(obj);

            var strD = obj.ToString().Replace(",", ".");
            var isNegative = strD.StartsWith("-");
            if (isNegative || strD.StartsWith("+"))
                strD = strD.Skip(1);

            var parsedVal = double.TryParse(strD, NumberStyles.Any, CultureInfo.InvariantCulture, out var tmpvalue) ? tmpvalue : (double?)null;
            if (isNegative)
                parsedVal = -parsedVal;
            return parsedVal;
        }

        public static double ToDouble(this object obj)
        {
            var doubleN = obj.ToDoubleN();
            if (doubleN != null) return (double)doubleN;
            throw new ArgumentNullException(nameof(obj));
        }
    }
}