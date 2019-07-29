using System;
using System.Linq;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Models
{
    public class Triangle
    {
        public Point Vertex1;
        public Point Vertex2;
        public Point Vertex3;
        
        public Triangle(Point vertex1, Point vertex2, Point vertex3)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
        }

        public LineSegment[] Edges()
        {
            return new[]
            {
                new LineSegment(Vertex1, Vertex2, true),
                new LineSegment(Vertex2, Vertex3, true),
                new LineSegment(Vertex3, Vertex1, true)
            };
        }

        public Point[] Vertices()
        {
            return new[]
            {
                Vertex1,
                Vertex2,
                Vertex3
            };
        }

        public bool ContainsInCircumcircle(Point p)
        {
            var p1 = Vertex1;
            var p2 = Vertex2;
            var p3 = Vertex3;
            if (Math.Abs(p1.Y - p2.Y) < DoubleExtensions.TOLERANCE && Math.Abs(p2.Y - p3.Y) < DoubleExtensions.TOLERANCE) // < double.Epsilon
                return false;

            double m1, m2;
            double mx1, mx2;
            double my1, my2;
            double xc, yc;

            if (Math.Abs(p2.Y - p1.Y) < DoubleExtensions.TOLERANCE) // < double.Epsilon
            {
                m2 = -(p3.X - p2.X) / (p3.Y - p2.Y);
                mx2 = (p2.X + p3.X) * 0.5;
                my2 = (p2.Y + p3.Y) * 0.5; //Calculate CircumCircle center (xc,yc) 

                xc = (p2.X + p1.X) * 0.5;
                yc = m2 * (xc - mx2) + my2;
            }
            else if (p3.Y - p2.Y < DoubleExtensions.TOLERANCE) // < double.Epsilon
            {
                m1 = -(p2.X - p1.X) / (p2.Y - p1.Y);
                mx1 = (p1.X + p2.X) * 0.5;
                my1 = (p1.Y + p2.Y) * 0.5;

                xc = (p3.X + p2.X) * 0.5; // Oblicz środek okręgu opisanego (xc,yc) 
                yc = m1 * (xc - mx1) + my1;
            }
            else
            {
                m1 = -(p2.X - p1.X) / (p2.Y - p1.Y);
                m2 = -(p3.X - p2.X) / (p3.Y - p2.Y);
                mx1 = (p1.X + p2.X) * 0.5;
                mx2 = (p2.X + p3.X) * 0.5;
                my1 = (p1.Y + p2.Y) * 0.5;
                my2 = (p2.Y + p3.Y) * 0.5;

                xc = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2); // Oblicz środek okręgu opisanego (xc,yc) 
                yc = m1 * (xc - mx1) + my1;
            }

            var dx = p2.X - xc;
            var dy = p2.Y - yc;
            var rsqr = dx * dx + dy * dy; //double r = Math.Sqrt(rsqr); // Promień okręgu opisanego

            dx = p.X - xc;
            dy = p.Y - yc;
            var drsqr = dx * dx + dy * dy;

            return drsqr <= rsqr;
        }

        public bool SharesVertexWith(Triangle triangle)
        {
            return Vertices().Any(v => triangle.Vertices().Any(v2 => v == v2));
        }

        public override bool Equals(object obj)
        {
            var triangle = (Triangle)obj;
            return Vertex1 == triangle.Vertex1 && Vertex2 == triangle.Vertex2 && Vertex3 == triangle.Vertex3;
        }

        public override int GetHashCode()
        {
            return Vertex1.GetHashCode() ^ Vertex2.GetHashCode() ^ Vertex3.GetHashCode();
        }

        public override string ToString()
        {
            return $"[{Vertex1}, {Vertex2}, {Vertex3}]";
        }
    }
}
