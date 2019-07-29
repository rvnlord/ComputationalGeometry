using System;

namespace WPFComputationalGeometry.Source.Models
{
    public struct PointFr
    {
        private Fraction _x;
        private Fraction _y;

        public Fraction X
        {
            get { return _x; }
            set { _x = value; }
        }

        public Fraction Y
        {
            get { return _y; }
            set { _y = value; }
        }

        public static readonly PointFr Origin = new PointFr(0, 0);
        
        public PointFr(long x, long y)
        {
            _x = new Fraction(x, 1);
            _y = new Fraction(y, 1);
        }

        public PointFr(double x, double y)
        {
            _x = new Fraction(x);
            _y = new Fraction(y);
        }

        public PointFr(Fraction x, Fraction y)
        {
            if(x.IsNaN() || y.IsNaN() || x.IsInfinity() || y.IsInfinity())
                throw new ArgumentException("x i y nie może wynosić NaN lub Nieskończoność");
            _x = x;
            _y = y;
        }

        public double Distance(PointFr that)
        {
            var dX = (_x - that.X).ToDouble();
            var dY = (_y - that.Y).ToDouble();
            return Math.Sqrt(dX * dX + dY * dY);
        }
        
        public Fraction DistanceSquared(PointFr that)
        {
            var dX = _x - that.X;
            var dY = _y - that.Y;
            return dX * dX + dY * dY;
        }
        
        public Fraction DistanceXY(PointFr that)
        {
            var dX = (_x - that.X).Abs();
            var dY = (_y - that.Y).Abs();
            return dX + dY;
        }
        
        public override bool Equals(object o)
        {
            if (o == null || GetType() != o.GetType())
                return false;
            var that = (PointFr)o;
            return _x == that.X && _y == that.Y;
        }

        public static bool operator == (PointFr p1, PointFr p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(PointFr p1, PointFr p2)
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

        public bool IsLeftOf(LineFr line)
        {
            return !line.Contains(this) && !IsRightOf(line);
        }

        public bool IsLeftOf(PointFr that)
        {
            return _x < that.X;
        }
        
        public bool IsRightOf(LineFr line)
        {
            if (line.Contains(this))
                return false;
            if (line.IsHorizontal())
                return Y < line.YIntercept();
            if (line.IsVertical())
                return X > line.XIntercept();

            var linePointNullable = line.Intersection(LineFr.Vertical(Fraction.Zero));
            if (linePointNullable == null)
                throw new ArgumentException("Wartość linePoint nie może wynosić null");

            var linePoint = (PointFr) linePointNullable;
            var temp = new LineFr(this, linePoint);

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
        
        public bool IsRightOf(LineSegmentFr segment)
        {
            return PointFrOrientationTest(this, segment) < 0;
        }

        public bool IsLeftOf(LineSegmentFr segment)
        {
            return PointFrOrientationTest(this, segment) > 0;
        }

        public static Fraction PointFrOrientationTest(PointFr point, LineSegmentFr segment)
        {
            var p0 = point;
            var p1 = segment.StartPoint;
            var p2 = segment.EndPoint;

            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
        }

        public bool IsRightOf(PointFr that)
        {
            return _x > that.X;
        }

        public static PointFr Midpoint(PointFr a, PointFr b)
        {
            return new PointFr(
                (a.X + b.X) / 2,
                (a.Y + b.Y) / 2
            );
        }

        public static Fraction Slope(PointFr from, PointFr to)
        {
            return (to.Y - from.Y) / (to.X - from.X);
        }

        public PointFr Translate(long dx, long dy)
        {
            return Translate(new Fraction(dx), new Fraction(dy));
        }
        
        public PointFr Translate(Fraction dx, Fraction dy)
        {
            return new PointFr(_x + dx, _y + dy);
        }

        public Point ToPointDouble()
        {
            return new Point(X.ToDouble(), Y.ToDouble());
        }

        public override string ToString()
        {
            return $"({X.ToDouble():0.00}, {Y.ToDouble():0.00})";
        }

        public static Fraction AngleFr(PointFr p1, PointFr p2, PointFr refp)
        {
            return Fraction.Atan2(p1.Y - refp.Y, p1.X - refp.X) - Fraction.Atan2(p2.Y - refp.Y, p2.X - refp.X) * 180 / Math.PI;
        }

        public static double Angle(PointFr p1, PointFr p2, PointFr refp)
        {
            return Math.Atan2((p1.Y - refp.Y).ToDouble(), (p1.X - refp.X).ToDouble()) - Math.Atan2((p2.Y - refp.Y).ToDouble(), (p2.X - refp.X).ToDouble()) * 180 / Math.PI;
        }
    }
}
