using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Models
{
    public class Polygon
    {
        private List<Point> _vertices;

        public Collection<Point> Vertices
        {
            get
            {
                return new Collection<Point>(_vertices);
            }
            set
            {
                _vertices = value.ToList();
                if (AreVerticesClockwise())
                    _vertices.Reverse();
            }
        }

        public Point? Center { get; private set; }

        public Polygon(IEnumerable<Point> vertices)
        {
            Vertices = new Collection<Point>(vertices.ToList());
            Center = GetCenter();
        }

        public IEnumerable<LineSegment> Edges(bool leftMostPointsFirst = false)
        {
            return Vertices.Select((v, i) => new LineSegment(v, Vertices[(i + 1) % Vertices.Count], leftMostPointsFirst));
        }

        public PolygonFr ToPolygonFraction()
        {
            return new PolygonFr(_vertices.Select(p => p.ToPointFraction()));
        }

        public bool AreVerticesClockwise()
        {
            var sum = 0.0;
            for (var i = 0; i < _vertices.Count; i++)
            {
                var p1 = _vertices[i];
                var p2 = _vertices[(i + 1) % _vertices.Count];
                sum += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

            if (Math.Abs(sum) < DoubleExtensions.TOLERANCE)
                throw new Exception("Niepoprawne obliczenia, suma nie może nigdy wynosić 0");

            return sum > 0;
        }

        private Point? GetCenter()
        {
            double accumulatedArea = 0;
            double centerX = 0;
            double centerY = 0;

            for (int i = 0, j = _vertices.Count - 1; i < _vertices.Count; j = i++)
            {
                var temp = _vertices[i].X * _vertices[j].Y - _vertices[j].X * _vertices[i].Y;
                accumulatedArea += temp;
                centerX += (_vertices[i].X + _vertices[j].X) * temp;
                centerY += (_vertices[i].Y + _vertices[j].Y) * temp;
            }

            if (Math.Abs(accumulatedArea) < DoubleExtensions.TOLERANCE) // Dzielenie przez 0 | < 1E-7f
                return null;

            accumulatedArea *= 3f;
            return new Point(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        public bool Contains(Point point)
        {
            if (Edges(true).Any(edge => edge.Contains(point)))
                return true;

            int i, j = _vertices.Count - 1;
            var contains = false;

            for (i = 0; i < _vertices.Count; i++)
            {
                if ((_vertices[i].Y < point.Y && _vertices[j].Y >= point.Y || _vertices[j].Y < point.Y && _vertices[i].Y >= point.Y) && (_vertices[i].X <= point.X || _vertices[j].X <= point.X))
                    contains ^= (_vertices[i].X + (point.Y - _vertices[i].Y) / (_vertices[j].Y - _vertices[i].Y) * (_vertices[j].X - _vertices[i].X) < point.X);
                j = i;
            }

            return contains;
        }

        public bool Contains(Polygon polygon)
        {
            var ps = polygon.Vertices.ToList();
            return ps.Count != 0 && ps.All(Contains);
        }

        public bool Contains(IEnumerable<Point> points)
        {
            var ps = points.ToList();
            return ps.Count != 0 && ps.All(Contains);
        }

        public override string ToString()
        {
            return $"[{string.Join("; ", Vertices)}]";
        }
    }
}
