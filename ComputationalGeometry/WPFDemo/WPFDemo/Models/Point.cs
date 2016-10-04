using System;
using System.Windows;
using static WPFDemo.Models.ExtensionMethods;

namespace WPFDemo.Models
{
    public struct Point
    {
        private double _x;
        private double _y;

        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        public static readonly Point Origin = new Point(0, 0);

        public Point(long x, long y)
        {
            _x = x;
            _y = y;
        }

        public Point(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public Point(double x, double y)
        {
            _x = x;
            _y = y;
        }

        public double Distance(Point that)
        {
            var dX = _x - that.X;
            var dY = _y - that.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }

        public double DistanceSquared(Point that)
        {
            var dX = _x - that.X;
            var dY = _y - that.Y;
            return dX * dX + dY * dY;
        }

        public double DistanceXY(Point that)
        {
            var dX = Math.Abs(_x - that.X);
            var dY = Math.Abs(_y - that.Y);
            return dX + dY;
        }

        public override bool Equals(object o)
        {
            if (o == null || GetType() != o.GetType())
                return false;
            var that = (Point)o;
            return Math.Abs(_x - that.X) < EPSILON && Math.Abs(_y - that.Y) < EPSILON;
        }

        public static bool operator ==(Point p1, Point p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }

        public override int GetHashCode()
        {
            return (_x.GetHashCode() * 23) ^ (Y.GetHashCode() * 17);
        }

        public bool IsAbove(PointFr that)
        {
            return _x > that.Y;
        }

        public bool IsBelow(PointFr that)
        {
            return _x < that.Y;
        }

        public bool IsLeftOf(Line line)
        {
            return !line.Contains(this) && !IsRightOf(line);
        }

        public bool IsLeftOf(Point that)
        {
            return _x < that.X;
        }

        public bool IsRightOf(Line line)
        {
            if (line.Contains(this))
                return false;
            if (line.IsHorizontal())
                return Y < line.YIntercept();
            if (line.IsVertical())
            {
                return X > line.XIntercept();
            }

            var linePointNullable = line.Intersection(Line.Vertical(0));
            if (linePointNullable == null)
                throw new ArgumentException("Wartość linePoint nie może wynosić null");

            var linePoint = (Point)linePointNullable;
            var temp = new Line(this, linePoint);

            if (Y < linePoint.Y)
            {
                if (line.Slope < 0) // a
                    return temp.Slope < 0 && temp.Slope > line.Slope;

                return temp.Slope < 0 || temp.Slope > line.Slope; // b
            }
            if (Y > linePoint.Y)
            {
                if (line.Slope < 0) // c
                    return temp.Slope >= 0 || temp.Slope < line.Slope;

                return temp.Slope >= 0 && temp.Slope < line.Slope; // d
            }
            return IsRightOf(linePoint); // 'this' ma taką samą współrzędną y jak 'linePoint'
        }

        public bool IsRightOf(LineSegment segment)
        {
            return PointOrientationTest(this, segment) < 0;
        }

        public bool IsLeftOf(LineSegment segment)
        {
            return PointOrientationTest(this, segment) > 0;
        }

        public static Fraction PointOrientationTest(Point point, LineSegment segment)
        {
            var p0 = point;
            var p1 = segment.StartPoint;
            var p2 = segment.EndPoint;

            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
        }

        public bool IsRightOf(Point that)
        {
            return _x > that.X;
        }

        public static Point Midpoint(Point a, Point b)
        {
            return new Point(
                (a.X + b.X) / 2,
                (a.Y + b.Y) / 2
            );
        }

        public static double Slope(Point from, Point to)
        {
            return (to.Y - from.Y) / (to.X - from.X);
        }

        public Point Translate(long dx, long dy)
        {
            return new Point(_x + dx, _y + dy);
        }

        public Point Translate(int dx, int dy)
        {
            return new Point(_x + dx, _y + dy);
        }

        public Point Translate(double dx, double dy)
        {
            return new Point(_x + dx, _y + dy);
        }

        public PointFr ToPointFraction()
        {
            return new PointFr(new Fraction(_x), new Fraction(_y));
        }

        public override string ToString()
        {
            return $"({X:0.00}, {Y:0.00})";
        }

        public static double Angle(Point p1, Point p2, Point refp)
        {
            return Math.Atan2(p1.Y - refp.Y, p1.X - refp.X) - Math.Atan2(p2.Y - refp.Y, p2.X - refp.X) * 180 / Math.PI;
        }

        public static implicit operator Point(System.Windows.Point value)
        {
            return new Point(value.X, value.Y);
        }

        public static implicit operator System.Windows.Point(Point value)
        {
            return new System.Windows.Point(value.X, value.Y);
        }
    }
}
