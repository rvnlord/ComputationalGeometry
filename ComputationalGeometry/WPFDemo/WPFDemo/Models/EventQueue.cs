using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using C5;

namespace WPFDemo.Models
{
    public class EventQueue
    {
        private readonly TreeDictionary<PointFr, C5.LinkedList<EventPoint>> _events; // Posortowana mapa punktów i odpowiadających im zdarzeń

        public EventQueue(C5.HashSet<LineSegmentFr> segments, SweepLine sweepLine) // Tworzy nową kolejkę zdarzeń przy użyciu zestawu odcinków
        {
            if (segments.Count <= 0)
                throw new ArgumentException($"Kolekcja {nameof(segments)} nie może być pusta");

            _events = new TreeDictionary<PointFr, C5.LinkedList<EventPoint>>(new PointFrXThenYComparer());

            var minY = Fraction.PositiveInfinity;
            var maxY = Fraction.NegativeInfinity;
            var minDeltaX = Fraction.PositiveInfinity;
            var xs = new TreeSet<Fraction>();

            foreach (var s in segments)
            {
                xs.Add(s.StartPoint.X);
                xs.Add(s.EndPoint.X);
                if (s.MinY < minY)
                    minY = s.MinY;
                if (s.MaxY > maxY)
                    maxY = s.MaxY;

                Insert(s.StartPoint, new EventPoint(s.StartPoint, EventPointType.Left, s, sweepLine));
                Insert(s.EndPoint, new EventPoint(s.EndPoint, EventPointType.Right, s, sweepLine));
            }

            var xsArray = xs.ToArray();
            for (var i = 1; i < xsArray.Length; i++)
            {
                var tempDeltaX = xsArray[i] - xsArray[i - 1];
                if (tempDeltaX < minDeltaX)
                    minDeltaX = tempDeltaX;
            }

            var deltaY = maxY - minY;
            var slope = deltaY / minDeltaX * -1; // * 1000

            sweepLine.SweepLineValue = new LineFr(PointFr.Origin, new PointFr(0, 1)); // slope, PointFr.Origin
            sweepLine.EventQueue = this; // Porzuciłem pomysł dynamicznego zmianu nachylenia miotły, ponieważ wymagało to obliczania dużych wartości przecięć z osiami wykresu dla prostej przy użyciu Ułamków (StackOverflowException)
        }

        public bool isEmpty()
        {
            return _events.Count <= 0;
        }

        public void Insert(PointFr p, EventPoint ep) // Wstawia zdarzenie ep w punkcie p do bieżącej kolejki zdarzeń zdarzenia Right są dodawane na początku, pozostałe na końcu
        {
            var existing = new C5.LinkedList<EventPoint>();
            if (_events.Exists(kvp => kvp.Key.Equals(p)))
            {
                existing = _events[p];
                _events.Remove(p);
            }
            
            if (ep.Type == EventPointType.Right) // Zdarzenia 'Right' powinny byc na początku listy
                existing.Insert(0, ep);
            else
                existing.Add(ep);
            _events.Add(p, existing);
        }
        
        public C5.HashSet<EventPoint> DeleteMin() // Pobierz i zwróć najmniejszy element kolejki
        {
            if (isEmpty()) 
                throw new Exception("Kolejka jest pusta");

            var entry =  _events.DeleteMin();
            var hs = new C5.HashSet<EventPoint>();
            hs.AddAll(entry.Value);
            return hs;
        }

        public override string ToString()
        {
            return _events.ToString();
        }
    }
}
