using System;
using System.Text;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Models
{
    public struct Line
    {
        public double Slope { get; set; }
        public double Constant { get; set; }

        public Line(double slope, double constant)
        {
            if (double.IsNaN(slope) || double.IsNaN(constant))
                throw new ArgumentException("Slope (Nachylenie) i Constant (Stała) nie mogą przyjmować wartości NaN");

            Slope = slope;
            Constant = constant;
        }

        public Line(double slope, Point p)
        {
            if (double.IsNaN(slope))
                throw new ArgumentException("Slope (Nachylenie) nie może przyjmować wartości NaN");

            Slope = double.IsInfinity(slope) ? double.PositiveInfinity : slope;
            Constant = CalculateConstant(p, slope);
        }

        public Line(Point p1, Point p2)
        {
            if (p1.Equals(p2))
                throw new ArgumentException("Nie można utworzyć linii dla dwóch takich samych punktów");

            Slope = CalculateSlope(p1, p2);
            Constant = CalculateConstant(p1, Slope);
        }

        public double Angle(bool asDegrees = true)
        {
            var radians = Math.Atan(Slope);
            if (asDegrees)
            {
                var degrees = radians.ToDegrees();
                return degrees < 0.0 ? 180.0 + degrees : degrees;
            }
            return radians < 0.0 ? Math.PI + radians : radians;
        }

        private static double CalculateConstant(Point p, double slope) // Constant = (-Slope* p.x) + p.y
        {
            if (double.IsInfinity(slope)) // IsVertical()
                return p.X;
            return -slope * p.X + p.Y;
        }

        private static double CalculateSlope(Point p1, Point p2) // Slope = (p2.y - p1.y) / (p2.x - p1.x)
        {
            if (p1.X.Equals(p2.X))
                return double.PositiveInfinity;
            return (p2.Y - p1.Y) / (p2.X - p1.X);
        }

        public bool Contains(Point p)
        {
            if (IsVertical())
                return Constant.Equals(p.X);
            return Math.Abs(Slope * p.X + Constant - p.Y) < DoubleExtensions.TOLERANCE;
        }

        public override bool Equals(object o)
        {
            if (Equals(this, o))
                return true;
            if (o == null || GetType() != o.GetType())
                return false;
            var that = (Line)o;
            return Math.Abs(Slope - that.Slope) < DoubleExtensions.TOLERANCE && Math.Abs(Constant - that.Constant) < DoubleExtensions.TOLERANCE;
        }

        public static Line Horizontal(double y)
        {
            var p = new Point(0, y);
            return new Line(p, p.Translate(1, 0));
        }

        public Point? Intersection(Line that)
        {
            if (Math.Abs(Slope - that.Slope) < DoubleExtensions.TOLERANCE)
                return null;
            var xIntN = XIntercept(that);
            if (xIntN == null)
                return null;
            var xInt = (double)xIntN;

            if (IsVertical())
            {
                var yIntN = that.YIntercept(xInt);
                if (yIntN == null)
                    return null;
                var yInt = (double) yIntN;
                return new Point(xInt, yInt);
            }
            else
            {
                var yIntN = YIntercept(xInt);
                if (yIntN == null)
                    return null;
                var yInt = (double)yIntN;
                return new Point(xInt, yInt);
            }
        }

        public Point? Intersection(LineSegment segment)
        {
            return segment.Intersection(this);
        }

        public bool Intersects(Line that)
        {
            return Math.Abs(Slope - that.Slope) > DoubleExtensions.TOLERANCE;
        }

        public bool Intersects(LineSegment that)
        {
            return that.Intersects(this);
        }

        public bool IsAxisAligned()
        {
            return IsHorizontal() || IsVertical();
        }

        public bool IsHorizontal()
        {
            return Math.Abs(Slope) < DoubleExtensions.TOLERANCE;
        }

        public bool isParallelTo(Line that)
        {
            return Math.Abs(Slope - that.Slope) < DoubleExtensions.TOLERANCE;
        }

        public bool isPerpendicularTo(Line that)
        {
            if (IsVertical())
                return that.IsHorizontal();
            if (that.IsVertical())
                return IsHorizontal();
            return Math.Abs(Slope * that.Slope - (-1)) < DoubleExtensions.TOLERANCE;
        }

        public bool IsVertical()
        {
            return double.IsInfinity(Slope);
        }

        public Line PerpendicularLine(Point p)
        {
            if (IsHorizontal())
                return Vertical(p.X);
            if (IsVertical())
                return Horizontal(p.Y);
            var newSlope = 1 / Slope;
            return new Line(newSlope, p);
        }

        public double Tangent(Line that) // tan(theta) = (m1 - m2)/(1 + (m1*m2))
        {
            if (isParallelTo(that))
                return 0;
            if (IsVertical())
            {
                if (that.IsHorizontal())
                    return -1;
                return 1 / Slope;
            }
            if (that.IsVertical())
            {
                if (that.IsHorizontal())
                    return -1;
                return Slope;
            }
            if (isPerpendicularTo(that))
                return -1;
            return (Slope - that.Slope) / (1 + Slope * that.Slope);
        }

        public static Line Vertical(double x)
        {
            var p = new Point(x, 0);
            return new Line(p, p.Translate(0, 1));
        }

        public override string ToString()
        {
            if (IsVertical())
                return "x = " + Constant;

            var sb = new StringBuilder("f(x) -> ");

            if (Math.Abs(Slope - (-1)) < DoubleExtensions.TOLERANCE) // Slope
                sb.Append("-x");
            else if (Math.Abs(Slope - 1) < DoubleExtensions.TOLERANCE)
                sb.Append("x");
            else if (Math.Abs(Slope) > DoubleExtensions.TOLERANCE)
                sb.Append(Slope).Append("*x");

            if (Math.Abs(Constant) > DoubleExtensions.TOLERANCE) // Constant
            {
                if (IsVertical() || IsHorizontal())
                    sb.Append(Constant);
                else if (Constant >= 0)
                    sb.Append(" + ").Append(Constant);
                else
                    sb.Append(" - ").Append(Math.Abs(Constant));
            }
            else if (IsHorizontal())
                sb.Append("0");

            return sb.ToString();
        }

        public double? XIntercept() // -constant / slope
        {
            if (IsHorizontal())
                return null;
            if (IsVertical())
                return Constant;
            return -Constant / Slope;
        }

        private double? XIntercept(Line that)  // y = slope* x + constant: (b2 - b1) / (m1 - m2)
        {
            if (IsVertical())
                return Constant;
            if (that.IsVertical())
                return that.Constant;
            return (that.Constant - Constant) / (Slope - that.Slope);
        }

        public double? YIntercept()
        {
            return IsVertical() ? (double?)null : Constant;
        }

        private double? YIntercept(double? xInt) // (Slope * xInt) + Constant
        {
            if (xInt == null)
                return null;
            return Slope * xInt + Constant;
        }

        public override int GetHashCode()
        {
            return (Slope.GetHashCode() * 23) ^ (Constant.GetHashCode() * 37);
        }
    }
}
