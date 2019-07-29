using System;
using System.Collections.Generic;
using gc = WPFComputationalGeometry.Source.Models.GeometryCalculations;

namespace WPFComputationalGeometry.Source.Models
{
    public class PolarAngleComparer : IEqualityComparer<Point>
    {
        public Point? O { get; set; }

        public PolarAngleComparer(Point o)
        {
            O = o;
        }

        public bool Equals(Point p1, Point p2)
        {
            if (O == null)
                throw new Exception("Nie podano punktu O");
            
            var op2 = new LineSegment((Point) O, p2);
            var orTest = gc.PointOrientationTest(p1, op2);
            return orTest == 0; // po lewej stronie > 0
        }

        public int GetHashCode(Point obj)
        {
            return base.GetHashCode();
        }
    }
}
