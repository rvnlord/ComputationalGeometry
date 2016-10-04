using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WPFDemo.Models;
using static WPFDemo.Models.GeometryCalculations;

namespace WPFDemo.Models
{
    public class TriangleFr
    {
        public PointFr Vertex1;
        public PointFr Vertex2;
        public PointFr Vertex3;

        public TriangleFr(PointFr vertex1, PointFr vertex2, PointFr vertex3)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
        }

        public LineSegmentFr[] Edges(bool leftMostPointFirst = false)
        {
            var vertices = Vertices();
            return new[]
            {
                new LineSegmentFr(vertices[0], vertices[1], leftMostPointFirst),
                new LineSegmentFr(vertices[1], vertices[2], leftMostPointFirst),
                new LineSegmentFr(vertices[2], vertices[0], leftMostPointFirst)
            };
        }

        public PointFr[] Vertices()
        {
            var vertices = new List<PointFr> { Vertex1, Vertex2, Vertex3 };
            if (AreVerticesClockwise())
                vertices.Reverse();
            return vertices.ToArray();
        }

        public bool AreVerticesClockwise()
        {
            var vertices = new[] { Vertex1, Vertex2, Vertex3 };
            Fraction sum = 0;
            for (var i = 0; i < vertices.Length; i++)
            {
                var p1 = vertices[i];
                var p2 = vertices[(i + 1) % vertices.Length];
                sum += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

            if (sum == 0)
                throw new Exception("Niepoprawne obliczenia, suma nie może nigdy wynosić 0");

            return sum > 0;
        }

        public bool ContainsInCircumcircle(PointFr p, TriangleFr superTriangle = null)
        {
            var vertices = Vertices().OrderBy(pnt => pnt.X).ThenBy(pnt => pnt.Y).ToList();
            var p1 = vertices[0];
            var p2 = vertices[1];
            var p3 = vertices[2];

            if ((p1.Y - p2.Y).Abs() == 0 && (p2.Y - p3.Y).Abs() == 0) // < double.Epsilon
                return false;

            if (superTriangle != null)
            {
                var sharedVertices = SharedVertices(superTriangle).ToList();
                var otherVertices = vertices.Except(sharedVertices).ToList();
                if (sharedVertices.Count == 1)
                    return !new LineSegmentFr(p, sharedVertices.Single()).Intersects(new LineFr(otherVertices[0], otherVertices[1]));
                if (sharedVertices.Count == 2)
                    return !new LineSegmentFr(p, sharedVertices.First()).Intersects(new LineFr(new LineFr(sharedVertices[0], sharedVertices[1]).Slope, otherVertices[0]));
            }

            Fraction m1, m2;
            Fraction mx1, mx2;
            Fraction my1, my2;
            Fraction xc, yc;

            if ((p2.Y - p1.Y).Abs() == 0) // < double.Epsilon
            {
                m2 = -(p3.X - p2.X) / (p3.Y - p2.Y);
                mx2 = (p2.X + p3.X) * 0.5;
                my2 = (p2.Y + p3.Y) * 0.5; // Oblicz środek okręgu opisanego (xc, yc) 

                xc = (p2.X + p1.X) * 0.5;
                yc = m2 * (xc - mx2) + my2;
            }
            else if ((p3.Y - p2.Y).Abs() == 0) // < double.Epsilon
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

        public CircleFr CircumCircle()
        {
            var vertices = Vertices().OrderBy(pnt => pnt.X).ThenBy(pnt => pnt.Y).ToList();
            var p1 = vertices[0];
            var p2 = vertices[1];
            var p3 = vertices[2];

            if ((p1.Y - p2.Y).Abs() == 0 && (p2.Y - p3.Y).Abs() == 0)
                return null;

            Fraction m1, m2;
            Fraction mx1, mx2;
            Fraction my1, my2;
            Fraction xc, yc;

            if ((p2.Y - p1.Y).Abs() == 0)
            {
                m2 = -(p3.X - p2.X) / (p3.Y - p2.Y);
                mx2 = (p2.X + p3.X) * 0.5;
                my2 = (p2.Y + p3.Y) * 0.5; // Oblicz środek okręgu opisanego (xc, yc) 

                xc = (p2.X + p1.X) * 0.5;
                yc = m2 * (xc - mx2) + my2;
            }
            else if ((p3.Y - p2.Y).Abs() == 0) // < double.Epsilon
            {
                m1 = -(p2.X - p1.X) / (p2.Y - p1.Y);
                mx1 = (p1.X + p2.X) * 0.5;
                my1 = (p1.Y + p2.Y) * 0.5;

                xc = (p3.X + p2.X) * 0.5; // Oblicz środek okręgu opisanego (xc, yc) 
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

            return new CircleFr(new PointFr(xc, yc), new Fraction(Math.Sqrt(rsqr.ToDouble())));
        }

        public bool SharesVertexWith(TriangleFr triangle)
        {
            return Vertices().Any(v => triangle.Vertices().Any(v2 => v == v2));
        }

        public List<PointFr> SharedVertices(TriangleFr triangle)
        {
            return Vertices().SelectMany(v => triangle.Vertices().Where(v2 => v == v2), (v, v2) => v).ToList();
        }

        public override bool Equals(object obj)
        {
            var triangle = (TriangleFr)obj;
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
