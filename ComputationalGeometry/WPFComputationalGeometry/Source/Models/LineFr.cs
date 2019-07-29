using System;
using System.Text;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Models
{
    public struct LineFr
    {
        public Fraction Slope { get; set; }
        public Fraction Constant { get; set; }

        public LineFr(Fraction slope, Fraction constant)
        {
            if (slope.IsNaN() || constant.IsNaN())
                throw new ArgumentException("Slope (Nachylenie) i Constant (Stała) nie mogą przyjmować wartości NaN");

            Slope = slope;
            Constant = constant;
        }
        
        public LineFr(Fraction slope, PointFr p)
        {
            if (slope.IsNaN())
                throw new ArgumentException("Slope (Nachylenie) nie może przyjmować wartości NaN");

            Slope = slope.IsInfinity() ? Fraction.PositiveInfinity : slope;
            Constant = CalculateConstant(p, slope);
        }
        
        public LineFr(PointFr p1, PointFr p2)
        {
            if (p1.Equals(p2))
                throw new ArgumentException("Nie można utworzyć linii dla dwóch takich samych punktów");

            Slope = CalculateSlope(p1, p2);
            Constant = CalculateConstant(p1, Slope);
        }
        
        public double Angle(bool asDegrees = true)
        {
            var radians = Math.Atan(Slope.ToDouble());
            if (asDegrees)
            {
                var degrees = radians.ToDegrees();
                return degrees < 0.0 ? 180.0 + degrees : degrees;
            }
            return radians < 0.0 ? Math.PI + radians : radians;
        }
        
        private static Fraction CalculateConstant(PointFr p, Fraction slope) // Constant = (-Slope* p.x) + p.y
        {
            if (slope.IsInfinity()) // IsVertical()
                return p.X;
            return -slope * p.X + p.Y;
        }
       
        private static Fraction CalculateSlope(PointFr p1, PointFr p2) // Slope = (p2.y - p1.y) / (p2.x - p1.x)
        {
            if (p1.X.Equals(p2.X))
                return Fraction.PositiveInfinity;
            return (p2.Y - p1.Y) / (p2.X - p1.X);
        }
        
        public bool Contains(PointFr p)
        {
            if (IsVertical())
                return Constant.Equals(p.X);
            return Slope * p.X + Constant == p.Y;
        }
        
        public override bool Equals(object o)
        {
            if (Equals(this, o))
                return true;
            if (o == null || GetType() != o.GetType())
                return false;
            var that = (LineFr)o;
            return Slope == that.Slope && Constant == that.Constant;
        }
        
        public static LineFr Horizontal(Fraction y)
        {
            var p = new PointFr(Fraction.Zero, y);
            return new LineFr(p, p.Translate(1, 0));
        }
        
        public PointFr? Intersection(LineFr that)
        {
            if (Slope == that.Slope)
                return null;
            var xInt = XIntercept(that);
            if (IsVertical())
                return new PointFr(xInt, that.YIntercept(xInt));
            return new PointFr(xInt, YIntercept(xInt));
        }
        
        public PointFr? Intersection(LineSegmentFr segment)
        {
            return segment.Intersection(this);
        }
        
        public bool Intersects(LineFr that)
        {
            return Slope != that.Slope;
        }
        
        public bool Intersects(LineSegmentFr that)
        {
            return that.Intersects(this);
        }
        
        public bool IsAxisAligned()
        {
            return IsHorizontal() || IsVertical();
        }
        
        public bool IsHorizontal()
        {
            return Slope == Fraction.Zero;
        }
        
        public bool isParallelTo(LineFr that)
        {
            return Slope == that.Slope;
        }
        
        public bool isPerpendicularTo(LineFr that)
        {
            if (IsVertical())
                return that.IsHorizontal();
            if (that.IsVertical())
                return IsHorizontal();
            return Slope * that.Slope == -1;
        }
        
        public bool IsVertical()
        {
            return Slope.IsInfinity();
        }

        public LineFr PerpendicularLine(PointFr p)
        {
            if (IsHorizontal())
                return Vertical(p.X);
            if (IsVertical())
                return Horizontal(p.Y);
            var newSlope = Slope.Inverse();
            return new LineFr(newSlope, p);
        }
        
        public Fraction Tangent(LineFr that) // tan(theta) = (m1 - m2)/(1 + (m1*m2))
        {
            if (isParallelTo(that))
                return Fraction.Zero;
            if (IsVertical())
            {
                if (that.IsHorizontal())
                    return -1;
                return Slope.Inverse();
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
        
        public static LineFr Vertical(Fraction x)
        {
            var p = new PointFr(x, Fraction.Zero);
            return new LineFr(p, p.Translate(0, 1));
        }
        
        public override string ToString()
        {
            if (IsVertical())
                return "x = " + Constant;

            var sb = new StringBuilder("f(x) -> ");
            
            if (Slope == -1) // Slope
                sb.Append("-x");
            else if (Slope == 1)
                sb.Append("x");
            else if (Slope != Fraction.Zero)
                sb.Append(Slope).Append("*x");
           
            if (Constant != Fraction.Zero) // Constant
            {
                if (IsVertical() || IsHorizontal())
                    sb.Append(Constant);
                else if (Constant >= 0)
                    sb.Append(" + ").Append(Constant);
                else
                    sb.Append(" - ").Append(Constant.Abs());
            }
            else if (IsHorizontal())
                sb.Append("0");

            return sb.ToString();
        }
        
        public Fraction XIntercept() // -constant / slope
        {
            if (IsHorizontal())
                return null;
            if (IsVertical())
                return Constant;
            return -Constant / Slope;
        }
        
        private Fraction XIntercept(LineFr that)  // y = slope* x + constant: (b2 - b1) / (m1 - m2)
        {
            if (IsVertical())
                return Constant;
            if (that.IsVertical())
                return that.Constant;
            return (that.Constant - Constant) / (Slope - that.Slope);
        }
        
        public Fraction YIntercept()
        {
            return IsVertical() ? null : Constant;
        }
        
        private Fraction YIntercept(Fraction xInt) // (Slope * xInt) + Constant
        {
            return Slope * xInt + Constant;
        }

        public override int GetHashCode()
        {
            return (Slope.GetHashCode() * 23) ^ (Constant.GetHashCode() * 37);
        }
    }
}
