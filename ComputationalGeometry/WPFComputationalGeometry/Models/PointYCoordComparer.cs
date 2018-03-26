using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFDemo.Models
{
    public class PointYCoordComparer : Comparer<Point>
    {
        public override int Compare(Point p1, Point p2)
        {
            return p1.Y.CompareTo(p2.Y);
        }
    }

    public class NullablePointYCoordComparer : Comparer<Point?>
    {
        public override int Compare(Point? p1, Point? p2)
        {
            if (p1 != null && p2 != null)
                return p1.Value.X.CompareTo(p2.Value.X);

            throw new Exception("Nie można porównać wartości null");
        }
    }
}
