using System;
using System.Collections.Generic;
using System.Windows;

namespace WPFDemo.Models
{
    public class EventPointXThenYComparer : Comparer<EventPoint>
    {
        public override int Compare(EventPoint p1, EventPoint p2)
        {
            if (p1 == null && p2 == null)
                return 0;
            if (p1 == null)
                return -1;
            if (p2 == null)
                return 1;

            var dX = p1.PointValue.X.CompareTo(p2.PointValue.X);
            return dX != 0 ? dX : p1.PointValue.Y.CompareTo(p2.PointValue.Y);
        }
    }
}
