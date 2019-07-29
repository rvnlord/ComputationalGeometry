using System;
using System.Globalization;

namespace WPFComputationalGeometry.Source.Common.Converters
{
    public static class DecimalConverter
    {
        public static decimal? ToDecimalN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToDecimal(obj);
            return decimal.TryParse(obj.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var tmpvalue) ? tmpvalue : (decimal?)null;
        }

        public static decimal ToDecimal(this object obj)
        {
            var decimalN = obj.ToDecimalN();
            if (decimalN != null) return (decimal)decimalN;
            throw new ArgumentNullException(nameof(obj));
        }
    }
}
