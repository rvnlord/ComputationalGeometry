using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml.Linq;

namespace WPFDemo.Models
{
    public static class ExtensionMethods
    {
        public const double EPSILON = 0.00001;

        public static T NextToTop<T>(this Stack<T> s)
        {
            var top = s.Pop();
            var nextToTop = s.Peek();
            s.Push(top);

            return nextToTop;
        }

        public static double ToDegrees(this double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        public static double ToRadians(this double degrees)
        {
            return Math.PI * degrees / 180.0;
        }

        public static bool EqualsStrict(this Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) < EPSILON && Math.Abs(p1.Y - p2.Y) < EPSILON;
        }

        public static bool EqualsStrict(this double d1, double d2)
        {
            return Math.Abs(d1 - d2) < EPSILON;
        }

        public static bool EqualsStrict(this double? d1, double d2)
        {
            if (d1 == null)
                return false;

            return Math.Abs((double)d1 - d2) < EPSILON;
        }

        public static bool EqualsStrict(this double d1, double? d2)
        {
            if (d2 == null)
                return false;

            return Math.Abs(d1 - (double)d2) < EPSILON;
        }

        public static bool EqualsStrict(this double? d1, double? d2)
        {
            if (d1 == null && d2 == null)
                return true;
            if (d1 == null || d2 == null)
                return false;

            return Math.Abs((double)d1 - (double)d2) < EPSILON;
        }

        public static string ToStringWithDeclaration(this XDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        public static double Distance(this Point p1, Point that)
        {
            var dX = p1.X - that.X;
            var dY = p1.Y- that.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }
    }
}
