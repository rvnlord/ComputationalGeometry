using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Models
{
    public class ChartElement
    {
        public ElementType ElementType { get; set; }
        public double XStart { get; set; }
        public double YStart { get; set; }
        public double? XEnd { get; set; }
        public double? YEnd { get; set; }
        public double XStartGridRelative { get; set; }
        public double YStartGridRelative { get; set; }
        public double? XEndGridRelative { get; set; }
        public double? YEndGridRelative { get; set; }
        public UIElement PhysicalRepresentation { get; set; }

        public List<Point> Vertices { get; set; } = new List<Point>();
        public List<Point> VerticesGridRelative { get; set; } = new List<Point>();

        public string XStartGridRelativeFormatted => $"{XStartGridRelative:0.00}";
        public string YStartGridRelativeFormatted => $"{YStartGridRelative:0.00}";
        public string XEndGridRelativeFormatted => $"{XEndGridRelative:0.00}";
        public string YEndGridRelativeFormatted => $"{YEndGridRelative:0.00}";

        public string Equation
        {
            get
            {
                if (XEndGridRelative == null || YEndGridRelative == null)
                    return "y = " + YStartGridRelative;

                var w = (double) (XStartGridRelative * 1 - XEndGridRelative * 1); 
                var w1 = (double) (YStartGridRelative * 1 - YEndGridRelative * 1);
                var w2 = (double) (XStartGridRelative * YEndGridRelative - XEndGridRelative * YStartGridRelative);

                if (w.Eq(0) && w1.Eq(0) && w2.Eq(0))
                    return "(system of indefinite equations)";
                if (w.Eq(0) && (w1.Eq(0) || !w2.Eq(0)))
                    return "(system of conflicting equations)";

                var a = Math.Round(w1 / w, 2);
                var b = Math.Round(w2 / w, 2);

                var sb = new StringBuilder();
                sb.Append("y = ");
                if (a < 0)
                    sb.Append("-");

                if (Math.Abs(a).Eq(1))
                    sb.Append("x ");
                else if (!a.Eq(0))
                    sb.Append($"{Math.Abs(a)}" + "x ");
                
                if (b < 0 && !a.Eq(0))
                    sb.Append("- ");
                else if (b < 0 && a.Eq(0))
                    sb.Append("-");
                else if (b > 0 && !a.Eq(0))
                    sb.Append("+ ");

                if (!b.Eq(0) || b.Eq(0) && a.Eq(0))
                    sb.Append($"{Math.Abs(b)}"); //:0.00

                return sb.ToString();
            }
        }

        public string ElementTypeName => ElementType == ElementType.Point
            ? "Point"
            : ElementType == ElementType.Polygon 
                ? "Polygon" 
                : "Segment";

        public static ObservableCollection<ChartElement> AllChartElements { get; set; }
    }

    public enum ElementType
    {
        Point,
        Line,
        Polygon
    }
}
