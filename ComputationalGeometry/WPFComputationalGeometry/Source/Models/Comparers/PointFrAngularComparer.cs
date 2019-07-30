using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Models.Comparers
{
    public class PointFrAngularComparer : Comparer<PointFr>
    {
        public PointFr? O { get; set; }
        public bool Inverse { get; set; }

        public PointFrAngularComparer(PointFr zeroPoint, bool inverse)
        {
            O = zeroPoint;
            Inverse = inverse;
        }

        public override int Compare(PointFr p1, PointFr p2)
        {
            var o = O ?? new PointFr(0, 0);
            var op1 = new LineSegmentFr(o, p1);
            var op2 = new LineSegmentFr(o, p2);
            var pos = GeometryCalculations.PointFrOrientationTest(p1, op2); // po lewej stronie > 0

            if (pos > 0 || (pos == 0 && op1.Length() < op2.Length()))
                return Inverse ? -1 : 1;
            if (pos < 0 || (pos == 0 && op1.Length() > op2.Length()))
                return Inverse ? 1 : -1;

            return 0;
        }
    }
}
