using System;
using WPFComputationalGeometry.Source.Models.Comparers;

namespace WPFComputationalGeometry.Source.Models
{
    public struct LineSegmentFr
    {
        public PointFr StartPoint { get; set; }
        public PointFr EndPoint { get; set; }

        public Fraction MinY { get; }
        public Fraction MinX { get; }
        public Fraction MaxY { get; }
        public Fraction MaxX { get; }

        public LineFr Line { get; private set; }

        public LineSegmentFr(PointFr start, PointFr end, bool LeftMostPointFirst = false)
        {
            if (start.Equals(end))
                throw new ArgumentException("Nie można utworzyć odcinka, o dwóch takich samych punktach");

            StartPoint = start;
            EndPoint = end;

            MaxX = start.X > end.X ? start.X : end.X;
            MaxY = start.Y > end.Y ? start.Y : end.Y;
            MinX = start.X < end.X ? start.X : end.X;
            MinY = start.Y < end.Y ? start.Y : end.Y;

            Line = new LineFr(start, end);

            if (LeftMostPointFirst)
                NormalizeEndPoints();
        }

        private bool IsLeftMostPointFirst()
        {
            var comparer = new PointFrXThenYComparer(); // Upewnij się że mniejszym punktem odcinka jest ten na wykresie o mniejszym x
            return comparer.Compare(StartPoint, EndPoint) <= 0;
        }

        public void SwapEndPoints()
        {
            var temp = StartPoint;
            StartPoint = EndPoint;
            EndPoint = temp;
            Line = new LineFr(StartPoint, EndPoint);
        }

        public void NormalizeEndPoints()
        {
            if (IsLeftMostPointFirst())
                return;
            SwapEndPoints();
        }

        public PointFr[] EndPoints()
        {
            return new[] { StartPoint, EndPoint };
        }

        public PointFr Center()
        {
            var segment = new LineSegmentFr(StartPoint, EndPoint, true);
            var dX = segment.EndPoint.X - segment.StartPoint.X;
            var dY = segment.EndPoint.Y - segment.StartPoint.Y;
            return new PointFr(segment.StartPoint.X + dX / 2, segment.StartPoint.Y + dY / 2);
        }

        public bool Contains(PointFr p)
        {
            return StartPoint.Equals(p) || EndPoint.Equals(p) 
                || (Line.Contains(p) && (p.X >= MinX) && p.X <= MaxX && p.Y >= MinY && p.Y <= MaxY);
        }

        public bool HasEnding(PointFr p)
        {
            return StartPoint.Equals(p) || EndPoint.Equals(p);
        }

        public PointFr? Intersection(LineSegmentFr that)
        {
            if (StartPoint.Equals(that.EndPoint))
                return StartPoint;
            if (EndPoint.Equals(that.StartPoint))
                return EndPoint;
            if (StartPoint.Equals(that.StartPoint))
                return Line.Slope.Equals(that.Line.Slope) ? (PointFr?)null : StartPoint;
            if (EndPoint.Equals(that.EndPoint))
                return Line.Slope.Equals(that.Line.Slope) ? (PointFr?)null : EndPoint;

            var p = Line.Intersection(that.Line);
            if (p == null || !Contains((PointFr)p) || !that.Contains((PointFr)p))
                return null;
            return p;
        }

        public PointFr? Intersection(LineFr line)
        {
            var p = Line.Intersection(line);
            if (p == null || !Contains((PointFr)p))
                return null;
            return p;
        }

        public bool Intersects(LineSegmentFr that)
        {
            return Intersection(that) != null;
        }

        public bool IntersectsStrict(LineSegmentFr that) // nie uwzględnia stykających się wierzchołków
        {
            if (!Intersects(that))
                return false;
            return !Contains(that.StartPoint) && !Contains(that.EndPoint) &&
                   !that.Contains(StartPoint) && !that.Contains(EndPoint);

            //var pn = Line.Intersection(that.Line);
            //if (pn == null) return false;
            //var p = (PointFr) pn;
            //return 
            //    Line.Contains(p) 
            //        && (MinX == MaxX ? p.X >= MinX : p.X > MinX) 
            //        && (MinX == MaxX ? p.X <= MaxX : p.X < MaxX) 
            //        && (MinY == MaxY ? p.Y >= MinY : p.Y > MinY)
            //        && (MinY == MaxY ? p.Y <= MaxY : p.Y < MaxY)
            //    && that.Line.Contains(p) 
            //        && (that.MinX == that.MaxX ? p.X >= that.MinX : p.X > that.MinX)
            //        && (that.MinX == that.MaxX ? p.X <= that.MaxX : p.X < that.MaxX)
            //        && (that.MinY == that.MaxY ? p.Y >= that.MinY : p.Y > that.MinY)
            //        && (that.MinY == that.MaxY ? p.Y <= that.MaxY : p.Y < that.MaxY);
        }

        public bool Intersects(LineFr line)
        {
            return Intersection(line) != null;
        }

        public bool IsEqualLength(LineSegmentFr that)
        {
            return LengthSquared().Equals(that.LengthSquared());
        }

        public bool IsLongerThan(LineSegmentFr that)
        {
            return LengthSquared() > that.LengthSquared();
        }

        public bool isShorterThan(LineSegmentFr that)
        {
            return LengthSquared() < that.LengthSquared();
        }

        public double Length()
        {
            return StartPoint.Distance(EndPoint);
        }

        public Fraction LengthSquared()
        {
            return StartPoint.DistanceSquared(EndPoint);
        }

        public Fraction LengthXY()
        {
            return StartPoint.DistanceXY(EndPoint);
        }

        public override bool Equals(object o)
        {
            if (o == null || GetType() != o.GetType())
                return false;
            var that = (LineSegmentFr)o;
            return StartPoint.Equals(that.StartPoint) && EndPoint.Equals(that.EndPoint);
        }

        public static bool operator ==(LineSegmentFr s1, LineSegmentFr s2)
        {
            return s1.Equals(s2);
        }

        public static bool operator !=(LineSegmentFr s1, LineSegmentFr s2)
        {
            return !(s1 == s2);
        }

        public LineSegment ToLineSegmentDouble()
        {
            return new LineSegment(StartPoint.ToPointDouble(), EndPoint.ToPointDouble());
        }

        public override string ToString()
        {
            return $"[{StartPoint}~{EndPoint}]";
        }

        public override int GetHashCode()
        {
            return (StartPoint.GetHashCode() * 13) ^ (EndPoint.GetHashCode() * 37);
        }
    }
}
