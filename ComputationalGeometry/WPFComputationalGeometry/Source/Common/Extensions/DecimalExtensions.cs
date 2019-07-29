using System;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class DecimalExtensions
    {
        public static decimal DecimalPlaces(this decimal dec)
        {
            dec = Math.Abs(dec);

            if (dec == Math.Floor(dec))
                return 0;

            var bits = decimal.GetBits(dec);
            var exponent = bits[3] >> 16;
            var decimals = exponent;
            long lowDecimal = bits[0] | (bits[1] >> 8);
            while (lowDecimal % 10 == 0)
            {
                decimals--;
                lowDecimal /= 10;
            }

            return decimals;
        }
    }
}
