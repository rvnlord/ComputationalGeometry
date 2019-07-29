using System;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class DoubleExtensions
    {
        public const double TOLERANCE = 0.00001;

        public static bool Eq(this double x, double y) => Math.Abs(x - y) < TOLERANCE;
        public static double ToDegrees(this double radians) => radians * (180.0 / Math.PI);
        public static double ToRadians(this double degrees) => Math.PI * degrees / 180.0;

        public static bool Eq(this double? d1, double d2)
        {
            if (d1 == null)
                return false;

            return Math.Abs((double)d1 - d2) < TOLERANCE;
        }

        public static bool Rw(this double d1, double? d2)
        {
            if (d2 == null)
                return false;

            return Math.Abs(d1 - (double)d2) < TOLERANCE;
        }

        public static bool Eq(this double? d1, double? d2)
        {
            if (d1 == null && d2 == null)
                return true;
            if (d1 == null || d2 == null)
                return false;

            return Math.Abs((double)d1 - (double)d2) < TOLERANCE;
        }
    }
}
