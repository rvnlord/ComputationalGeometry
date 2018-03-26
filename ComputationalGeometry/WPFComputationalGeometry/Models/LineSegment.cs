using System;
using static WPFDemo.Models.ExtensionMethods;

namespace WPFDemo.Models
{
    public struct LineSegment
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public double MinY { get; }
        public double MinX { get; }
        public double MaxY { get; }
        public double MaxX { get; }

        public Line Line { get; private set; }

        public LineSegment(Point start, Point end, bool LeftMostPointFirst = false)
        {
            if (start.Equals(end))
                throw new ArgumentException("Nie można utworzyć odcinka, o dwóch takich samych punktach");

            StartPoint = start;
            EndPoint = end;

            MaxX = start.X > end.X ? start.X : end.X;
            MaxY = start.Y > end.Y ? start.Y : end.Y;
            MinX = start.X < end.X ? start.X : end.X;
            MinY = start.Y < end.Y ? start.Y : end.Y;

            Line = new Line(start, end);

            if (LeftMostPointFirst)
                NormalizeEndPoints();
        }

        private bool IsLeftMostPointFirst()
        {
            var comparer = new PointXThenYComparer(); // Upewnij się że mniejszym punktem odcinka jest ten na wykresie o mniejszym x
            return comparer.Compare(StartPoint, EndPoint) <= 0;
        }

        public void SwapEndPoints()
        {
            var temp = StartPoint;
            StartPoint = EndPoint;
            EndPoint = temp;
            Line = new Line(StartPoint, EndPoint);
        }

        public void NormalizeEndPoints()
        {
            if (IsLeftMostPointFirst())
                return;
            SwapEndPoints();
        }

        public Point[] EndPoints()
        {
            return new[] { StartPoint, EndPoint };
        }

        public Point Center()
        {
            var segment = new LineSegment(StartPoint, EndPoint, true);
            var dX = segment.EndPoint.X - segment.StartPoint.X;
            var dY = segment.EndPoint.Y - segment.StartPoint.Y;
            return new Point(segment.StartPoint.X + dX / 2, segment.StartPoint.Y + dY / 2);
        }

        public bool Contains(Point p)
        {
            return StartPoint.Equals(p) || EndPoint.Equals(p)
                || (Line.Contains(p) && (p.X >= MinX) && p.X <= MaxX && p.Y >= MinY && p.Y <= MaxY);
        }

        public bool HasEnding(Point p)
        {
            return StartPoint.Equals(p) || EndPoint.Equals(p);
        }

        public Point? Intersection(LineSegment that)
        {
            if (StartPoint.Equals(that.EndPoint))
                return StartPoint;
            if (EndPoint.Equals(that.StartPoint))
                return EndPoint;
            if (StartPoint.Equals(that.StartPoint))
                return Line.Slope.Equals(that.Line.Slope) ? (Point?)null : StartPoint;
            if (EndPoint.Equals(that.EndPoint))
                return Line.Slope.Equals(that.Line.Slope) ? (Point?)null : EndPoint;

            var p = Line.Intersection(that.Line);
            if (p == null || !Contains((Point)p) || !that.Contains((Point)p))
                return null;
            return p;
        }

        public Point? Intersection(Line line)
        {
            var p = Line.Intersection(line);
            if (p == null || !Contains((Point)p))
                return null;
            return p;
        }

        public bool Intersects(LineSegment that)
        {
            return Intersection(that) != null;
        }

        public bool IntersectsStrict(LineSegment that) // nie uwzględnia stykających się wierzchołków
        {
            var pn = Line.Intersection(that.Line);
            if (pn == null) return false;
            var p = (Point)pn;

            var pXRounded = Math.Round(p.X, 5);
            var pYRounded = Math.Round(p.Y, 5);
            var MinXRounded = Math.Round(MinX, 5);
            var MaxXRounded = Math.Round(MaxX, 5);
            var MinYRounded = Math.Round(MinY, 5);
            var MaxYRounded = Math.Round(MaxY, 5);
            var thatMinXRounded = Math.Round(that.MinX, 5);
            var thatMaxXRounded = Math.Round(that.MaxX, 5);
            var thatMinYRounded = Math.Round(that.MinY, 5);
            var thatMaxYRounded = Math.Round(that.MaxY, 5);

            return 
                Line.Contains(p) 
                    && (Math.Abs(MinXRounded - MaxXRounded) < EPSILON ? pXRounded >= MinXRounded : pXRounded > MinXRounded)
                    && (Math.Abs(MinXRounded - MaxXRounded) < EPSILON ? pXRounded <= MaxXRounded : pXRounded < MaxXRounded)
                    && (Math.Abs(MinYRounded - MaxYRounded) < EPSILON ? pYRounded >= MinYRounded : pYRounded > MinYRounded)
                    && (Math.Abs(MinYRounded - MaxYRounded) < EPSILON ? pYRounded <= MaxYRounded : pYRounded < MaxYRounded)
                && that.Line.Contains(p) 
                    && (Math.Abs(thatMinXRounded - thatMaxXRounded) < EPSILON ? pXRounded >= thatMinXRounded : pXRounded > thatMinXRounded)
                    && (Math.Abs(thatMinXRounded - thatMaxXRounded) < EPSILON ? pXRounded <= thatMaxXRounded : pXRounded < thatMaxXRounded)
                    && (Math.Abs(thatMinYRounded - thatMaxYRounded) < EPSILON ? pYRounded >= thatMinYRounded : pYRounded > thatMinYRounded)
                    && (Math.Abs(thatMinYRounded - thatMaxYRounded) < EPSILON ? pYRounded <= thatMaxYRounded : pYRounded < thatMaxYRounded);
        }

        public bool Intersects(Line line)
        {
            return Intersection(line) != null;
        }

        public bool IsEqualLength(LineSegment that)
        {
            return LengthSquared().Equals(that.LengthSquared());
        }

        public bool IsLongerThan(LineSegment that)
        {
            return LengthSquared() > that.LengthSquared();
        }

        public bool isShorterThan(LineSegment that)
        {
            return LengthSquared() < that.LengthSquared();
        }

        public double Length()
        {
            return StartPoint.Distance(EndPoint);
        }

        public double LengthSquared()
        {
            return StartPoint.DistanceSquared(EndPoint);
        }

        public double LengthXY()
        {
            return StartPoint.DistanceXY(EndPoint);
        }

        public override bool Equals(object o)
        {
            if (o == null || GetType() != o.GetType())
                return false;
            var that = (LineSegment)o;
            return StartPoint.Equals(that.StartPoint) && EndPoint.Equals(that.EndPoint);
        }

        public static bool operator ==(LineSegment s1, LineSegment s2)
        {
            return s1.Equals(s2);
        }

        public static bool operator !=(LineSegment s1, LineSegment s2)
        {
            return !(s1 == s2);
        }

        public LineSegmentFr ToLineSegmentFraction()
        {
            return new LineSegmentFr(StartPoint.ToPointFraction(), EndPoint.ToPointFraction());
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
