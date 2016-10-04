using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.Models
{
    public class CircleFr
    {
        public PointFr Center { get; set; }
        public Fraction Radius { get; set; }

        public CircleFr(PointFr p, Fraction r)
        {
            Center = p;
            Radius = r;
        }
    }
}
