using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WPFComputationalGeometry.Source.Models
{
    public class PolygonFr
    {
        private List<PointFr> _vertices;

        public Collection<PointFr> Vertices
        {
            get
            {
                return new Collection<PointFr>(_vertices);
            }
            set
            {
                _vertices = value.ToList();
                if (AreVerticesClockwise())
                    _vertices.Reverse();
            }
        }

        public PointFr? Center { get; private set; }

        public PolygonFr(IEnumerable<PointFr> vertices)
        {
            Vertices = new Collection<PointFr>(vertices.ToList());
            Center = GetCenter();
        }

        public IEnumerable<LineSegmentFr> Edges(bool leftMostPointsFirst = false)
        {
            return Vertices.Select((v, i) => new LineSegmentFr(v, Vertices[(i + 1) % Vertices.Count], leftMostPointsFirst));
        }

        public Polygon ToPolygonDouble()
        {
            return new Polygon(_vertices.Select(p => p.ToPointDouble()));
        }

        public bool AreVerticesClockwise()
        {
            Fraction sum = 0;
            for (var i = 0; i < _vertices.Count; i++)
            {
                var p1 = _vertices[i];
                var p2 = _vertices[(i + 1) % _vertices.Count];
                sum += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

            if (sum == 0)
                throw new Exception("Niepoprawne obliczenia, suma nie może nigdy wynosić 0");

            return sum > 0;
        }

        private PointFr? GetCenter()
        {
            Fraction accumulatedArea = 0;
            Fraction centerX = 0;
            Fraction centerY = 0;

            for (int i = 0, j = _vertices.Count - 1; i < _vertices.Count; j = i++)
            {
                var temp = _vertices[i].X * _vertices[j].Y - _vertices[j].X * _vertices[i].Y;
                accumulatedArea += temp;
                centerX += (_vertices[i].X + _vertices[j].X) * temp;
                centerY += (_vertices[i].Y + _vertices[j].Y) * temp;
            }

            if (accumulatedArea == 0) // Dzielenie przez 0 | < 1E-7f
                return null;

            accumulatedArea *= 3f;
            return new PointFr(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        public bool Contains(PointFr point)
        {
            return GeometryCalculations.IsInsidePolygon(point, this);
        }

        public bool Contains2(PointFr point)
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

        public bool Contains(PolygonFr polygon)
        {
            var ps = polygon.Vertices.ToList();
            return ps.Count != 0 && ps.All(Contains);
        }

        public bool Contains(IEnumerable<PointFr> points)
        {
            var ps = points.ToList();
            return ps.Count != 0 && ps.All(Contains);
        }

        public List<PointFr> Intersections(LineSegmentFr segment)
        {
            return
                Edges(true)
                    .Select(segment.Intersection)
                    .Where(intersection => intersection != null)
                    .Select(intersection => (PointFr) intersection).ToList();
        }

        public bool Intersects(LineSegmentFr segment)
        {
            return Intersections(segment).Any();
        }

        public override string ToString()
        {
            return $"[{string.Join("; ", Vertices)}]";
        }
    }
}