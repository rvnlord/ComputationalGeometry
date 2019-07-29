using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using WPFComputationalGeometry.Source.Common.Extensions;
using WPFComputationalGeometry.Source.Common.Extensions.Collections;

namespace WPFComputationalGeometry.Source.Models
{
    public static class GeometryCalculations
    {
        // < 0 - Po Prawej
        // > 0 - Po Lewej
        // = 0 - Współliniowe
        public static double PointOrientationTest(Point point, LineSegment segment)
        {
            var p0 = point;
            var p1 = segment.StartPoint;
            var p2 = segment.EndPoint;

            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
        } // test bada współliniowość, a nie czy punkt jest zawarty w odcinku

        public static Fraction PointFrOrientationTest(PointFr point, LineSegmentFr segment)
        {
            var p0 = point;
            var p1 = segment.StartPoint;
            var p2 = segment.EndPoint;

            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
        }

        public static bool PointMembershipTest(Point point, LineSegment segment)
        {
            return Math.Abs(PointOrientationTest(point, segment)) < DoubleExtensions.TOLERANCE; // == 0
        }

        public static bool LineRectangularBoundsIntersection(LineSegment segment1, LineSegment segment2)
        {
            var p0 = segment1.StartPoint;
            var p1 = segment1.EndPoint;
            var p2 = segment2.StartPoint;
            var p3 = segment2.EndPoint;

            var r0 = new Point
            {
                X = new[] { p0.X, p1.X }.Min(),
                Y = new[] { p0.Y, p1.Y }.Min()
            };
            var r1 = new Point
            {
                X = new[] { p0.X, p1.X }.Max(),
                Y = new[] { p0.Y, p1.Y }.Max()
            };
            var r2 = new Point
            {
                X = new[] { p2.X, p3.X }.Min(),
                Y = new[] { p2.Y, p3.Y }.Min()
            };
            var r3 = new Point
            {
                X = new[] { p2.X, p3.X }.Max(),
                Y = new[] { p2.Y, p3.Y }.Max()
            };

            return r1.X >= r2.X && r3.X >= r0.X && r1.Y >= r2.Y && r3.Y >= r0.Y;
        }

        public static bool SegmentsIntersection(LineSegment segment1, LineSegment segment2)
        {
            if (!LineRectangularBoundsIntersection(segment1, segment2))
                return false;

            var p0 = segment1.StartPoint;
            var p1 = segment1.EndPoint;
            var p2 = segment2.StartPoint;
            var p3 = segment2.EndPoint;

            return PointOrientationTest(p0, segment2) * PointOrientationTest(p1, segment2) <= 0 &&
                   PointOrientationTest(p2, segment1) * PointOrientationTest(p3, segment1) <= 0;
        }

        public static LineSegment PointsClosestPairIterative(List<Point> points)
        {
            Point? a = null, b = null;
            var queue = new PriorityQueue<Point>(new PointXCoordComparerInversed());
            points.ForEach(p => queue.Add(p));

            var t = new SkipList<Point>(new PointYCoordComparer())
            {
                new Point { X = double.NegativeInfinity, Y = double.NegativeInfinity },
                new Point { X = double.NegativeInfinity, Y = double.PositiveInfinity }
            };

            var current = queue.Peek();
            var firstActive = current;
            var delta = double.PositiveInfinity;

            while (queue.Count > 0)
            {
                var p = current;
                queue.Remove(current);
                if (queue.Count > 0)
                    current = queue.Peek();
                var r = t.Ceiling(p);
                var l = t.Previous(r);

                UpdateDeltaOnRight(ref p, ref r, ref delta, t, ref a, ref b);
                UpdateDeltaOnLeft(ref p, ref l, ref delta, t, ref a, ref b);
                UpdateActivePoints(ref p, ref firstActive, t, delta);

                t.Add(p);
            }

            return new LineSegment(a ?? new Point(), b ?? new Point());
        }

        public static double D(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private static void UpdateDeltaOnRight(ref Point p, ref Point r, ref double delta, SkipList<Point> t, ref Point? a, ref Point? b)
        {
            var i = 0;
            while (r != new Point(double.NegativeInfinity, double.PositiveInfinity) && (i < 4))
            {
                var dist = D(p, r);
                if (dist < delta)
                {
                    delta = dist;
                    a = p;
                    b = r;
                }
                r = t.Next(r);

                if (r == default(Point))
                    break;

                i++;
            }
        }

        private static void UpdateDeltaOnLeft(ref Point p, ref Point l, ref double delta, SkipList<Point> t, ref Point? a, ref Point? b)
        {
            var i = 0;
            while (l != new Point(double.NegativeInfinity, double.NegativeInfinity) && (i < 4))
            {
                var dist = D(p, l);
                if (dist < delta)
                {
                    delta = dist;
                    a = p;
                    b = l;
                }
                l = t.Previous(l);

                if (l == default(Point))
                    break;

                i++;
            }
        }

        private static void UpdateActivePoints(ref Point p, ref Point firstActive, SkipList<Point> t, double delta)
        {
            var q = firstActive;
            while (p.X - q.X > delta && q.X < double.NegativeInfinity)
            {
                firstActive = t.Next(q);
                t.Remove(q);
                q = firstActive;
            }
        }

        public static LineSegment PointsClosestPairRecursive(List<Point> points)
        {
            return PointsClosestPairRecursiveForSorted(points.OrderBy(p => p.X).ToList());
        }

        public static LineSegment PointsClosestPairRecursiveForSorted(List<Point> points)
        {
            var count = points.Count;
            if (count <= 4)
                return PointsClosestPairBruteForce(points);

            var leftByX = points.Take(count / 2).ToList();
            var leftResult = PointsClosestPairRecursiveForSorted(leftByX);

            var rightByX = points.Skip(count / 2).ToList();
            var rightResult = PointsClosestPairRecursiveForSorted(rightByX);

            var result = D(rightResult.StartPoint, rightResult.EndPoint) < D(leftResult.StartPoint, leftResult.EndPoint)
                ? rightResult
                : leftResult;

            var midX = leftByX.Last().X;
            var minDist = result.Length();
            var lowerThanMinDistByX = points.Where(p => Math.Abs(midX - p.X) <= minDist);
            var lowerThanMinDistByY = lowerThanMinDistByX.OrderBy(p => p.Y).ToArray(); // sortowanie po Y tego wyżej

            var last = lowerThanMinDistByY.Length - 1;
            for (var i = 0; i < last; i++)
            {
                var pLower = lowerThanMinDistByY[i];

                for (var j = i + 1; j <= last; j++)
                {
                    var pUpper = lowerThanMinDistByY[j];

                    if (pUpper.Y - pLower.Y >= result.Length())
                        break;

                    if (D(pLower, pUpper) < result.Length())
                        result = new LineSegment(pLower, pUpper);
                }
            }

            return result;
        }

        public static LineSegment PointsClosestPairBruteForce(List<Point> points)
        {
            var n = points.Count;
            //return Enumerable.Range(0, n - 1)
            //    .SelectMany(i => Enumerable.Range(i + 1, n - (i + 1))
            //    .Select(j => new LineSegment(points[i], points[j])))
            //    .OrderBy(seg => D(seg.StartPoint, seg.EndPoint))
            //    .First();

            var result = new LineSegment(points[0], points[1]);
            var minLength = result.Length();

            for (var i = 0; i < n; i++)
                for (var j = i + 1; j < n; j++)
                    if (D(points[i], points[j]) < minLength)
                    {
                        result = new LineSegment(points[i], points[j]);
                        minLength = result.Length();
                    }

            return result;
        }

        public static List<Point> AngularSortDescending(List<Point> points, Point? zeroPoint = null, bool includeZeroPoint = false)
        {
            var o = zeroPoint ?? new Point(0, 0);
            var sortedpoints = points.Where(p => p != o).OrderBy(p => p.Y).ToList();
            var pointsIn34 = sortedpoints.TakeWhile(p => p.Y < o.Y).ToList();
            var pointsIn12 = sortedpoints.SkipWhile(p => p.Y < o.Y).ToList();
            
            pointsIn34.Sort(new PointAngularComparer(o, true));
            pointsIn12.Sort(new PointAngularComparer(o, true));

            var result = new List<Point>(); 
            result.AddRange(pointsIn34);
            result.AddRange(pointsIn12);
            if (includeZeroPoint) result.Add(o);

            return result;
        }

        public static List<PointFr> AngularSortDescending(List<PointFr> points, PointFr? zeroPoint = null, bool includeZeroPoint = false, PointFr? startAt = null)
        {
            var o = zeroPoint ?? new PointFr(0, 0);
            var sortedPoints = new List<PointFr>();
            if (startAt != null) sortedPoints.Add((PointFr)startAt);
            sortedPoints.AddRange(points.Where(p => p != o));
            sortedPoints = sortedPoints.OrderBy(p => p.Y).ToList();
            var pointsIn34 = sortedPoints.TakeWhile(p => p.Y < o.Y).ToList();
            var pointsIn12 = sortedPoints.SkipWhile(p => p.Y < o.Y).ToList();

            pointsIn34.Sort(new PointFrAngularComparer(o, true));
            pointsIn12.Sort(new PointFrAngularComparer(o, true));

            var result = new List<PointFr>();
            result.AddRange(pointsIn34);
            result.AddRange(pointsIn12);
            if (includeZeroPoint) result.Add(o);
            if (startAt != null)
                result = result.SkipWhile(p => p != startAt).Skip(1).Concat(result.TakeWhile(p => p != startAt)).ToList();

            return result;
        }

        public static List<Point> AngularSort(List<Point> points, Point? zeroPoint = null, bool includeZeroPoint = false)
        {
            var o = zeroPoint ?? new Point(0, 0);
            var sortedpoints = points.Where(p => p != o).OrderBy(p => p.Y).ToList();
            var pointsIn34 = sortedpoints.TakeWhile(p => p.Y <= o.Y).ToList();
            var pointsIn12 = sortedpoints.SkipWhile(p => p.Y <= o.Y).ToList();

            pointsIn12.Sort(new PointAngularComparer(o, false));
            pointsIn34.Sort(new PointAngularComparer(o, false));

            var result = new List<Point>();
            if (includeZeroPoint) result.Add(o);
            result.AddRange(pointsIn12);
            result.AddRange(pointsIn34);

            return result;
        }

        public static List<PointFr> AngularSort(List<PointFr> points, PointFr? zeroPoint = null, bool includeZeroPoint = false, PointFr? startAt = null)
        {
            var o = zeroPoint ?? new PointFr(0, 0);
            var sortedPoints = new List<PointFr>();
            if (startAt != null) sortedPoints.Add((PointFr) startAt);
            sortedPoints.AddRange(points.Where(p => p != o));
            sortedPoints = sortedPoints.OrderBy(p => p.Y).ToList();
            var pointsIn34 = sortedPoints.TakeWhile(p => p.Y <= o.Y).ToList();
            var pointsIn12 = sortedPoints.SkipWhile(p => p.Y <= o.Y).ToList();

            pointsIn12.Sort(new PointFrAngularComparer(o, false));
            pointsIn34.Sort(new PointFrAngularComparer(o, false));

            var result = new List<PointFr>();
            if (includeZeroPoint) result.Add(o);
            result.AddRange(pointsIn12);
            result.AddRange(pointsIn34);
            if (startAt != null)
                result = result.SkipWhile(p => p != startAt).Skip(1).Concat(result.TakeWhile(p => p != startAt)).ToList();

            return result;
        }

        public static bool PointIsLessOrEqualTo(Point p1, Point p2, Point o)
        {
            var op1 = new LineSegment(o, p1);
            var op2 = new LineSegment(o, p2);
            var pos = PointOrientationTest(p1, op2); // po lewej stronie > 0

            return pos > 0 || pos.Eq(0) && op1.Length() <= op2.Length();
        }

        public static List<Point> ConvexHullGraham(List<Point> points)
        {
            var pointsByYThenX = points.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            var p0 = pointsByYThenX.First();

            var pn = AngularSort(points.Where(p => p != p0).ToList(), p0)
                .GroupBy(p => Math.Atan2(p.Y - p0.Y, p.X - p0.X))
                .Select(g => g.MaxBy(p => D(p0, p)).First()).ToList();

            var m = pn.Count;
            var s = new Stack<Point>();
            s.Push(p0);
            s.Push(pn[0]);
            s.Push(pn[1]);

            for (var i = 2; i < m; i++)
            {
                while (PointOrientationTest(s.NextToTop(), new LineSegment(s.Peek(), pn[i])) < 0) // po prawej
                    s.Pop();
                s.Push(pn[i]);
            }

            return s.ToList();
        }

        public static List<Point> ConvexHullJarvis(List<Point> points)
        {
            var pointsByYThenX = points.OrderBy(point => point.Y).ThenBy(point => point.X).ToList();
            var d = pointsByYThenX.First();
            var g = pointsByYThenX.Last();
            var s = new Stack<Point>();

            var p = d;
            s.Push(p);
            while (p != g)
            {
                var r = AngularSort(pointsByYThenX, p).First(); // .Where(x => x != p).ToList() niepotrzebne bo punkt środka jest wyrzucony w sortowaniu 
                s.Push(r);
                p = r;
            }

            p = g;
            while (p != d)
            {
                var r = AngularSort(pointsByYThenX.TakeWhile(x => x.Y <= p.Y).ToList(), p).First();
                s.Push(r);
                p = r;
            }
            s.Pop();

            return s.ToList();
        }

        public static List<PointFr> ConvexHullJarvis(List<PointFr> points)
        {
            var pointsByYThenX = points.OrderBy(point => point.Y).ThenBy(point => point.X).ToList();
            var d = pointsByYThenX.First();
            var g = pointsByYThenX.Last();
            var s = new Stack<PointFr>();

            var p = d;
            s.Push(p);
            while (p != g)
            {
                var r = AngularSort(pointsByYThenX, p).First(); 
                s.Push(r);
                p = r;
            }

            p = g;
            while (p != d)
            {
                var r = AngularSort(pointsByYThenX.TakeWhile(x => x.Y <= p.Y).ToList(), p).First();
                s.Push(r);
                p = r;
            }
            s.Pop();

            return s.ToList();
        }

        public static LineSegment FarthestPoints(List<Point> points)
        {
            if (points.Count == 2)
                return new LineSegment(points[0], points[1]);

            var antipodalPairs = ShamosAntipodalPairs(points).ToList();
            return antipodalPairs.MaxBy(pair => Math.Abs(pair.Length())).First();
        }

        public static IEnumerable<LineSegment> ShamosAntipodalPairs(List<Point> points) // Rotating Calipers
        {
            var ch = ConvexHullGraham(points).AsEnumerable().Reverse().ToList(); // clockwise, p0 jako ostatni jeśli bez odwracania

            var m = ch.Count - 1;
            var k = 0;

            while (TriangleSignedArea(ch[m], ch[0], ch[k + 1]) > TriangleSignedArea(ch[m], ch[0], ch[k]))
                k++;

            var i = 0;
            var j = k;

            while (i <= k && j <= m)
            {
                yield return new LineSegment(ch[i], ch[j]);
                while (j < m && TriangleSignedArea(ch[i], ch[i + 1], ch[j + 1]) > TriangleSignedArea(ch[i], ch[i + 1], ch[j]))
                    yield return new LineSegment(ch[i], ch[++j]);
                i++;
            }
        }

        public static double TriangleSignedArea(Point a, Point b, Point c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);
        }

        public static Dictionary<Point, List<LineSegment>> IntersectionsBentleyOttmann(List<LineSegment> segments, out LineSegment[] sweepLines)
        {
            var hsSegments = new C5.HashSet<LineSegmentFr>();
            hsSegments.AddAll(segments.Select(s => new LineSegmentFr(
                new PointFr(new Fraction(s.StartPoint.X), new Fraction(s.StartPoint.Y)),
                new PointFr(new Fraction(s.EndPoint.X), new Fraction(s.EndPoint.Y)), true)).ToList());

            var result = IntersectionsBentleyOttmann(hsSegments, out var sweepLinesFr);

            var endpoints = result.Values.SelectMany(s => s).Select(s => new [] { s.StartPoint, s.EndPoint })
                .SelectMany(p => p).ToList();
            var minY = endpoints.MinBy(p => p.Y).First().Y;
            var maxY = endpoints.MaxBy(p => p.Y).First().Y;

            sweepLines = sweepLinesFr.Keys.Select(key => new LineSegment(
                new Point(key.X.ToDouble(), minY.ToDouble()),
                new Point(key.X.ToDouble(), maxY.ToDouble()))).ToArray();

            return result.ToDictionary(kvp => new Point(kvp.Key.X.ToDouble(), kvp.Key.Y.ToDouble()), 
                kvp => kvp.Value.Select(s => new LineSegment(
                new Point(s.StartPoint.X.ToDouble(), s.StartPoint.Y.ToDouble()),
                new Point(s.EndPoint.X.ToDouble(), s.EndPoint.Y.ToDouble()))).ToList());
        }

        public static Dictionary<Point, List<LineSegment>> IntersectionsBentleyOttmann(List<LineSegment> segments)
        {
            var hsSegments = new C5.HashSet<LineSegmentFr>();
            hsSegments.AddAll(segments.Select(s => new LineSegmentFr(
                new PointFr(new Fraction(s.StartPoint.X), new Fraction(s.StartPoint.Y)),
                new PointFr(new Fraction(s.EndPoint.X), new Fraction(s.EndPoint.Y)), true)).ToList());

            Dictionary<PointFr, LineFr> sweepLinesFr;
            var result = IntersectionsBentleyOttmann(hsSegments, out sweepLinesFr);

            return result.ToDictionary(kvp => new Point(kvp.Key.X.ToDouble(), kvp.Key.Y.ToDouble()),
                kvp => kvp.Value.Select(s => new LineSegment(
                new Point(s.StartPoint.X.ToDouble(), s.StartPoint.Y.ToDouble()),
                new Point(s.EndPoint.X.ToDouble(), s.EndPoint.Y.ToDouble()))).ToList());
        }

        public static List<Point> IntersectionsBentleyOttmannPoints(List<LineSegment> segments)
        {
            return IntersectionsBentleyOttmann(segments).Select(kvp => kvp.Key).ToList();
        }

        public static Dictionary<PointFr, C5.HashSet<LineSegmentFr>> IntersectionsBentleyOttmann(C5.HashSet<LineSegmentFr> segments, out Dictionary<PointFr, LineFr> sweepLines)
        {
            if (segments.Count < 2)
            {
                sweepLines = null;
                return new Dictionary<PointFr, C5.HashSet<LineSegmentFr>>();
            }

            var sweepLine = new SweepLine();
            var eventQueue = new EventQueue(segments, sweepLine);

            while (!eventQueue.isEmpty())
            {
                var eventPoints = eventQueue.DeleteMin();
                sweepLine.HandleEventPoints(eventPoints);
            }

            sweepLines = sweepLine.OldSweepLineValues;
            return sweepLine.Intersections;
        }

        public static IEnumerable<Point> IntersectionsNaive(List<LineSegment> segments)
        {
            var result = IntersectionsNaive(segments.Select(s => new LineSegmentFr(
                new PointFr(new Fraction(s.StartPoint.X), new Fraction(s.StartPoint.Y)),
                new PointFr(new Fraction(s.EndPoint.X), new Fraction(s.EndPoint.Y)), true)).ToList());

            return result.Select(p => new Point(p.X.ToDouble(), p.Y.ToDouble()));
        }

        public static IEnumerable<PointFr> IntersectionsNaive(List<LineSegmentFr> segments)
        {
            var array = segments.ToArray();
            for (var i = 0; i < array.Length - 1; i++)
            {
                for (var j = i + 1; j < array.Length; j++)
                {
                    var p = array[i].Intersection(array[j]);
                    if (p == null)
                        continue;
                    yield return (PointFr)p;
                }
            }
        }
        
        public static List<LineSegment> TriangulateDnQWrapper(List<Point> points)
        {
            var result = TriangulateDnQ(points.Select(p => new PointFr(new Fraction(p.X), new Fraction(p.Y))).ToList());
            
            return result.Select(s => new LineSegment(
                    new Point(s.StartPoint.X.ToDouble(), s.StartPoint.Y.ToDouble()),
                    new Point(s.EndPoint.X.ToDouble(), s.EndPoint.Y.ToDouble())
                )).ToList();
        }

        public static List<LineSegmentFr> TriangulateDnQ(List<PointFr> points)
        {
            if (points.Count <= 2)
                return new List<LineSegmentFr> { new LineSegmentFr(points[0], points[1], true) };
            if (points.Count <= 3)
                return new List<LineSegmentFr> { new LineSegmentFr(points[0], points[1], true), new LineSegmentFr(points[1], points[2], true), new LineSegmentFr(points[2], points[0], true) };

            var PointsByXThenY = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var leftTri = TriangulateDnQ(PointsByXThenY.Take(PointsByXThenY.Count / 2).ToList());
            var rightTri = TriangulateDnQ(PointsByXThenY.Skip(PointsByXThenY.Count / 2).ToList());

            return MergeTriangulationDnQ(leftTri, rightTri);
        }

        private static List<LineSegmentFr> MergeTriangulationDnQ(List<LineSegmentFr> leftTri, List<LineSegmentFr> rightTri)
        {
            var leftPoints = leftTri.SelectMany(p => p.EndPoints()).OrderBy(p => p.Y).ThenByDescending(p => p.X).Distinct().ToList();
            var rightPoints = rightTri.SelectMany(p => p.EndPoints()).OrderBy(p => p.Y).ThenBy(p => p.X).Distinct().ToList();
            var triangulation = leftTri.Concat(rightTri).ToList();
            var baseLREdge = new LineSegmentFr(leftPoints.First(), rightPoints.First(), true);

            var allPoints = leftPoints.Concat(rightPoints).ToList();
            while (allPoints.Any(p => p.IsRightOf(baseLREdge)))
            {
                var newBaseLREdgeC1 = new LineSegmentFr(baseLREdge.StartPoint, allPoints.First(p => p.IsRightOf(baseLREdge)), true);
                var newBaseLREdgeC2 = new LineSegmentFr(baseLREdge.EndPoint, allPoints.First(p => p.IsRightOf(baseLREdge)), true);
                if (baseLREdge.StartPoint.Y < baseLREdge.EndPoint.Y)
                    baseLREdge = triangulation.Contains(newBaseLREdgeC1) ? newBaseLREdgeC2 : newBaseLREdgeC1;
                else
                    baseLREdge = triangulation.Contains(newBaseLREdgeC2) ? newBaseLREdgeC1 : newBaseLREdgeC2;
            }

            triangulation.Add(baseLREdge);

            PointFr? rightCandidate;
            PointFr? leftCandidate;
            do
            {
                rightCandidate = null;
                leftCandidate = null;
                var rightCandidates = AngularSortDescending(rightPoints, baseLREdge.EndPoint, false, baseLREdge.StartPoint);
                for (var i = 0; i < rightCandidates.Count; i++)
                {
                    var rc = rightCandidates[i];
                    if (rc.IsRightOf(baseLREdge))
                        break;

                    if (i + 1 >= rightCandidates.Count ||
                        !new TriangleFr(baseLREdge.StartPoint, baseLREdge.EndPoint, rc).ContainsInCircumcircle(
                            rightCandidates[i + 1]))
                    {
                        if (rightTri.Any(s => s.IntersectsStrict(new LineSegmentFr(baseLREdge.StartPoint, rc, true))))
                           continue;
                        rightCandidate = rc;
                        break;
                    }

                    rightTri.RemoveAll(s => s == new LineSegmentFr(baseLREdge.EndPoint, rc, true));
                    triangulation.RemoveAll(s => s == new LineSegmentFr(baseLREdge.EndPoint, rc, true));
                }

                var leftCandidates = AngularSort(leftPoints, baseLREdge.StartPoint, false, baseLREdge.EndPoint);
                for (var i = 0; i < leftCandidates.Count; i++)
                {
                    var lc = leftCandidates[i];
                    if (lc.IsRightOf(baseLREdge))
                        break;

                    if (i + 1 >= leftCandidates.Count ||
                        !new TriangleFr(baseLREdge.StartPoint, baseLREdge.EndPoint, lc).ContainsInCircumcircle(
                            leftCandidates[i + 1]))
                    {
                        if (leftTri.Any(s => s.IntersectsStrict(new LineSegmentFr(baseLREdge.EndPoint, lc, true))))
                            continue;
                        leftCandidate = lc;
                        break;
                    }

                    leftTri.RemoveAll(s => s == new LineSegmentFr(baseLREdge.StartPoint, lc, true));
                    triangulation.RemoveAll(s => s == new LineSegmentFr(baseLREdge.StartPoint, lc, true));
                }

                if (leftCandidate != null && rightCandidate != null)
                {
                    if (new TriangleFr(baseLREdge.StartPoint, baseLREdge.EndPoint, (PointFr)rightCandidate)
                            .ContainsInCircumcircle((PointFr)leftCandidate))
                        triangulation.Add(new LineSegmentFr(baseLREdge.EndPoint, (PointFr)leftCandidate, true));
                    else if (
                        new TriangleFr(baseLREdge.StartPoint, baseLREdge.EndPoint, (PointFr)leftCandidate)
                            .ContainsInCircumcircle((PointFr)rightCandidate))
                        triangulation.Add(new LineSegmentFr(baseLREdge.StartPoint, (PointFr)rightCandidate, true));
                    else // cztery punkty leżą na jednym okręgu
                        triangulation.Add(new LineSegmentFr(baseLREdge.EndPoint, (PointFr)leftCandidate, true));
                }
                else if (leftCandidate != null)
                    triangulation.Add(new LineSegmentFr(baseLREdge.EndPoint, (PointFr)leftCandidate, true));
                else if (rightCandidate != null)
                    triangulation.Add(new LineSegmentFr(baseLREdge.StartPoint, (PointFr)rightCandidate, true));

                baseLREdge = triangulation.Last();
            }
            while (leftCandidate != null || rightCandidate != null);

            return triangulation;
        }

        public static List<LineSegment> TriangulateIterativeWrapper(List<Point> points) //, out Triangle superTriangle// nie obsługuje punktów o jednakowych współrzędnych x lub y ani duplikatów
        {
            if (points.Count < 3)
                throw new ArgumentException("Can't triangulate less than 3 points");
            var pointByXThenY = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            return TriangulateIterative(pointByXThenY.Select(p => p.ToPointFraction()).ToList()).Distinct().ToList()
                .SelectMany(t => t.Edges(true)).Distinct().Select(s => s.ToLineSegmentDouble()).ToList();
        }

        public static List<TriangleFr> TriangulateIterative(List<PointFr> points)
        {
            if (points.Count < 3)
                throw new ArgumentException("Can't triangulate less trhan 3 points");

            var superTriangle = GetSuperTriangle(points);
            var triangles = new List<TriangleFr> { superTriangle }; // dodaj do listy trójkąt zawierający wszystkie triangulowane punkty

            foreach (var p in points)
            {
                var edges = new List<LineSegmentFr>(); // zainicjuj bufor odcinków
                for (var j = triangles.Count - 1; j >= 0; j--) // jeżeli wierzchołek leży wewnątrz opisanego okręgu wtedy trzy boki trójkąta sa dodawane do listy, a trójkąt jest usuwany z listy trójkątów
                {
                    var t = triangles[j];
                    if (!t.ContainsInCircumcircle(p, superTriangle))
                        continue;
                    edges.AddRange(t.Edges(true));
                    triangles.RemoveAt(j);
                }
                
                edges = edges.GroupBy(g => g).Where(g => g.Count() < 2).Select(g => g.Key).ToList(); // usuń zduplikowane boki, powinna zostać otoczka wypukła w której boki są ułożone w kolejności odwrotnej do wskazówek zegara
                triangles.AddRange(edges.Select(edge => new TriangleFr(edge.StartPoint, edge.EndPoint, p))); // dodaj w kolejności odwrotnej do wskazówek zegara trójkąty wypełniając kolejne puste miejsce w triangulacji
            }
            
            triangles.RemoveAll(t => t.SharesVertexWith(superTriangle)); // usuwamy wszystkie trójkąty dzielące wierzchołek z trójkątem dodanym na początku
            return triangles;
        }

        private static TriangleFr GetSuperTriangle(IReadOnlyList<PointFr> points)
        {
            var xMin = points.MinBy(p => p.X).First().X;
            var yMin = points.MinBy(p => p.Y).First().Y;
            var xMax = points.MaxBy(p => p.X).First().X;
            var yMax = points.MaxBy(p => p.Y).First().Y;

            var dx = xMax - xMin;
            var dy = yMax - yMin;
            var dMax = dx > dy ? dx : dy;
            var xMid = (xMin + xMax) / 2;
            var yMid = (yMin + yMax) / 2;

            return new TriangleFr(
                new PointFr(xMid - 2 * dMax, yMid - dMax),
                new PointFr(xMid, yMid + 2 * dMax),
                new PointFr(xMid + 2 * dMax, yMid - dMax)
            );
        }

        public static bool IsInsidePolygonWrapper(Point point, Polygon polygon)
        {
            return IsInsidePolygon(point.ToPointFraction(), polygon.ToPolygonFraction());
        }

        public static bool IsInsidePolygon(PointFr point, PolygonFr polygon)
        {
            if (polygon.Edges(true).Any(edge => edge.Contains(point)))
                return true;

            var vertices = polygon.Vertices.ToList();
            int i, j = vertices.Count - 1;
            var contains = false;

            for (i = 0; i < vertices.Count; i++)
            {
                if ((vertices[i].Y < point.Y && vertices[j].Y >= point.Y || vertices[j].Y < point.Y && vertices[i].Y >= point.Y) && (vertices[i].X <= point.X || vertices[j].X <= point.X))
                    contains ^= (vertices[i].X + (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) * (vertices[j].X - vertices[i].X) < point.X);
                j = i;
            }

            return contains;
        }

        public static bool IsInsideConvexPolygonWrapper(Point point, Polygon polygon)
        {
            return IsInsideConvexPolygon(point.ToPointFraction(), polygon.ToPolygonFraction());
        }

        public static bool IsInsideConvexPolygon(PointFr point, PolygonFr polygon)
        {
            var vertices = polygon.Vertices.ToList(); // wierzchołki posortowane przeciwnie do wskazówek zegara względem wnętrza wielokąta
            var edges = polygon.Edges().ToList();

            if (vertices.Count < 3)
                throw new ArgumentException("Polygon can't have less than 3 vertices");
            if (ConvexHullJarvis(vertices).Count != vertices.Count) // Powinno wystarczyć Count
                throw new ArgumentException("Polygon must be convex");

            if (vertices.Count == 3)
                return !edges.Any(point.IsRightOf);
            
            var polyCenter = polygon.Center;
            if (polyCenter == null)
                throw new Exception("Can't calculate center of the Polygon");
            var mid = (PointFr)polyCenter;

            while (edges.Count > 1)
            {
                var testSegment = new LineSegmentFr(mid, edges[edges.Count / 2].StartPoint);
                if (point.IsRightOf(testSegment))
                    edges = edges.Take(edges.Count / 2).ToList();
                else if (point.IsLeftOf(testSegment))
                    edges = edges.Skip(edges.Count / 2).ToList();
                else if (testSegment.Contains(point))
                    return true;
                else
                    throw new Exception("Invalid calculations performed, it should never happen");
            }

            return !point.IsRightOf(edges.Single()); // czyli IsLeftOf + Contains, jeżeli jest we wierzchołku to będzie spełniony ostatni warunek z while'a
        }

        public static List<LineSegment> VoronoiWrapper(List<Point> points)
        {
            return Voronoi(points.Select(p => p.ToPointFraction()).ToList()).Select(s => s.ToLineSegmentDouble()).ToList();
        }

        public static List<LineSegmentFr> Voronoi(List<PointFr> points)
        {
            var triangulation = TriangulateIterative(points);
            var ch = new PolygonFr(ConvexHullJarvis(points));
            var dictTriEdges = new Dictionary<LineSegmentFr, PointFr>();
            var voronoiDiagram = new List<LineSegmentFr>();
            foreach (var triangle in triangulation)
            {
                var circumCenter = triangle.CircumCircle().Center;
                foreach (var edge in triangle.Edges(true))
                {
                    if (dictTriEdges.ContainsKey(edge))
                    {
                        voronoiDiagram.Add(new LineSegmentFr(circumCenter, dictTriEdges[edge], true));
                        dictTriEdges.Remove(edge);
                    }
                    else
                        dictTriEdges.Add(edge, circumCenter);
                }
            }

            voronoiDiagram.AddRange(
                dictTriEdges.Where(kvp => ch.Contains2(kvp.Value) && kvp.Key.Center() != kvp.Value)
                    .Select(kvp => new LineSegmentFr(kvp.Key.Center(), kvp.Value)));

            for (var i = voronoiDiagram.Count - 1; i >= 0; i--)
            {
                var edge = voronoiDiagram[i];
                var chIntersections = ch.Intersections(edge);
                if (chIntersections.Count > 2) throw new Exception("Błędne obliczenia");
                if (!ch.Contains2(edge.StartPoint) && !ch.Contains2(edge.EndPoint)) // chIntersections.Count > 1 || 
                {
                    voronoiDiagram.Remove(edge);
                    continue;
                }
                if (!ch.Contains2(edge.StartPoint))
                    edge.StartPoint = chIntersections.Except(edge.EndPoints()).Single();
                else if (!ch.Contains2(edge.EndPoint))
                    edge.EndPoint = chIntersections.Except(edge.EndPoints()).Single();
                voronoiDiagram[i] = edge;
            }

            return voronoiDiagram;
        }
    }
}

