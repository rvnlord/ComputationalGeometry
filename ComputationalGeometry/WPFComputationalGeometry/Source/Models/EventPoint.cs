using System;

namespace WPFComputationalGeometry.Source.Models
{
    public class EventPoint : IComparable<EventPoint>
    {
        public PointFr PointValue { get; set; }
        public EventPointType Type { get; set; }
        public LineSegmentFr[] CrossingSegments { get; set; }
        public LineSegmentFr? Segment { get; set; }
        public SweepLine SweepLine { get; set; }

        public EventPoint(PointFr value, EventPointType type, LineSegmentFr[] crossingSegments, SweepLine sweepLine)
        {
            PointValue = value;
            Type = type;
            CrossingSegments = crossingSegments;
            SweepLine = sweepLine;
        }

        public EventPoint(PointFr value, EventPointType type, LineSegmentFr? segment, SweepLine sweepLine)
        {
            PointValue = value;
            Type = type;
            Segment = segment;
            SweepLine = sweepLine;
        }

        public int CompareTo(EventPoint eventPoint2)
        {
            var ep1 = this;
            var ep2 = eventPoint2;

            if (ReferenceEquals(ep1, ep2) || ep1.Equals(ep2)) // Takie same zdarzenia
                return 0;
            
            var ip1Nullable = SweepLine.Intersection(ep1); // Punkty przecięcia z miotłą
            var ip2Nullable = SweepLine.Intersection(ep2);
            if (ip1Nullable == null && ip2Nullable == null)
                return 0;
            if (ip1Nullable == null)
                return -1;
            if (ip2Nullable == null)
                return 1;

            var ip1 = (PointFr) ip1Nullable;
            var ip2 = (PointFr) ip2Nullable;
            
            var deltaY = ip1.Y - ip2.Y; // różnica pomiędzy miejscami zdarzeń (współrzędna y)

            if (deltaY != 0) // jeśli miotła jest przecięta w dwóch różnych miejscach
                return deltaY < 0 ? -1 : 1; // zdarzenie o niższej współrzędnej y będzie pierwsze

            if (ep1.Segment == null || ep2.Segment == null)
                throw new ArgumentException("Wartość ep1.Segment i ep2.Segment nie może być pusta");

            var ep1Segment = (LineSegmentFr) ep1.Segment;
            var ep2Segment = (LineSegmentFr) ep2.Segment;

            var ep1Slope = ep1Segment.Line.Slope;
            var ep2Slope = ep2Segment.Line.Slope;
            
            if (ep1Slope != ep2Slope) // Jeśli odcinki mają różne nachylenia, pierwszy będzie ten o mniejszym
            {
                if (SweepLine.IsBefore)
                    return ep1Slope > ep2Slope ? -1 : 1;
                return ep1Slope > ep2Slope ? 1 : -1;
            }

            var deltaXP1 = ep1Segment.StartPoint.X - ep2Segment.StartPoint.X; // Sprawdź czy punkty najbardziej po lewej stronie się różnią
            if (deltaXP1 != 0)
                return deltaXP1 < 0 ? -1 : 1;
            
            var deltaXP2 = ep1Segment.EndPoint.X - ep2Segment.EndPoint.X; // Jeżeli punkty po lewej stronie są takie same po prawej muszą być różne
            return deltaXP2 < 0 ? -1 : 1;
        }

        public override bool Equals(object o)
        {
            if (this == o)
                return true;
            if (o == null || GetType() != o.GetType())
                return false;

            var eventPoint2 = (EventPoint)o;
            if ((Type == EventPointType.Intersection && eventPoint2.Type != EventPointType.Intersection) ||
                (Type != EventPointType.Intersection && eventPoint2.Type == EventPointType.Intersection))
                return false;
            if (Type == EventPointType.Intersection && eventPoint2.Type == EventPointType.Intersection)
                return PointValue.Equals(eventPoint2.PointValue);
            return Segment.Equals(eventPoint2.Segment);
        }

        public override int GetHashCode()
        {
            return Type == EventPointType.Intersection ? PointValue.GetHashCode() : Segment.GetHashCode();
        }

        public override string ToString()
        {
            return "{" + $"{Enum.GetName(Type.GetType(), Type)}, {PointValue}, {Segment}" + "}";
        }
    }

    public enum EventPointType
    {
        Left,
        Right,
        Intersection
    }
}
