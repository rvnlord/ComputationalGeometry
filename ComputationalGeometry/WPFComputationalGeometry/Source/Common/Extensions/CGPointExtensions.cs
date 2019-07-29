using System;
using WPFComputationalGeometry.Source.Models;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class CGPointExtensions
    {
        public static bool Eq(this Point p1, Point p2) => p1.X.Eq(p2.X) && p1.Y.Eq(p2.Y);

        public static double Distance(this Point p1, Point that)
        {
            var dX = p1.X - that.X;
            var dY = p1.Y - that.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }
    }
}
