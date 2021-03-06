﻿using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Models.Comparers
{
    public class PointFrXThenYComparer : Comparer<PointFr>
    {
        public override int Compare(PointFr p1, PointFr p2)
        {
            var dX = p1.X.CompareTo(p2.X);
            return dX != 0 ? dX : p1.Y.CompareTo(p2.Y);
        }
    }
}
