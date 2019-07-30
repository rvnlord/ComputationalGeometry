using System;
using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Models.Comparers
{
    public class PointXCoordComparer : Comparer<Point>
    {
        public override int Compare(Point p1, Point p2)
        {
            return p1.X.CompareTo(p2.X);
        }
    }

    public class PointXCoordComparerInversed : Comparer<Point>
    {
        public override int Compare(Point p1, Point p2)
        {
            return p1.X.CompareTo(p2.X) * -1;
        }
    }

    public class NullablePointXCoordComparerInversed : Comparer<Point?>
    {
        public override int Compare(Point? p1, Point? p2)
        {
            if (p1 != null && p2 != null)
                return p1.Value.X.CompareTo(p2.Value.X) * -1;

            throw new Exception("Nie można porównać wartości null");
        }
    }
}
