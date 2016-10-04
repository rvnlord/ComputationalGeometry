using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.Models
{
    public class Circle
    {
        public Point Center { get; set; }
        public double Radius { get; set; }

        public Circle(Point p, double r)
        {
            Center = p;
            Radius = r;
        }
    }
}
