using System.Collections.Generic;
using WPFComputationalGeometry.Source.Common.Extensions;
using gc = WPFComputationalGeometry.Source.Models.GeometryCalculations;

namespace WPFComputationalGeometry.Source.Models.Comparers
{
    public class PointAngularComparer : Comparer<Point>
    {
        public Point? O { get; set; }
        public bool Inverse { get; set; }

        public PointAngularComparer(Point zeroPoint, bool inverse)
        {
            O = zeroPoint;
            Inverse = inverse;
        }

        public override int Compare(Point p1, Point p2)
        {
            var o = O ?? new Point(0, 0);
            var op1 = new LineSegment(o, p1);
            var op2 = new LineSegment(o, p2);
            var pos = gc.PointOrientationTest(p1, op2); // po lewej stronie > 0

            if (pos > 0 || pos.Eq(0) && op1.Length() < op2.Length())
                return Inverse ? -1 : 1;
            if (pos < 0 || pos.Eq(0) && op1.Length() > op2.Length())
                return Inverse ? 1 : -1;

            return 0;
        }
    }
}
