using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Models
{
    public class PointXThenYComparer : Comparer<Point>
    {
        public override int Compare(Point p1, Point p2)
        {
            var dX = p1.X.CompareTo(p2.X);
            return dX != 0 ? dX : p1.Y.CompareTo(p2.Y);
        }
    }
}
