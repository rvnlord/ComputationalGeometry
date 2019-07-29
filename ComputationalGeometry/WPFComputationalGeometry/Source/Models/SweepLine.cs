using System;
using System.Collections.Generic;
using System.Linq;
using C5;

namespace WPFComputationalGeometry.Source.Models
{
    public class SweepLine
    {
        private readonly Dictionary<PointFr, C5.HashSet<EventPoint>> _intersections; // Mapa punktów porzecięć i ich zdarzeń
        
        public PointFr? CurrentSweepPoint { get; private set; }

        public Dictionary<PointFr, LineFr> OldSweepLineValues { get; } = new Dictionary<PointFr, LineFr>();

        public LineFr? SweepLineValue { get; set; }

        public EventQueue EventQueue { get; set; }

        public Dictionary<PointFr, C5.HashSet<LineSegmentFr>> Intersections // Punkty przecięć z przechodzącymi przez nie liniami
        {
            get
            {
                var segments = new Dictionary<PointFr, C5.HashSet<LineSegmentFr>>();
                foreach (var entrySet in _intersections)
                {
                    var set = new C5.HashSet<LineSegmentFr>();
                    foreach (var ep in entrySet.Value)
                    {
                        if (ep.Segment != null)
                            set.Add((LineSegmentFr)ep.Segment);
                    }
                    segments.Add(entrySet.Key, set);
                }
                return segments;
            }
        }

        public bool IsBefore { get; set; }

        public TreeSet<EventPoint> EventPoints { get; }

        public SweepLine()
        {
            EventPoints = new TreeSet<EventPoint>();
            _intersections = new Dictionary<PointFr, C5.HashSet<EventPoint>>();
            SweepLineValue = null;
            CurrentSweepPoint = null;
            IsBefore = true;
        }

        public EventPoint Above(EventPoint ep) // Zdarzenie nad obecnym
        {
            EventPoint above;
            EventPoints.TrySuccessor(ep, out above);
            return above;
        }

        public EventPoint Below(EventPoint ep) // Zdarzenie pod obecnym
        {
            EventPoint below;
            EventPoints.TryPredecessor(ep, out below);
            return below;
        }

        private void CheckIntersection(EventPoint ep1, EventPoint ep2) // Sprawdza czy istnieje przecięcie pomiędzy dwoma zdarzeniami
        {
            if (CurrentSweepPoint == null)
                return;
            var currentSweepPoint = (PointFr)CurrentSweepPoint;
            if (ep1 == null || ep2 == null || ep1.Type == EventPointType.Intersection || ep2.Type == EventPointType.Intersection) // Zatrzymaj jeżeli conajmniej jedno ze zdarzeń jest puste lub jest przecięciem
                return;
            if (ep2.Segment == null || ep1.Segment == null)
                throw new ArgumentException("Wartość ep1.Segment i ep2.Segment nie może być pusta");

            var pNullable = ((LineSegmentFr)ep1.Segment).Intersection((LineSegmentFr)ep2.Segment); // Znajdź punkt przecięcia pomiędzy liniami zdarzeń           
            if (pNullable == null)
                return;
            var p = (PointFr)pNullable;

            var existing = new C5.HashSet<EventPoint>();
            if (_intersections.ContainsKey(p))
            {
                existing = _intersections[p]; // Dodaj przecięcie do słownika
                _intersections.Remove(p);
            }

            existing.Add(ep1);
            existing.Add(ep2);
            _intersections.Add(p, existing);

            if (SweepLineValue == null)
                throw new ArgumentException("Wartość miotły nie może być pusta");

            var sweepLineValue = (LineFr) SweepLineValue;
            if (p.IsRightOf(sweepLineValue) || (sweepLineValue.Contains(p) && p.Y > currentSweepPoint.Y)) // Jeśli przecięcie jest po prawej stronie miotły lub na miotle i powyzej obecnego zdarzenia, dodaj do kolejki
            {
                var intersection = new EventPoint(p, EventPointType.Intersection, (LineSegmentFr?)null, this);
                EventQueue.Insert(p, intersection);
            }
        }

        protected List<EventPoint> Events()
        {
            return EventPoints.ToList();
        }

        public void HandleEventPoints(C5.HashSet<EventPoint> eventPoints)
        {
            if (eventPoints.Count == 0)
                return;

            var array = eventPoints.ToArray();
            MoveSweepLineTo(array[0]);
            
            if (eventPoints.Count > 1) // Jeśli w zestawie jest więcej niż jeden element, muszą się one ze sobą przecinać
                for (var i = 0; i < array.Length - 1; i++)
                    for (var j = i + 1; j < array.Length; j++)
                        CheckIntersection(array[i], array[j]);
            
            foreach (var ep in eventPoints)
                HandleEventPoint(ep);
        }
        
        private void HandleEventPoint(EventPoint eventPoint)
        {
            switch (eventPoint.Type)
            {
                case EventPointType.Left:
                    IsBefore = false;
                    Insert(eventPoint);
                    CheckIntersection(eventPoint, Above(eventPoint));
                    CheckIntersection(eventPoint, Below(eventPoint));
                    break;

                case EventPointType.Right:
                    IsBefore = true;
                    Remove(eventPoint);
                    CheckIntersection(Above(eventPoint), Below(eventPoint));
                    break;

                case EventPointType.Intersection:
                    IsBefore = true;
                    var set = _intersections[eventPoint.PointValue];
                    var toInsert = new Stack<EventPoint>();
                    foreach (var ep in set.Where(Remove)) // jeżeli zdarzenie nie zostało usunięte, trzeba je później dodać
                        toInsert.Push(ep);
                    IsBefore = false;

                    while (toInsert.Count > 0) // Wstaw wszystkie zdarzenia, które zostały usunięte
                    {
                        var ep = toInsert.Pop();
                        Insert(ep);
                        CheckIntersection(ep, Above(ep));
                        CheckIntersection(ep, Below(ep));
                    }
                    break;
            }
        }
        
        public bool Insert(EventPoint ep) // Dodaj zdarzenie i zwróć true jeżeli zostało poprawnie dodane
        {
            return EventPoints.Add(ep);
        }
        

        public PointFr? Intersection(EventPoint ep) // Przecięcie z miotłą lub null jeśli nie ma
        {
            if (ep.Type == EventPointType.Intersection)
                return ep.PointValue;
            if (ep.Segment == null || SweepLineValue == null)
                throw new ArgumentException("Wartość ep.Segment i SweepLineValue nie może być pusta");
            return ((LineSegmentFr)ep.Segment).Intersection((LineFr)SweepLineValue);
        }

        public bool Remove(EventPoint ep)
        {
            return EventPoints.Remove(ep);
        }

        private void MoveSweepLineTo(EventPoint ep) // Przesuń miotłę do nowego punktu zdarzenia
        {
            CurrentSweepPoint = ep.PointValue;
            if (SweepLineValue == null)
                throw new ArgumentException("Wartość SweepLineValue nie może być pusta podczas zmiany stanu miotły");
            SweepLineValue = new LineFr(((LineFr)SweepLineValue).Slope, (PointFr)CurrentSweepPoint);
            
            if (!OldSweepLineValues.ContainsKey((PointFr)CurrentSweepPoint))
                OldSweepLineValues.Add((PointFr)CurrentSweepPoint, (LineFr)SweepLineValue);
        }
    }
}
