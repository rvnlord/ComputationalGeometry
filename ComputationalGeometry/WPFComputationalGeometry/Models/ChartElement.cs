using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFDemo.Models
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

                if (w == 0 && w1 == 0 && w2 == 0)
                    return "(układ nieoznaczony)";
                if (w == 0 && (w1 != 0 || w2 != 0))
                    return "(układ sprzeczny)";

                var a = Math.Round(w1 / w, 2);
                var b = Math.Round(w2 / w, 2);

                var sb = new StringBuilder();
                sb.Append("y = ");
                if (a < 0)
                    sb.Append("-");

                if (Math.Abs(a) == 1)
                    sb.Append("x ");
                else if (a != 0)
                    sb.Append($"{Math.Abs(a)}" + "x ");
                
                if (b < 0 && a != 0)
                    sb.Append("- ");
                else if (b < 0 && a == 0)
                    sb.Append("-");
                else if (b > 0 && a != 0)
                    sb.Append("+ ");

                if (b != 0 || (b == 0 && a == 0))
                    sb.Append($"{Math.Abs(b)}"); //:0.00

                return sb.ToString();
            }
        }

        public string ElementTypeName => ElementType == ElementType.Point
            ? "Punkt"
            : ElementType == ElementType.Polygon 
                ? "Wielokąt" 
                : "Odcinek";

        public static ObservableCollection<ChartElement> AllChartElements { get; set; }
    }

    public enum ElementType
    {
        Point,
        Line,
        Polygon
    }
}
