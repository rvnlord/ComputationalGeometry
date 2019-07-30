using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using WPFComputationalGeometry.Source.Common;
using WPFComputationalGeometry.Source.Common.Converters;
using WPFComputationalGeometry.Source.Common.Extensions;
using WPFComputationalGeometry.Source.Models;
using ColorConverter = WPFComputationalGeometry.Source.Common.Converters.ColorConverter;
using Label = System.Windows.Controls.Label;
using Path = System.Windows.Shapes.Path;
using Line = System.Windows.Shapes.Line;
using LineSegment = WPFComputationalGeometry.Source.Models.LineSegment;
using Polygon = System.Windows.Shapes.Polygon;
using Point = System.Windows.Point;
using CGPoint = WPFComputationalGeometry.Source.Models.Point;
using MoreLinq;
using WPFComputationalGeometry.Source.Common.Utils;
using WPFComputationalGeometry.Source.Common.Utils.UtilsClasses;
using WPFComputationalGeometry.Source.Models.Comparers;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using TextBox = System.Windows.Controls.TextBox;

namespace WPFComputationalGeometry.Source.Windows
{
    public partial class MainWindow
    {
        private NotifyIcon _notifyIcon;

        private int _gridDensity = 20;
        private const int _chartAxisDiff = 10;
        private Point _middlePoint;

        private Point? _lineStartPoint;
        private readonly List<Line> _polygonLines = new List<Line>();
        private Line _currentLine;
        
        private bool _draw = true;
        private bool _deletingRow;
        private bool _selectingRowsProgramatically;
        private bool _rddlElementTypeSelectingProgramatically;

        private Point? _startDraggingPoint;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Inicjuj kontrolki

            var iconHandle = Properties.Resources.NotifyIcon.GetHicon();
            var icon = System.Drawing.Icon.FromHandle(iconHandle);

            _notifyIcon = new NotifyIcon
            {
                BalloonTipTitle = lblWindowTitle.Content.ToString(),
                BalloonTipText = @"is hidden here",
                Icon = icon
            };
            _notifyIcon.Click += notifyIcon_Click;

            TxtSaveToFile.PreviewMouseLeftButtonDown += TxtSaveToFile_MouseDown;
            TxtSaveToFile.PreviewMouseRightButtonDown += TxtSaveToFile_MouseDown;
            var db = new DbContext();
            DataContext = db;
            RgvData.ItemsSource = db.ChartElements;

            // Ustaw Długości Osi obok wykresu

            GridYAxis.Height = CanvasChart.Height + 2 * _chartAxisDiff;
            GridXAxis.Width = CanvasChart.Width + 2 * _chartAxisDiff;

            // Dodaj Placeholdery do TextBoxów

            RnumXStart.ToNumericBox(2);
            RnumYStart.ToNumericBox(2);
            RnumXEnd.ToNumericBox(2);
            RnumYEnd.ToNumericBox(2);
            RnumSamples.ToNumericBox(0, 0, 100000);

            gridMain.Children.OfType<Grid>().FirstOrDefault(grid => grid.Name == "GridControls")?.Children.OfType<TextBox>().ForEach(t =>
            {
                if (t.Name.StartsWith("Rnum"))
                    return;

                t.GotFocus += TxtAll_GotFocus;
                t.LostFocus += TxtAll_LostFocus;

                var currBg = ((SolidColorBrush)t.Foreground).Color;
                t.FontStyle = FontStyles.Italic;
                t.Text = t.Tag?.ToString() ?? "";
                t.Foreground = new SolidColorBrush(Color.FromArgb(128, currBg.R, currBg.G, currBg.B));
            });

            // Utwórz siatkę i dodaj numerowanie osi do wykresu

            var gridColor = Color.FromRgb(0, 0, 73);
            for (var i = 0; i <= CanvasChart.Height; i += _gridDensity)
            {
                var gridLineHorizontal = new Line
                {
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = 1,
                    X1 = 0,
                    Y1 = i,
                    X2 = CanvasChart.ActualWidth,
                    Y2 = i,
                    Name = "gridLineHorizontal",
                    SnapsToDevicePixels = true
                };

                gridLineHorizontal.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                CanvasChart.Children.Add(gridLineHorizontal);
            }

            for (var i = 0; i <= CanvasChart.Width; i += _gridDensity)
            {
                var gridLineVertical = new Line
                {
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = 1,
                    X1 = i,
                    Y1 = 0,
                    X2 = i,
                    Y2 = CanvasChart.ActualHeight,
                    Name = "gridLineVertical",
                    SnapsToDevicePixels = true
                };

                gridLineVertical.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                CanvasChart.Children.Add(gridLineVertical);
            }

            var middleHorizontalLine = CanvasChart.Children.OfType<Line>()
                .Where(x => x.Name == "gridLineHorizontal")
                .Aggregate((curMin, x) => curMin == null || Math.Abs(CanvasChart.Height / 2 - x.Y1) < Math.Abs(CanvasChart.Height / 2 - curMin.Y1) ? x : curMin);

            middleHorizontalLine.Stroke = new SolidColorBrush(Colors.Blue);
            middleHorizontalLine.StrokeThickness = 2;
            Panel.SetZIndex(middleHorizontalLine, 10);

            var middleVerticalLine = CanvasChart.Children.OfType<Line>()
                .Where(x => x.Name == "gridLineVertical")
                .Aggregate((curMin, x) => curMin == null || Math.Abs(CanvasChart.Width / 2 - x.X1) < Math.Abs(CanvasChart.Width / 2 - curMin.X1) ? x : curMin);

            middleVerticalLine.Stroke = new SolidColorBrush(Colors.Blue);
            middleVerticalLine.StrokeThickness = 2;
            Panel.SetZIndex(middleVerticalLine, 10);

            _middlePoint = new Point(middleVerticalLine.X1, middleHorizontalLine.Y1);

            var firstHorizontalLineNum = middleHorizontalLine.Y1 / _gridDensity;
            var chartLines = CanvasChart.Children.OfType<Line>().ToList();
            chartLines.Where(x => x.Name == "gridLineHorizontal").ToList().ForEach(line =>
            {
                var lbl = new Label
                {
                    Content = firstHorizontalLineNum--,
                    Margin = new Thickness(0, _chartAxisDiff + (int)line.Y1 - _gridDensity / 2, 10, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    FontSize = 12,
                    Height = _gridDensity
                };
                GridYAxis.Children.Add(lbl);
            });

            var firstVerticalLineNum = 0 - middleVerticalLine.X1 / _gridDensity;
            chartLines.Where(x => x.Name == "gridLineVertical").ToList().ForEach(line =>
            {
                var lbl = new Label
                {
                    Content = firstVerticalLineNum++,
                    Margin = new Thickness(_chartAxisDiff + (int)line.X1 - _gridDensity / 2, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    FontSize = 12,
                    Width = _gridDensity
                };
                GridXAxis.Children.Add(lbl);
            });

            SvChart.ScrollToHorizontalOffset(middleVerticalLine.X1 - SvChart.Width / 2);
            SvChart.ScrollToVerticalOffset(middleHorizontalLine.Y1 - SvChart.Height / 2);
            SvXAxis.ScrollToHorizontalOffset(middleVerticalLine.X1 - SvChart.Width / 2);
            SvYAxis.ScrollToVerticalOffset(middleHorizontalLine.Y1 - SvChart.Height / 2);

            // Ustaw Kontrolki

            //Enum.GetValues(typeof (ElementType)).Cast<ElementType>();
            RddlElementType.ItemsSource = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("(Select Element Type)", -1),
                new KeyValuePair<string, int>("Segment", 0),
                new KeyValuePair<string, int>("Point", 1),
                new KeyValuePair<string, int>("Polygon", 2)
            };
            RddlElementType.DisplayMemberPath = "Key";
            RddlElementType.SelectedValuePath = "Value";
            RddlElementType.SelectedValue = -1;

            RnumXStart.GetNumericBox().MinValue = Math.Round((0 - _middlePoint.X) / _gridDensity, 2).ToDecimal();
            RnumXStart.GetNumericBox().MaxValue = Math.Round((CanvasChart.Width - _middlePoint.X) / _gridDensity, 2).ToDecimal();
            RnumYStart.GetNumericBox().MinValue = Math.Round((0 - _middlePoint.Y) / _gridDensity, 2).ToDecimal();
            RnumYStart.GetNumericBox().MaxValue = Math.Round((CanvasChart.Height - _middlePoint.Y) / _gridDensity, 2).ToDecimal();

            RnumXEnd.GetNumericBox().MinValue = Math.Round((0 - _middlePoint.X) / _gridDensity, 2).ToDecimal();
            RnumXEnd.GetNumericBox().MaxValue = Math.Round((CanvasChart.Width - _middlePoint.X) / _gridDensity, 2).ToDecimal();
            RnumYEnd.GetNumericBox().MinValue = Math.Round((0 - _middlePoint.Y) / _gridDensity, 2).ToDecimal();
            RnumYEnd.GetNumericBox().MaxValue = Math.Round((CanvasChart.Height - _middlePoint.Y) / _gridDensity, 2).ToDecimal();

            RddlCalculationType.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int)CalculationType.None, "(Select Calculations)"),
                new DdlItem((int)CalculationType.PointOrientationTest, "Orientation Test"),
                new DdlItem((int)CalculationType.PointMembershipTest, "Membership Test"),
                new DdlItem((int)CalculationType.LineRectangularBoundsIntersection, "Rectangular Bounds Intersection Test"),
                new DdlItem((int)CalculationType.LineIntersection, "Segments Intersection Test"),
                new DdlItem((int)CalculationType.PointsClosestPairIterative, "Closest Pair of Points (Iterative)"),
                new DdlItem((int)CalculationType.PointsClosestPairRecursive, "Closest Pair of Points (Recursive Divide and Conquer)"),
                new DdlItem((int)CalculationType.PointsClosestPairNaive, "Closest Pair of Points (Naive)"),
                new DdlItem((int)CalculationType.PointsFarthestPair, "Farthest Pair of Points (Shanos)"),
                new DdlItem((int)CalculationType.AngularSorting, "Angle Wise Points Sorting"),
                new DdlItem((int)CalculationType.ConvexHullGraham, "Convex Hull (Graham)"),
                new DdlItem((int)CalculationType.ConvehHullJarvis, "Convex Hull (Jarvis)"),
                new DdlItem((int)CalculationType.IntersectionsBentleyOttmann, "Segments Intersections (Bentley-Ottmann)"),
                new DdlItem((int)CalculationType.IntersectionsNaive, "Segments Intersections (Naive)"),
                new DdlItem((int)CalculationType.TriangulationDnQ, "Mesh Triangulation (Divide and Conquer)"),
                new DdlItem((int)CalculationType.TriangulationIterative, "Mesh Triangulation (Delaunay Iterative)"),
                new DdlItem((int)CalculationType.PolygonCenter, "Polygon Center"),
                new DdlItem((int)CalculationType.IsPointInsidePolygon, "Test if Point is inside Polygon (Linear)"),
                new DdlItem((int)CalculationType.IsPointInsideConvexPolygon, "Test if Point is inside Convex Polygon (Logarithmic)"),
                new DdlItem((int)CalculationType.Voronoi, "Mesh Voronoi Tesselation"),
            };
            RddlCalculationType.DisplayMemberPath = "Text";
            RddlCalculationType.SelectedValuePath = "Index";
            RddlCalculationType.SelectedValue = -1;
            BtnCalculate.IsEnabled = false;

            RddlTestType.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int)TestType.None, "(Perform Test)"),
                new DdlItem((int)TestType.PointsClosestPairSpeedTest, "Closest Pair of Points Speed Test"),
                new DdlItem((int)TestType.IntersectionsSpeedTest, "Segments Intersections Speed Test"),
            };
            RddlTestType.DisplayMemberPath = "Text";
            RddlTestType.SelectedValuePath = "Index";
            RddlTestType.SelectedValue = -1;
            BtnTest.IsEnabled = false;
        }

        private void CanvasChart_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var db = (DbContext)DataContext;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    return;

                if (!_draw)
                {
                    _draw = true;
                    return;
                }

                if (RbDrawLines.IsChecked == true)
                {
                    _lineStartPoint = e.GetPosition((Canvas)sender);
                }
                else if (RbDrawPoints.IsChecked == true)
                {
                    var pointPosition = new Point(Math.Round(e.GetPosition((Canvas)sender).X), Math.Round(e.GetPosition((Canvas)sender).Y));
                    var geometry = new EllipseGeometry(pointPosition, 2.5, 2.5);
                    
                    if (db.ChartElements.Select(x => new Point { X = x.XStart, Y = x.YStart }).Any(p => Math.Abs(p.X - pointPosition.X) < _gridDensity && Math.Abs(p.Y - pointPosition.Y) < _gridDensity))
                        return;

                    var point = new Path
                    {
                        Fill = Brushes.Yellow,
                        StrokeThickness = 0,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Data = geometry,
                        SnapsToDevicePixels = true
                    };
                    point.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                    point.MouseDown += Point_MouseDown;
                    
                    db.ChartElements.Add(new ChartElement
                    {
                        ElementType = ElementType.Point,
                        XStart = ((EllipseGeometry)point.Data).Center.X,
                        YStart = ((EllipseGeometry)point.Data).Center.Y,
                        XStartGridRelative = (((EllipseGeometry)point.Data).Center.X - _middlePoint.X) / _gridDensity,
                        YStartGridRelative = (_middlePoint.Y - ((EllipseGeometry)point.Data).Center.Y) / _gridDensity,
                        PhysicalRepresentation = point
                    });
                    RgvData.ItemsSource = db.ChartElements;
                    Panel.SetZIndex(point, 21);
                    CanvasChart.Children.Add(point);
                }
                else if (RbDrawPolygons.IsChecked == true)
                {
                    Line firstPolygonLine = null;
                    Line oldLastPolygonLine = null;
                    CGPoint? firstPoint = null;

                    var pos = e.GetPosition((Canvas)sender);
                    if (_polygonLines.Count > 0)
                    {
                        firstPolygonLine = _polygonLines.First();
                        firstPoint = new CGPoint(firstPolygonLine.X1, firstPolygonLine.Y1);
                        oldLastPolygonLine = _polygonLines.Last();
                        if (new CGPoint(oldLastPolygonLine.X1, oldLastPolygonLine.Y1).Distance(new CGPoint(oldLastPolygonLine.X2, oldLastPolygonLine.Y2)) < _gridDensity)
                            return;
                        if (_polygonLines.Count > 2 && ((CGPoint)firstPoint).Distance(pos) < 20)
                            pos = (CGPoint)firstPoint;
                    }

                    var allPolygonsEdges = new List<LineSegment>();
                    allPolygonsEdges.AddRange(_polygonLines.Select(l => 
                        new LineSegment(
                            new CGPoint(l.X1, l.Y1),
                            new CGPoint(l.X2, l.Y2))));
                    LineSegment? currEdge = null;
                    if (allPolygonsEdges.Count > 0)
                        currEdge = allPolygonsEdges.Last();
                    allPolygonsEdges = allPolygonsEdges.Take(allPolygonsEdges.Count - 1).ToList();
                    foreach (var polygon in CanvasChart.Children.OfType<Polygon>())
                        allPolygonsEdges.AddRange(polygon.Points.Select((p, i) => new LineSegment(p, polygon.Points[(i + 1) % polygon.Points.Count])));

                    var currPoints = _polygonLines.SelectMany(l =>
                        new[] { new CGPoint(l.X1, l.Y1), new CGPoint(l.X2, l.Y2) }).Distinct();
                    var polygons = CanvasChart.Children.OfType<Polygon>().Select(cp =>
                        new Models.Polygon(cp.Points.Select(p => (CGPoint) p))).ToList();

                    if ((currEdge != null && allPolygonsEdges.Any(edge => edge.IntersectsStrict((LineSegment) currEdge)))) // jeżeli krawedzie się przecinają, nie dodawaj linii
                        return;
                    if (polygons.Any(poly => poly.Contains(currPoints) || poly.Contains(pos))) // Anuluj jeżeli nastąpiła próba rysowania wewnątrz innego wielokąta
                    {
                        foreach (var l in _polygonLines)
                            ((Canvas)sender).Children.Remove(l);
                        _polygonLines.Clear();
                        return;
                    }

                    if (pos == firstPoint) // Warunek końca rysowania wielokąta
                    { // _polygonLines zawiera pos w tym miejscu wiec nie trzeba go dodawać
                        var currPolygon = new Models.Polygon(_polygonLines.SelectMany(l =>
                            new[] { new CGPoint(l.X1, l.Y1), new CGPoint(l.X2, l.Y2) }).Distinct());
                        if (polygons.Any(poly => currPolygon.Contains(poly))) // Anuluj rysowanie jeżeli wewnątrz jest wielokąt
                        {
                            foreach (var l in _polygonLines)
                                ((Canvas)sender).Children.Remove(l);
                            _polygonLines.Clear();
                            return;
                        }

                        oldLastPolygonLine.X2 = pos.X;
                        oldLastPolygonLine.Y2 = pos.Y;

                        var chartPolygon = new Polygon
                        {
                            Stroke = new SolidColorBrush(Colors.LightBlue),
                            Fill = new SolidColorBrush(Color.FromArgb(16, 0, 0, 255)),
                            StrokeThickness = 3,
                            Points = new PointCollection(_polygonLines.Select(l => new Point(l.X1, l.Y1))),
                            Name = "polygon",
                            SnapsToDevicePixels = true,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        CanvasChart.Children.Add(chartPolygon);
                        Panel.SetZIndex(chartPolygon, 11);
                        chartPolygon.MouseDown += Polygon_MouseDown;

                        db.ChartElements.Add(new ChartElement
                        {
                            ElementType = ElementType.Polygon,
                            XStart = firstPolygonLine.X1,
                            YStart = firstPolygonLine.Y1,
                            XStartGridRelative = (firstPolygonLine.X1 - _middlePoint.X) / _gridDensity,
                            YStartGridRelative = (_middlePoint.Y - firstPolygonLine.Y1) / _gridDensity,
                            PhysicalRepresentation = chartPolygon,
                            Vertices = _polygonLines.Select(l => new CGPoint(
                                (l.X1 - _middlePoint.X) / _gridDensity, 
                                (_middlePoint.Y - l.Y1) / _gridDensity)).ToList()
                        });
                        RgvData.ItemsSource = db.ChartElements;

                        foreach (var l in _polygonLines)
                            CanvasChart.Children.Remove(l);
                        _polygonLines.Clear();

                        return;
                    }

                    _polygonLines.Add(
                        new Line
                        {
                            Stroke = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                            StrokeThickness = 3,
                            X1 = Math.Round(pos.X),
                            Y1 = Math.Round(pos.Y),
                            X2 = Math.Round(pos.X),
                            Y2 = Math.Round(pos.Y),
                            Name = "polygonLine",
                            SnapsToDevicePixels = true,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    var newLastLine = _polygonLines.Last();
                    newLastLine.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                    Panel.SetZIndex(newLastLine, 11);
                    CanvasChart.Children.Add(newLastLine);
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                _startDraggingPoint = e.GetPosition(SvChart);

                if (RbDrawPolygons.IsChecked == true)
                {
                    if (_polygonLines.Count > 0)
                    {
                        foreach (var l in _polygonLines)
                            ((Canvas)sender).Children.Remove(l);

                        _polygonLines.Clear();
                    }
                }
            }
        }

        private void CanvasChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && RbDrawPolygons.IsChecked != true)
            {
                if (RbDrawLines.IsChecked == true)
                {
                    if (_lineStartPoint != null)
                    {
                        if (_currentLine == null)
                            _currentLine = new Line();

                        _currentLine.Stroke = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                        _currentLine.StrokeThickness = 3;
                        _currentLine.X1 = Math.Round(_lineStartPoint.Value.X);
                        _currentLine.Y1 = Math.Round(_lineStartPoint.Value.Y);
                        _currentLine.X2 = Math.Round(e.GetPosition((Canvas) sender).X);
                        _currentLine.Y2 = Math.Round(e.GetPosition((Canvas) sender).Y);
                        _currentLine.Name = "lineCurrent";
                        _currentLine.SnapsToDevicePixels = true;
                        _currentLine.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                        _currentLine.HorizontalAlignment = HorizontalAlignment.Center;
                        _currentLine.VerticalAlignment = VerticalAlignment.Center;
                        Panel.SetZIndex(_currentLine, 11);

                        if (!CanvasChart.Children.Contains(_currentLine))
                            CanvasChart.Children.Add(_currentLine);
                    }
                }
                else if (RbDrawPoints.IsChecked == true)
                {

                }
            }
            else if (e.RightButton == MouseButtonState.Pressed && _startDraggingPoint != null)
            {
                SvChart.ScrollToHorizontalOffset(SvChart.HorizontalOffset + _startDraggingPoint.Value.X - e.GetPosition(SvChart).X);
                SvChart.ScrollToVerticalOffset(SvChart.VerticalOffset + _startDraggingPoint.Value.Y - e.GetPosition(SvChart).Y);
                SvXAxis.ScrollToHorizontalOffset(SvXAxis.HorizontalOffset + _startDraggingPoint.Value.X - e.GetPosition(SvChart).X);
                SvYAxis.ScrollToVerticalOffset(SvYAxis.VerticalOffset + _startDraggingPoint.Value.Y - e.GetPosition(SvChart).Y);
                _startDraggingPoint = e.GetPosition(SvChart);
            }
            else if (RbDrawPolygons.IsChecked == true)
            {
                if (_polygonLines.Count > 0)
                {
                    var pos = e.GetPosition((Canvas)sender);
                    var firstPolygonLine = _polygonLines.First();
                    var lastPolygonLine = _polygonLines.Last();
                    lastPolygonLine.X2 = Math.Round(pos.X);
                    lastPolygonLine.Y2 = Math.Round(pos.Y);
                    foreach (var l in _polygonLines)
                        l.Stroke = new SolidColorBrush(Color.FromRgb(102, 102, 102));

                    var firstPoint = new CGPoint(firstPolygonLine.X1, firstPolygonLine.Y1);
                    if (_polygonLines.Count > 2 && firstPoint.Distance(pos) < 20)
                    {
                        lastPolygonLine.X2 = firstPoint.X;
                        lastPolygonLine.Y2 = firstPoint.Y;
                        foreach (var l in _polygonLines)
                            l.Stroke = new SolidColorBrush(Color.FromRgb(179, 179, 179));
                    }
                }
            }
        }

        private void CanvasChart_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (RbDrawLines.IsChecked == true)
                {
                    if (_lineStartPoint != null && _currentLine != null)
                    {
                        _currentLine.Stroke = new SolidColorBrush(Colors.White);
                        _currentLine.MouseDown += Line_MouseDown;

                        var db = (DbContext)DataContext;
                        if (Math.Sqrt(Math.Pow(_currentLine.X2 - _currentLine.X1, 2) +
                                      Math.Pow(_currentLine.Y2 - _currentLine.Y1, 2)) < _gridDensity || db.ChartElements.Select(x => new Point { X = x.XStart, Y = x.YStart }).Any(p => Math.Abs(p.X - _currentLine.X1) < 5 && Math.Abs(p.Y - _currentLine.Y1) < 5))
                            CanvasChart.Children.Remove(_currentLine);
                        else
                        {
                            db.ChartElements.Add(new ChartElement
                            {
                                ElementType = ElementType.Line,
                                XStart = _currentLine.X1,
                                YStart = _currentLine.Y1,
                                XEnd = _currentLine.X2,
                                YEnd = _currentLine.Y2,
                                XStartGridRelative = (_currentLine.X1 - _middlePoint.X) / _gridDensity,
                                YStartGridRelative = (_middlePoint.Y - _currentLine.Y1) / _gridDensity,
                                XEndGridRelative = (_currentLine.X2 - _middlePoint.X) / _gridDensity,
                                YEndGridRelative = (_middlePoint.Y - _currentLine.Y2) / _gridDensity,
                                PhysicalRepresentation = _currentLine
                            });
                            RgvData.ItemsSource = db.ChartElements;
                        }

                        _currentLine = null;
                        _lineStartPoint = null;
                    }
                }
                else if (RbDrawPoints.IsChecked == true)
                {

                }
            }
            else if (e.ChangedButton == MouseButton.Right && _startDraggingPoint != null)
            {
                _startDraggingPoint = null;
            }
            else if (RbDrawPolygons.IsChecked == true)
            {

            }

            _draw = true;
        }

        private void Line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RbSelect.IsChecked != true && !Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            _draw = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                var db = (DbContext)DataContext;
                _selectingRowsProgramatically = true;
                var clickedLine = (Line) sender;
                var selectedElements = SelectedElements();

                if (selectedElements.Contains(clickedLine))
                    RgvData.SelectedItems.Remove(db.ChartElements.Single(el => Equals(el.PhysicalRepresentation, clickedLine)));             
                else
                    RgvData.SelectedItems.Add(db.ChartElements.Single(el => Equals(el.PhysicalRepresentation, clickedLine)));

                UpdateInputs();

                _selectingRowsProgramatically = false;
            }
        }

        private void Point_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RbSelect.IsChecked != true && !Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            _draw = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                var db = (DbContext)DataContext;
                var clickedPoint = (Path)sender;
                _selectingRowsProgramatically = true;
                var selectedElements = SelectedElements();

                if (selectedElements.Contains(clickedPoint))
                    RgvData.SelectedItems.Remove(db.ChartElements.Single(el => Equals(el.PhysicalRepresentation, clickedPoint)));
                else
                    RgvData.SelectedItems.Add(db.ChartElements.Single(el => Equals(el.PhysicalRepresentation, clickedPoint)));


                UpdateInputs();

                _selectingRowsProgramatically = false;
            }
        }
        
        private void Polygon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RbSelect.IsChecked != true && !Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            _draw = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                var db = (DbContext)DataContext;
                _selectingRowsProgramatically = true;
                var clickedPolygon = (Polygon)sender;
                var selectedElements = SelectedElements();
                if (selectedElements.Contains(clickedPolygon))
                    RgvData.SelectedItems.Remove(db.ChartElements.Single(el => Equals(el.PhysicalRepresentation, clickedPolygon)));
                else
                    RgvData.SelectedItems.Add(db.ChartElements.Single(el => Equals(el.PhysicalRepresentation, clickedPolygon)));

                UpdateInputs();

                _selectingRowsProgramatically = false;
            }
        }

        private void CanvasChart_MouseLeave(object sender, MouseEventArgs e)
        {
            if (RbDrawLines.IsChecked == true)
            {
                if (_currentLine == null)
                    return;

                ((Canvas) sender).Children.Remove(_currentLine);
                _currentLine = null;
                _lineStartPoint = null;
            }

            if (RbDrawPolygons.IsChecked == true)
            {
                if (_polygonLines.Count == 0)
                    return;

                foreach (var l in _polygonLines)
                    ((Canvas)sender).Children.Remove(l);

                _polygonLines.Clear();
            }
        }

        private void SvChart_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var db = (DbContext)DataContext;
                var selectedelements = SelectedElements();
                foreach (var se in selectedelements)
                {
                    var tse = db.ChartElements.Single(x => Equals(x.PhysicalRepresentation, se));
                    db.ChartElements.Remove(tse);
                    CanvasChart.Children.Remove(se);
                }

                RgvData.ItemsSource = db.ChartElements;
                UpdateInputs();
            }
        }

        private void BtnClearChart_Click(object sender, RoutedEventArgs e)
        {
            CanvasChart.Children.Cast<UIElement>()
                .Where(x => (x is Line && ((Line)x).Name != "gridLineHorizontal" && ((Line)x).Name != "gridLineVertical") || x is Path || x is Polygon).ToList()
                .ForEach(x => CanvasChart.Children.Remove(x));
            var db = (DbContext)DataContext;
            db.ChartElements.Clear();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var db = (DbContext)DataContext;
            RgvData.SelectedItems.Clear();
            db.ChartElements.ToList().ForEach(x => RgvData.SelectedItems.Add(x));
            RgvData.Focus();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            RgvData.SelectedItems.Clear();
        }

        private void BtnAddElement_Click(object sender, RoutedEventArgs e)
        {
            var db = (DbContext)DataContext;

            if (db.ChartElements.Count(x => x.XStartGridRelative.Eq(RnumXStart.GetNumericBox().Value.ToDouble()) && x.YStartGridRelative.Eq(RnumYStart.GetNumericBox().Value.ToDouble())) != 0)
            {
                MessageBox.Show(
                    "You can place only one element at each point",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var item = (KeyValuePair<string, int>)RddlElementType.SelectedItem;
            if (item.Key == "Point")
            {
                var pointPosition = new Point(
                    _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity);
                var geometry = new EllipseGeometry(pointPosition, 2.5, 2.5);

                var point = new Path
                {
                    Fill = Brushes.Yellow,
                    StrokeThickness = 0,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = geometry,
                    SnapsToDevicePixels = true
                };
                point.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                point.MouseDown += Point_MouseDown;

                db.ChartElements.Add(new ChartElement
                {
                    ElementType = ElementType.Point,
                    XStart = _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    YStart = _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    XStartGridRelative = RnumXStart.GetNumericBox().Value.ToDouble(),
                    YStartGridRelative = RnumYStart.GetNumericBox().Value.ToDouble(),
                    PhysicalRepresentation = point
                });
                RgvData.ItemsSource = db.ChartElements;
                CanvasChart.Children.Add(point);

                RgvData.SelectedItems.Clear();
                UpdateInputs();
            }
            else if (item.Key == "Segment")
            {
                var line = new Line
                {
                    Stroke = Brushes.White,
                    StrokeThickness = 3,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    SnapsToDevicePixels = true,
                    X1 = _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    Y1 = _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    X2 = _middlePoint.X + RnumXEnd.GetNumericBox().Value.ToDouble() * _gridDensity,
                    Y2 = _middlePoint.Y - RnumYEnd.GetNumericBox().Value.ToDouble() * _gridDensity
                };
                line.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                line.MouseDown += Line_MouseDown;

                db.ChartElements.Add(new ChartElement
                {
                    ElementType = ElementType.Line,
                    XStart = _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    YStart = _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                    XStartGridRelative = RnumXStart.GetNumericBox().Value.ToDouble(),
                    YStartGridRelative = RnumYStart.GetNumericBox().Value.ToDouble(),
                    XEnd = _middlePoint.X + RnumXEnd.GetNumericBox().Value.ToDouble() * _gridDensity,
                    YEnd = _middlePoint.Y - RnumYEnd.GetNumericBox().Value.ToDouble() * _gridDensity,
                    XEndGridRelative = RnumXEnd.GetNumericBox().Value.ToDouble(),
                    YEndGridRelative = RnumYEnd.GetNumericBox().Value.ToDouble(),
                    PhysicalRepresentation = line
                });
                RgvData.ItemsSource = db.ChartElements;
                CanvasChart.Children.Add(line);

                RgvData.SelectedItems.Clear();
                UpdateInputs();
            }
            else
                MessageBox.Show(
                    "Element type hasn't been selected",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnModifyElement_Click(object sender, RoutedEventArgs e)
        {
            var db = (DbContext)DataContext;
            var selectionCount = RgvData.SelectedItems.Count;
            var selected = RgvData.SelectedItems.Cast<ChartElement>().FirstOrDefault();
            if (selectionCount != 1)
            {
                MessageBox.Show(
                    $"You can modify only one element at once, currently selected: {selectionCount}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (db.ChartElements.Count(x => x != selected && x.XStartGridRelative.Eq(RnumXStart.GetNumericBox().Value.ToDouble()) && x.YStartGridRelative.Eq(RnumYStart.GetNumericBox().Value.ToDouble())) != 0)
            {
                MessageBox.Show(
                    "You can place only one element in each point",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (selected != null && selected.ElementType == ElementType.Line)
            {
                var line = (Line) selected.PhysicalRepresentation;

                line.X1 = _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity;
                line.Y1 = _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity;
                line.X2 = _middlePoint.X + RnumXEnd.GetNumericBox().Value.ToDouble() * _gridDensity;
                line.Y2 = _middlePoint.Y - RnumYEnd.GetNumericBox().Value.ToDouble() * _gridDensity;
                
                selected.XStart = _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity;
                selected.YStart = _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity;
                selected.XEnd = _middlePoint.X + RnumXEnd.GetNumericBox().Value.ToDouble() * _gridDensity;
                selected.YEnd = _middlePoint.Y - RnumYEnd.GetNumericBox().Value.ToDouble() * _gridDensity;

                selected.XStartGridRelative = RnumXStart.GetNumericBox().Value.ToDouble();
                selected.YStartGridRelative = RnumYStart.GetNumericBox().Value.ToDouble();
                selected.XEndGridRelative = RnumXEnd.GetNumericBox().Value.ToDouble();
                selected.YEndGridRelative = RnumYEnd.GetNumericBox().Value.ToDouble();

                //RgvData.ItemsSource = db.ChartElements;
                RgvData.Items.Refresh();
            }
            else if (selected != null && selected.ElementType == ElementType.Point)
            {
                var point = (Path) selected.PhysicalRepresentation;

                var pointData = (EllipseGeometry) point.Data;
                var geometry = new EllipseGeometry(
                    new Point(
                        _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity,
                        _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity),
                    pointData.RadiusX,
                    pointData.RadiusY);

                point.Data = geometry;

                selected.XStart = _middlePoint.X + RnumXStart.GetNumericBox().Value.ToDouble() * _gridDensity;
                selected.YStart = _middlePoint.Y - RnumYStart.GetNumericBox().Value.ToDouble() * _gridDensity;

                selected.XStartGridRelative = RnumXStart.GetNumericBox().Value.ToDouble();
                selected.YStartGridRelative = RnumYStart.GetNumericBox().Value.ToDouble();

                //RgvData.ItemsSource = db.ChartElements;
                RgvData.Items.Refresh();
            }
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            UpdateInputs();

            var db = (DbContext)RgvData.DataContext;
            var calculationType = (CalculationType)RddlCalculationType.SelectedValue;
            var selectedItems = RgvData.SelectedItems.Cast<ChartElement>().ToList();

            var selitemsCount = selectedItems.Count;
            var selectedLines = selectedItems.Where(i => i.ElementType == ElementType.Line).ToList();
            var selectedPoints = selectedItems.Where(i => i.ElementType == ElementType.Point).ToList();
            var selectedPolygons = selectedItems.Where(i => i.ElementType == ElementType.Polygon).ToList();
            var selectedSegmentsCount = selectedLines.Count;
            var selectedPointsCount = selectedPoints.Count;
            var selectedPolygonsCount = selectedPolygons.Count;

            switch (calculationType)
            {
                #region Inne Obliczenia

                case CalculationType.PointOrientationTest:
                {
                    if (selitemsCount != 2 || selectedSegmentsCount != 1 || selectedPointsCount != 1)
                    {
                        MessageBox.Show(
                            $"You should select 1 Segment and 1 Point, currently selected Segments: {selectedSegmentsCount}, Points: {selectedPointsCount}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var pointChartEl = selectedPoints.Single();
                    var lineChartEl = selectedLines.Single();
                    var point = new Point(pointChartEl.XStartGridRelative, pointChartEl.YStartGridRelative);
                    var segment = new LineSegment(
                        new Point(lineChartEl.XStartGridRelative, lineChartEl.YStartGridRelative),
                        new Point(lineChartEl.XEndGridRelative ?? 0, lineChartEl.YEndGridRelative ?? 0));

                    var result = GeometryCalculations.PointOrientationTest(point, segment);

                    LblCalculations.Content = new TextBlock
                    {
                        Text = "Orientation Test:\n\n" +
                            $"Δ = {result:0.00}, Point lies on the {(result < 0 ? "LEFT side of the segment" : (result > 0 ? "RIGHT side of the segment" : "segment (all three points are collinear)"))}"
                    };

                    break;
                }
                case CalculationType.PointMembershipTest:
                {
                    if (selitemsCount != 2 || selectedSegmentsCount != 1 || selectedPointsCount != 1)
                    {
                        MessageBox.Show(
                            $"You should select 1 Segment and 1 Point, currently selected Segments: {selectedSegmentsCount}, Points: {selectedPointsCount}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var pointChartEl = selectedPoints.Single();
                    var lineChartEl = selectedLines.Single();
                    var point = new Point(pointChartEl.XStartGridRelative, pointChartEl.YStartGridRelative);
                    var segment = new LineSegment(
                        new Point(lineChartEl.XStartGridRelative, lineChartEl.YStartGridRelative),
                        new Point(lineChartEl.XEndGridRelative ?? 0, lineChartEl.YEndGridRelative ?? 0));

                    var result = GeometryCalculations.PointMembershipTest(point, segment);

                    LblCalculations.Content = new TextBlock
                    {
                        Text = "Test of Belonging to the Segment:\n\n" +
                            $"Point {(result ? "belongs to the segment (is collinear with points lying at its ends)" : "doesn't belong to the segment")}"
                    };

                    break;
                }
                case CalculationType.LineRectangularBoundsIntersection:
                {
                    if (selitemsCount != 2 || selectedSegmentsCount != 2)
                    {
                        MessageBox.Show(
                            $"You should select 2 Segments, selected: Segments: {selectedSegmentsCount}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var segments = selectedLines
                        .Select(s =>
                            new LineSegment(
                                new Point(s.XStartGridRelative, s.YStartGridRelative),
                                new Point(s.XEndGridRelative ?? 0, s.YEndGridRelative ?? 0)))
                        .ToList();

                    var result = GeometryCalculations.LineRectangularBoundsIntersection(segments[0], segments[1]);

                    LblCalculations.Content = new TextBlock
                    {
                        Text = "Rectangular Bounds Intersection Test:\n\n" +
                            $"Segments Rectangular Bounds {(result ? "" : "don't ")}intersect"
                    };

                    var chartLines = selectedLines.Select(sl => (Line) sl.PhysicalRepresentation).ToList();

                    var rectPositions = chartLines.Select(cl => new Point
                    {
                        X = new[] { cl.X1, cl.X2 }.Min(),
                        Y = new[] { cl.Y1, cl.Y2 }.Min()
                    }).ToList();

                    for (var i = 0; i < rectPositions.Count; i++)
                    {
                        var rect = new Rectangle
                        {
                            Stroke = !result ? Brushes.LightBlue : new SolidColorBrush(Color.FromRgb(250, 108, 108)),
                            StrokeThickness = 2,
                            Height = Math.Abs(chartLines[i].Y1 - chartLines[i].Y2),
                            Width = Math.Abs(chartLines[i].X1 - chartLines[i].X2),
                            StrokeDashArray = { 2, 4 }
                        };

                        Canvas.SetLeft(rect, rectPositions[i].X);
                        Canvas.SetTop(rect, rectPositions[i].Y);
                        CanvasChart.Children.Add(rect);
                    }

                    break;
                }
                case CalculationType.LineIntersection:
                {
                    if (selitemsCount != 2 || selectedSegmentsCount != 2)
                    {
                        MessageBox.Show(
                            $"You should select 2 Segments, selected: Segments: {selectedSegmentsCount}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var segments = selectedLines
                        .Select(s =>
                            new LineSegment(
                                new Point(s.XStartGridRelative, s.YStartGridRelative),
                                new Point(s.XEndGridRelative ?? 0, s.YEndGridRelative ?? 0)))
                        .ToList();

                    var result = GeometryCalculations.SegmentsIntersection(segments[0], segments[1]);

                    LblCalculations.Content = new TextBlock
                    {
                        Text = "Segments Intersection Test:\n\n" +
                            $"Segments {(result ? "" : "don't ")}intersect"
                    };

                    break;
                }
                case CalculationType.PointsClosestPairRecursive:
                case CalculationType.PointsClosestPairNaive:
                case CalculationType.PointsClosestPairIterative:
                {
                    var points = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Point)
                        .Select(s => new CGPoint(s.XStartGridRelative, s.YStartGridRelative))
                        .ToList();

                    if (points.Count < 2)
                    {
                        MessageBox.Show(
                            $"There must be at least 2 Points on the Chart. Points: {points.Count}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    LineSegment result;
                    if (calculationType == CalculationType.PointsClosestPairIterative)
                        result = GeometryCalculations.PointsClosestPairIterative(points);
                    else if (calculationType == CalculationType.PointsClosestPairRecursive)
                        result = GeometryCalculations.PointsClosestPairRecursive(points);
                    else if (calculationType == CalculationType.PointsClosestPairNaive)
                        result = GeometryCalculations.PointsClosestPairBruteForce(points);
                    else
                        throw new Exception("Niepoprawny argument w switchu");

                    var chartDist = new Line
                    {
                        Stroke = Brushes.LightBlue,
                        StrokeThickness = 2,
                        X1 = _middlePoint.X + result.StartPoint.X * _gridDensity,
                        Y1 = _middlePoint.Y - result.StartPoint.Y * _gridDensity,
                        X2 = _middlePoint.X + result.EndPoint.X * _gridDensity,
                        Y2 = _middlePoint.Y - result.EndPoint.Y * _gridDensity,
                        StrokeDashArray = { 2, 4 },
                        Name = "distanceLine"
                    };

                    // Podświetl punkty w tabelce
                    var pointsToSelect = db.ChartElements.Where(el =>
                            el.ElementType == ElementType.Point &&
                            new[] { result.StartPoint.X, result.EndPoint.X }.Contains(el.XStartGridRelative) &&
                            new[] { result.StartPoint.Y, result.EndPoint.Y }.Contains(el.YStartGridRelative))
                        .ToList();
                        
                    RgvData.SelectedItems.Clear();
                    pointsToSelect.ForEach(x => RgvData.SelectedItems.Add(x));
                    RgvData.Focus();

                    // Dodaj wyniki do Labela
                    LblCalculations.Content = new TextBlock
                    {
                        Text = "Pair of Closest Points:\n\n" +
                            $"p1: ({result.StartPoint.X}, {result.StartPoint.Y})\n" +
                            $"p2: ({result.EndPoint.X}, {result.EndPoint.Y})"
                    };

                    // Pokaż punkty na wykresie
                    CanvasChart.Children.Add(chartDist);

                    break;
                }
                case CalculationType.PointsFarthestPair:
                {
                    var points = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Point)
                        .Select(s => new CGPoint(s.XStartGridRelative, s.YStartGridRelative))
                        .ToList();

                    if (points.Count < 2)
                    {
                        MessageBox.Show(
                            $"There must be at least 2 Points on the Chart. Points: {points.Count}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = GeometryCalculations.FarthestPoints(points);

                    // Podświetl punkty w tabelce
                    RgvData.SelectedItems.Clear();
                    foreach (var p in new[] { result.StartPoint, result.EndPoint })
                    {
                        RgvData.SelectedItems.Add(db.ChartElements.Single(el =>
                            el.ElementType == ElementType.Point &&
                            el.XStartGridRelative.Eq(p.X) &&
                            el.YStartGridRelative.Eq(p.Y)));
                    }
                    RgvData.Focus();

                    // Dodaj wyniki do Labela
                    LblCalculations.Content = new TextBlock
                    {
                        Text = "Farthest Pair of Points:\n\n" +
                            $"p1: ({result.StartPoint.X}, {result.StartPoint.Y})\n" +
                            $"p2: ({result.EndPoint.X}, {result.EndPoint.Y})"
                    };

                    // Dodaj wizualizację do wykresu
                    var chartDist = new Line
                    {
                        Stroke = Brushes.LightBlue,
                        StrokeThickness = 2,
                        X1 = _middlePoint.X + result.StartPoint.X * _gridDensity,
                        Y1 = _middlePoint.Y - result.StartPoint.Y * _gridDensity,
                        X2 = _middlePoint.X + result.EndPoint.X * _gridDensity,
                        Y2 = _middlePoint.Y - result.EndPoint.Y * _gridDensity,
                        StrokeDashArray = { 2, 4 },
                        Name = "distanceLine"
                    };
                    CanvasChart.Children.Add(chartDist);

                    break;
                }
                case CalculationType.AngularSorting:
                {
                    var o = new CGPoint(0, 0);
                    var includeZeroPoint = false;
                    if (selectedPointsCount == 1)
                    {
                        var selectedPoint = selectedPoints.Single();
                        o = new CGPoint(selectedPoint.XStartGridRelative, selectedPoint.YStartGridRelative);
                        includeZeroPoint = true;
                    }

                    var points = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Point)
                        .Select(s => new CGPoint(s.XStartGridRelative, s.YStartGridRelative))
                        .ToList();

                    var chartOther = RgvData.Items.Cast<ChartElement>()
                        .Where(i => i.ElementType != ElementType.Point).ToList();

                    if (points.Count < 2)
                    {
                        MessageBox.Show(
                            $"There must be at least 2 Points on the Chart. Points: {points.Count}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = GeometryCalculations.AngularSort(points, o, includeZeroPoint);

                    // Aktualizuj punkty w tabelce
                    var chartSortedPoints = result.Select(el => new ChartElement
                    {
                        ElementType = ElementType.Point,
                        XStart = _middlePoint.X + el.X * _gridDensity,
                        YStart = _middlePoint.Y - el.Y * _gridDensity,
                        XStartGridRelative = el.X,
                        YStartGridRelative = el.Y,
                        PhysicalRepresentation = db.ChartElements.Single(ce => ce.XStartGridRelative.Eq(el.X) && ce.YStartGridRelative.Eq(el.Y)).PhysicalRepresentation
                    }).ToList();

                    db.ChartElements.Clear();
                    chartSortedPoints.ForEach(x => db.ChartElements.Add(x));
                    chartOther.ForEach(x => db.ChartElements.Add(x));

                    // Podświetl punkty w tabelce
                    RgvData.SelectedItems.Clear();
                    chartSortedPoints.ForEach(x => RgvData.SelectedItems.Add(x));
                    RgvData.Focus();

                    // Dodaj wyniki do Labela
                    LblCalculations.Content = new TextBlock { Text = "Points have been sorted Angle Wise. You can select any point to sort relatively to its position." };

                    // Dodaj wizualizację do wykresu
                    var colors = new List<Color>();
                    var cc = new ColorConverter();
                    for (double i = 0; i < 1; i += 1 / (double)chartSortedPoints.Count)
                    {
                        var c = cc.HslToRgb(i, 0.5, 0.5);
                        colors.Add(c);
                    }
                    
                    for (var i = 0; i < chartSortedPoints.Count; i++)
                    {
                        var cp = chartSortedPoints[i];
                        var chartDist = new Line
                        {
                            Stroke = new SolidColorBrush(colors[i]),
                            StrokeThickness = 2,
                            X1 = _middlePoint.X + o.X * _gridDensity,
                            Y1 = _middlePoint.Y - o.Y * _gridDensity,
                            X2 = cp.XStart,
                            Y2 = cp.YStart,
                            StrokeDashArray = {2, 4},
                            Name = "distanceLine"
                        };
                        CanvasChart.Children.Add(chartDist);
                    }

                    break;
                }
                case CalculationType.ConvexHullGraham:
                case CalculationType.ConvehHullJarvis:
                {
                    var points = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Point)
                        .Select(s => new CGPoint(s.XStartGridRelative, s.YStartGridRelative))
                        .ToList();

                    if (points.Count < 3)
                    {
                        MessageBox.Show(
                            $"There must be at least 3 Points on the Chart. Points: {points.Count}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = new List<CGPoint>();
                    if (calculationType == CalculationType.ConvexHullGraham)
                        result = GeometryCalculations.ConvexHullGraham(points);
                    else if (calculationType == CalculationType.ConvehHullJarvis)
                        result = GeometryCalculations.ConvexHullJarvis(points);

                    // Podświetl punkty w tabelce
                    RgvData.SelectedItems.Clear();
                    foreach (var p in result)
                    {
                        RgvData.SelectedItems.Add(db.ChartElements.Single(el =>
                            el.ElementType == ElementType.Point && 
                            el.XStartGridRelative.Eq(p.X) &&
                            el.YStartGridRelative.Eq(p.Y)));
                    }
                    RgvData.Focus();

                    // Dodaj wyniki do Labela
                    LblCalculations.Content = new TextBlock { Text = "Convex Hull has been selected " + (calculationType == CalculationType.ConvexHullGraham ? "Grahama" : "Jarvisa") };

                    // Dodaj wizualizację do wykresu
                    var n = result.Count;
                    for (var i = 0; i < n; i++)
                    {
                        var p1 = result[i % n];
                        var p2 = result[(i + 1) % n];
                        var chartDist = new Line
                        {
                            Stroke = new SolidColorBrush(Colors.BlueViolet),
                            StrokeThickness = 2,
                            X1 = _middlePoint.X + p1.X * _gridDensity,
                            Y1 = _middlePoint.Y - p1.Y * _gridDensity,
                            X2 = _middlePoint.X + p2.X * _gridDensity,
                            Y2 = _middlePoint.Y - p2.Y * _gridDensity,
                            StrokeDashArray = { 2, 4 },
                            Name = "distanceLine"
                        };
                        CanvasChart.Children.Add(chartDist);
                    }

                    break;
                }

                #endregion
                
                #region Punkty Przecięć Odcinków

                case CalculationType.IntersectionsNaive:
                case CalculationType.IntersectionsBentleyOttmann:
                {
                    #region Inicjuj odcinki

                    var segments = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Line)
                        .Select(s =>
                            new LineSegment(
                                new CGPoint(s.XStartGridRelative, s.YStartGridRelative),
                                new CGPoint(s.XEndGridRelative.ToDouble(), s.YEndGridRelative.ToDouble())))
                        .ToList();

                    #endregion

                    #region Sprawdź wstępne warunki

                    if (segments.Count < 2)
                    {
                        MessageBox.Show(
                            $"There should be at least 2 Segemnts on the Chart. Segments: {segments.Count}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    #endregion

                    #region Wykonaj obliczenia

                    var resultExt = new Dictionary<CGPoint, List<LineSegment>>();
                    var result = new List<CGPoint>();
                    var sweepLines = Enumerable.Empty<LineSegment>().ToArray(); 
                    if (calculationType == CalculationType.IntersectionsBentleyOttmann)
                    {
                        resultExt = GeometryCalculations.IntersectionsBentleyOttmann(segments, out sweepLines);
                        result = resultExt.Keys.ToList();
                    }
                    else if (calculationType == CalculationType.IntersectionsNaive)
                        result = GeometryCalculations.IntersectionsNaive(segments).ToList();

                    #endregion

                    #region Podświetl odcinki w tabelce

                    var resultExtFlat = resultExt.SelectMany(x => x.Value).Distinct().ToList();
                    RgvData.SelectedItems.Clear();
                    foreach (var s in resultExtFlat)
                    {
                        RgvData.SelectedItems.Add(db.ChartElements.Where(el =>
                            el.ElementType == ElementType.Line && 
                            el.XStartGridRelative.Eq(s.StartPoint.X) &&
                            el.YStartGridRelative.Eq(s.StartPoint.Y) &&
                            el.XEndGridRelative.Eq(s.EndPoint.X) &&
                            el.YEndGridRelative.Eq(s.EndPoint.Y) ||
                            el.XStartGridRelative.Eq(s.EndPoint.X) &&
                            el.YStartGridRelative.Eq(s.EndPoint.Y) &&
                            el.XEndGridRelative.Eq(s.StartPoint.X) &&
                            el.YEndGridRelative.Eq(s.StartPoint.Y)).First(el => !RgvData.SelectedItems.Contains(el)));
                    }
                    RgvData.Focus();

                    #endregion

                    #region Dodaj wyniki do Labela

                    LblCalculations.Content = new TextBlock { Text = "Selected " + result.Count + " Segment Intersections " + (calculationType == CalculationType.IntersectionsBentleyOttmann ? "(Bentley-Ottmann)" : "(Naive)") };

                    #endregion

                    #region Dodaj punkty przecięć do wykresu

                    foreach (var p in result)
                    {
                        var pointPosition = new Point(
                            _middlePoint.X + p.X * _gridDensity,
                            _middlePoint.Y - p.Y * _gridDensity);
                        var geometry = new EllipseGeometry(pointPosition, 4, 4);
                        var point = new Path
                        {
                            Name = "intersectionPoint",
                            Fill = calculationType == CalculationType.IntersectionsNaive ? Brushes.Green : Brushes.LightBlue,
                            StrokeThickness = 0,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Data = geometry,
                            SnapsToDevicePixels = true
                        };
                        point.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                        CanvasChart.Children.Add(point);
                        Panel.SetZIndex(point, 20);
                    }

                    #endregion

                    #region Dodaj stany miotły do wykresu (przy naiwnym algorytmie tablica będzie pusta))

                    foreach (var sl in sweepLines)
                    {
                        var chartDist = new Line
                        {
                            Stroke = new SolidColorBrush(Colors.BlueViolet),
                            StrokeThickness = 2,
                            X1 = _middlePoint.X + sl.StartPoint.X * _gridDensity,
                            Y1 = _middlePoint.Y - sl.StartPoint.Y * _gridDensity,
                            X2 = _middlePoint.X + sl.EndPoint.X * _gridDensity,
                            Y2 = _middlePoint.Y - sl.EndPoint.Y * _gridDensity,
                            StrokeDashArray = { 2, 4 },
                            Name = "distanceLine"
                        };
                        CanvasChart.Children.Add(chartDist);
                    }

                    #endregion

                    break;
                }

                #endregion

                #region Triangulacja

                case CalculationType.TriangulationIterative:
                case CalculationType.TriangulationDnQ:
                {
                    var points = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Point)
                        .Select(p => new CGPoint(p.XStartGridRelative, p.YStartGridRelative))
                        .ToList();

                    try
                    {
                        var result = new List<LineSegment>();
                        if (calculationType == CalculationType.TriangulationIterative)
                            result = GeometryCalculations.TriangulateIterativeWrapper(points);
                        else if (calculationType == CalculationType.TriangulationDnQ)
                            result = GeometryCalculations.TriangulateDnQWrapper(points);

                        LblCalculations.Content = new TextBlock { Text = "Mesh Triangulation has been displayed on the Chart" };

                        foreach (var s in result)
                        {
                            CanvasChart.Children.Add(new Line
                            {
                                Stroke = new SolidColorBrush(Colors.BlueViolet),
                                StrokeThickness = 2,
                                X1 = _middlePoint.X + s.StartPoint.X * _gridDensity,
                                Y1 = _middlePoint.Y - s.StartPoint.Y * _gridDensity,
                                X2 = _middlePoint.X + s.EndPoint.X * _gridDensity,
                                Y2 = _middlePoint.Y - s.EndPoint.Y * _gridDensity,
                                StrokeDashArray = { 2, 4 },
                                Name = "distanceLine"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    break;
                }

                #endregion

                #region Środek Wielokąta
                
                case CalculationType.PolygonCenter:
                {
                    if (selitemsCount != 1 || selectedPolygonsCount != 1)
                    {
                        MessageBox.Show(
                            $"You must select exactly 1 Polygon, selected: Polygons: {selectedPolygonsCount}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var gvPolygon = selectedPolygons.Single();
                    var polygon = new PolygonFr(gvPolygon.Vertices.Select(p => p.ToPointFraction()));

                    try
                    {
                        var resultN = polygon.Center?.ToPointDouble();
                        CGPoint result;
                        if (resultN != null)
                            result = (CGPoint) resultN;
                        else
                        {
                            LblCalculations.Content = new TextBlock { Text = "Center of the Polygon can't be calculated" };
                            return;
                        }
                        
                        LblCalculations.Content = new TextBlock { Text = $"Center of the Polygon lies at: {result}" };

                        var pointPosition = new Point(
                            _middlePoint.X + result.X * _gridDensity,
                            _middlePoint.Y - result.Y * _gridDensity);
                        var geometry = new EllipseGeometry(pointPosition, 4, 4);
                        var point = new Path
                        {
                            Name = "intersectionPoint",
                            Fill = Brushes.LightBlue,
                            StrokeThickness = 0,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Data = geometry,
                            SnapsToDevicePixels = true
                        };
                        point.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                        CanvasChart.Children.Add(point);
                        Panel.SetZIndex(point, 20);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    break;
                }

                #endregion

                #region Przynależność Punktu do Wielokąta

                case CalculationType.IsPointInsidePolygon:
                case CalculationType.IsPointInsideConvexPolygon:
                {
                    if (selitemsCount != 2 || selectedPointsCount != 1 || selectedPolygonsCount != 1)
                    {
                        MessageBox.Show(
                            $"You need to select 1 Polygon and 1 Point, currently selected: Polygons: {selectedPolygonsCount}, Points: {selectedPointsCount}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var gvPolygon = selectedPolygons.Single();
                    var gvPoint = selectedPoints.Single();

                    var polygon = new Models.Polygon(gvPolygon.Vertices);
                    var point = new CGPoint(gvPoint.XStartGridRelative, gvPoint.YStartGridRelative);

                    try
                    {
                        var result = false;
                        if (calculationType == CalculationType.IsPointInsidePolygon)
                            result = GeometryCalculations.IsInsidePolygonWrapper(point, polygon);
                        else if (calculationType == CalculationType.IsPointInsideConvexPolygon)
                            result = GeometryCalculations.IsInsideConvexPolygonWrapper(point, polygon);

                        LblCalculations.Content = new TextBlock { Text = $"Point{(!result ? " DOESN'T belong" : " belongs")} to the Polygon." };

                        var chartPolygon = (Polygon) gvPolygon.PhysicalRepresentation;
                        var chartPoint = (Path) gvPoint.PhysicalRepresentation;

                        chartPolygon.Stroke = result ? new SolidColorBrush(Color.FromRgb(0, 100, 0)) : new SolidColorBrush(Color.FromRgb(255, 69, 0));
                        chartPolygon.Fill = result ? new SolidColorBrush(Color.FromArgb(64, 0, 50, 0)) : new SolidColorBrush(Color.FromArgb(64, 139, 0, 0));
                        chartPoint.Fill = result ? new SolidColorBrush(Color.FromRgb(0, 150, 0)) : new SolidColorBrush(Color.FromRgb(255, 69, 0));
                        chartPoint.Data = new EllipseGeometry(((EllipseGeometry)chartPoint.Data).Center, 4, 4);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    break;
                }

                #endregion

                #region Voronoi

                case CalculationType.Voronoi:
                {
                    var points = db.ChartElements
                        .Where(i => i.ElementType == ElementType.Point)
                        .Select(p => new CGPoint(p.XStartGridRelative, p.YStartGridRelative))
                        .ToList();

                    try
                    {
                        var result = GeometryCalculations.VoronoiWrapper(points);

                        LblCalculations.Content = new TextBlock { Text = "Voronoi Diagrams have been displayed on the Chart" };

                        foreach (var s in result)
                        {
                            CanvasChart.Children.Add(new Line
                            {
                                Stroke = new SolidColorBrush(Colors.BlueViolet),
                                StrokeThickness = 2,
                                X1 = _middlePoint.X + s.StartPoint.X * _gridDensity,
                                Y1 = _middlePoint.Y - s.StartPoint.Y * _gridDensity,
                                X2 = _middlePoint.X + s.EndPoint.X * _gridDensity,
                                Y2 = _middlePoint.Y - s.EndPoint.Y * _gridDensity,
                                StrokeDashArray = { 2, 4 },
                                Name = "distanceLine"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    break;
                }

                #endregion

                case CalculationType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            UpdateInputs();
            
            var testType = (TestType)RddlTestType.SelectedValue;

            switch (testType)
            {
                #region Para Najbliższych Punktów - SpeedTest

                case TestType.PointsClosestPairSpeedTest:
                {
                    var pointsAmmountDbl = RnumSamples.GetNumericBox().Value.ToDouble();
                    const int threshold = 50;

                    if (pointsAmmountDbl < 2)
                    {
                        MessageBox.Show(
                            $"You need to draw at least 2 Points (samples) for comparison, selected points: {(int) pointsAmmountDbl}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var sb = new StringBuilder();

                    await AsyncUtils.AsyncWithLoader(gridMain, this.LogicalDescendants<Button>().ToArray(), () =>
                    {
                        var pointsAmmount = (int)pointsAmmountDbl;

                        sb.Append("Algorithm Time:\n");

                        var rng = new Random(10);
                        var points = Enumerable.Range(0, pointsAmmount).Select(i =>
                            new CGPoint(
                                Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2),
                                Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2)))
                            .Distinct().ToList();

                        while (points.Count < pointsAmmount)
                        {
                            while (points.Count < pointsAmmount)
                            {
                                points.Add(new Point(
                                    Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2),
                                    Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2)));
                            }
                            points = points.Distinct().ToList();
                        }

                        var sw = Stopwatch.StartNew();
                        var resultBruteForce = GeometryCalculations.PointsClosestPairBruteForce(points).NormalizeEndPoints();
                        sw.Stop();
                        sb.Append($"{sw.Elapsed.TotalMilliseconds} ms - Naive\n");

                        var sw1 = Stopwatch.StartNew();
                        var resultIterative = GeometryCalculations.PointsClosestPairIterative(points).NormalizeEndPoints();
                        sw1.Stop();
                        sb.Append($"{sw1.Elapsed.TotalMilliseconds} ms - Iterative\n");

                        var sw2 = Stopwatch.StartNew();
                        var resultRecursive = GeometryCalculations.PointsClosestPairRecursive(points).NormalizeEndPoints();
                        sw2.Stop();
                        sb.Append($"{sw2.Elapsed.TotalMilliseconds} ms - Recursive (Divide and Conquer)");

                        if (!resultBruteForce.Equals(resultIterative) || !resultBruteForce.Equals(resultRecursive))
                            throw new Exception("One of the algorithms is incorrect");
                    });

                    LblCalculations.Content = new TextBlock { Text = sb.ToString() };

                    break;
                }

                #endregion

                #region Przecięcia Odcinków - SpeedTest

                case TestType.IntersectionsSpeedTest:
                {
                    var segmentsAmmountDbl = RnumSamples.GetNumericBox().Value.ToDouble();
                    const int threshold = 50;

                    if (segmentsAmmountDbl < 2)
                    {
                        MessageBox.Show(
                            $"You need to draw at least 2 Segments (samples) for comparison, selected segments: {(int)segmentsAmmountDbl}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var segmentsAmmount = (int) segmentsAmmountDbl;

                    var sb = new StringBuilder();

                    await AsyncUtils.AsyncWithLoader(gridMain, this.LogicalDescendants<Button>().ToArray(), () =>
                    {
                        sb.Append("Algorithm Time:\n");

                        var rng = new Random();
                        var segments = Enumerable.Range(0, segmentsAmmount).Select(i =>
                            new LineSegment(
                                new Point(
                                    Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2),
                                    Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2)),
                                new Point(
                                    Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2),
                                    Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2))))
                            .Distinct().ToList();

                        while (segments.Count < segmentsAmmount)
                        {
                            while (segments.Count < segmentsAmmount)
                            {
                                segments.Add(new LineSegment(
                                    new Point(
                                        Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2),
                                        Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2)),
                                    new Point(
                                        Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2),
                                        Math.Round(rng.Next(threshold * 2 - 1) + rng.NextDouble() - threshold, 2))));
                            }
                            segments = segments.Distinct().ToList();
                        }

                        var comparer = new PointXThenYComparer();
                        var sw = Stopwatch.StartNew();
                        var resultsNaiveRaw = GeometryCalculations.IntersectionsNaive(segments);

                        sw.Stop();
                        var resultsNaive = resultsNaiveRaw
                            .Select(p => new CGPoint(Math.Round(p.X, 5), Math.Round(p.Y, 5)))
                            .OrderBy(p => p, comparer)
                            .ToList();
                        sb.Append($"{sw.Elapsed.TotalMilliseconds} ms - Naive\n");

                        var sw1 = Stopwatch.StartNew();
                        var resultsBentleyOttmannRaw = GeometryCalculations.IntersectionsBentleyOttmann(segments);
                        sw1.Stop();
                        var resultsBentleyOttmann = resultsBentleyOttmannRaw.Keys
                            .Select(p => new CGPoint(Math.Round(p.X, 5), Math.Round(p.Y, 5)))
                            .OrderBy(p => p, comparer)
                            .ToList();
                        sb.Append($"{sw1.Elapsed.TotalMilliseconds} ms - Bentley-Ottmann\n");

                        var equals = true;
                        for (var i = 0; i < resultsNaive.Count; i++)
                        {
                            if (resultsNaive[i] != resultsBentleyOttmann[i])
                            {
                                equals = false;
                                break;
                            }
                            if (i == resultsNaive.Count - 1)
                                equals = true;
                        }

                        if (!equals)
                            throw new Exception("One of the algorithms is incorrect");
                    });

                    LblCalculations.Content = new TextBlock { Text = sb + "(WARNING: currently the speed test might not be reliable because Bentley-Ottmann calculates using Big Rational Numbers)" };

                    break;
                }

                #endregion

                case TestType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RddlElementType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_rddlElementTypeSelectingProgramatically)
                return;

            var item = (KeyValuePair<string, int>) RddlElementType.SelectedItem;

            if (item.Key == "Point")
            {
                RnumXStart.IsEnabled = true;
                RnumYStart.IsEnabled = true;
                RnumXEnd.IsEnabled = false;
                RnumYEnd.IsEnabled = false;
                BtnModifyElement.IsEnabled = false;
                BtnAddElement.IsEnabled = true;
            }
            else if (item.Key == "Segment")
            {
                RnumXStart.IsEnabled = true;
                RnumYStart.IsEnabled = true;
                RnumXEnd.IsEnabled = true;
                RnumYEnd.IsEnabled = true;
                BtnModifyElement.IsEnabled = false;
                BtnAddElement.IsEnabled = true;
            }
            else if (item.Key == "Polygon")
            {
                RnumXStart.IsEnabled = false;
                RnumYStart.IsEnabled = false;
                RnumXEnd.IsEnabled = false;
                RnumYEnd.IsEnabled = false;
                BtnModifyElement.IsEnabled = false;
                BtnAddElement.IsEnabled = false;
            }
        }

        private static void TxtAll_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.ResetValue();
            (sender as TextBox)?.ClearValue();
        }

        private static void TxtAll_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.ResetValue();
        }

        private void SvYAxis_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void SvXAxis_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void CanvasChart_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                return;
            }

            const int minDensity = 20;
            const int multiplier = 2;
            const int maxZoom = 3;
            var maxDensity = minDensity * Math.Pow(multiplier, maxZoom);
            var pos = e.GetPosition((Canvas)sender);
            var mousePos = new Point(Math.Round(pos.X), Math.Round(pos.Y));
            var posRelSv = e.GetPosition(SvChart);
            var mousePosRelSv = new Point(Math.Round(posRelSv.X), Math.Round(posRelSv.Y));
            var db = (DbContext) RgvData.DataContext;

            if (e.Delta > 0 && _gridDensity < maxDensity)
            {
                mousePos.X *= multiplier;
                mousePos.Y *= multiplier;
                _gridDensity *= multiplier;
                _middlePoint.X *= multiplier;
                _middlePoint.Y *= multiplier;
                CanvasChart.Width *= multiplier;
                CanvasChart.Height *= multiplier;
                GridXAxis.Width = (GridXAxis.Width - 2 * _chartAxisDiff)*multiplier + 2 * _chartAxisDiff;
                GridYAxis.Height = (GridYAxis.Height - 2 * _chartAxisDiff) * multiplier + 2 * _chartAxisDiff;

                foreach (var uielement in CanvasChart.Children)
                {
                    if (uielement is Rectangle r)
                    {
                        Canvas.SetLeft(r, Canvas.GetLeft(r)*multiplier);
                        Canvas.SetTop(r, Canvas.GetTop(r)*multiplier);
                        r.Width *= multiplier;
                        r.Height *= multiplier;
                    }
                    else if (uielement is Line l)
                    {
                        l.X1 *= multiplier;
                        l.X2 *= multiplier;
                        l.Y1 *= multiplier;
                        l.Y2 *= multiplier;
                    }
                    else if (uielement is Path p1)
                    {
                        var pointData = (EllipseGeometry)p1.Data;
                        var geometry = new EllipseGeometry(
                            new Point(
                                pointData.Center.X * multiplier,
                                pointData.Center.Y * multiplier),
                            pointData.RadiusX,
                            pointData.RadiusY);

                        p1.Data = geometry;
                    }
                    else if (uielement is Polygon poly)
                    {
                        var points = new PointCollection();
                        foreach (var p in poly.Points)
                            points.Add(new Point(p.X * multiplier, p.Y * multiplier));
                        poly.Points = points;
                    }
                }

                foreach (var lbl in GridXAxis.Children.OfType<Label>())
                    lbl.Margin = new Thickness(lbl.Margin.Left * multiplier, lbl.Margin.Top, lbl.Margin.Right, lbl.Margin.Bottom);

                foreach (var lbl in GridYAxis.Children.OfType<Label>())
                    lbl.Margin = new Thickness(lbl.Margin.Left, lbl.Margin.Top * multiplier, lbl.Margin.Right, lbl.Margin.Bottom);

                foreach (var gvEl in db.ChartElements)
                {
                    gvEl.XStart *= multiplier;
                    gvEl.YStart *= multiplier;
                    gvEl.XEnd *= multiplier;
                    gvEl.YEnd *= multiplier;
                }

                SvChart.ScrollToHorizontalOffset(mousePos.X - mousePosRelSv.X); // - SvChart.Width / 2
                SvChart.ScrollToVerticalOffset(mousePos.Y - mousePosRelSv.Y); // - SvChart.Height / 2
                SvXAxis.ScrollToHorizontalOffset(mousePos.X - mousePosRelSv.X); // - SvChart.Width / 2
                SvYAxis.ScrollToVerticalOffset(mousePos.Y - mousePosRelSv.Y); // - SvChart.Height / 2
            }
            else if (e.Delta < 0 && _gridDensity > minDensity)
            {
                mousePos.X /= multiplier;
                mousePos.Y /= multiplier;
                _gridDensity /= multiplier;
                _middlePoint.X /= multiplier;
                _middlePoint.Y /= multiplier;
                CanvasChart.Width /= multiplier;
                CanvasChart.Height /= multiplier;
                GridXAxis.Width = (GridXAxis.Width - 2 * _chartAxisDiff) / multiplier + 2 * _chartAxisDiff;
                GridYAxis.Height = (GridYAxis.Height - 2 * _chartAxisDiff) / multiplier + 2 * _chartAxisDiff;

                foreach (var uielement in CanvasChart.Children)
                {
                    if (uielement is Rectangle r)
                    {
                        Canvas.SetLeft(r, Canvas.GetLeft(r) / multiplier);
                        Canvas.SetTop(r, Canvas.GetTop(r) / multiplier);
                        r.Width /= multiplier;
                        r.Height /= multiplier;
                    }
                    else if (uielement is Line l)
                    {
                        l.X1 /= multiplier;
                        l.X2 /= multiplier;
                        l.Y1 /= multiplier;
                        l.Y2 /= multiplier;
                    }
                    else if (uielement is Path p1)
                    {
                        var pointData = (EllipseGeometry)p1.Data;
                        var geometry = new EllipseGeometry(
                            new Point(
                                pointData.Center.X / multiplier,
                                pointData.Center.Y / multiplier),
                            pointData.RadiusX,
                            pointData.RadiusY);

                        p1.Data = geometry;
                    }
                    else if (uielement is Polygon poly)
                    {
                        var points = new PointCollection();
                        foreach (var p in poly.Points)
                            points.Add(new Point(p.X / multiplier, p.Y / multiplier));
                        poly.Points = points;
                    }
                }

                foreach (var lbl in GridXAxis.Children.OfType<Label>())
                    lbl.Margin = new Thickness(lbl.Margin.Left / multiplier, lbl.Margin.Top, lbl.Margin.Right, lbl.Margin.Bottom);

                foreach (var lbl in GridYAxis.Children.OfType<Label>())
                    lbl.Margin = new Thickness(lbl.Margin.Left, lbl.Margin.Top / multiplier, lbl.Margin.Right, lbl.Margin.Bottom);

                foreach (var gvEl in db.ChartElements)
                {
                    gvEl.XStart /= multiplier;
                    gvEl.YStart /= multiplier;
                    gvEl.XEnd /= multiplier;
                    gvEl.YEnd /= multiplier;
                }

                SvChart.ScrollToHorizontalOffset(mousePos.X - mousePosRelSv.X);
                SvChart.ScrollToVerticalOffset(mousePos.Y - mousePosRelSv.Y);
                SvXAxis.ScrollToHorizontalOffset(mousePos.X - mousePosRelSv.X);
                SvYAxis.ScrollToVerticalOffset(mousePos.Y - mousePosRelSv.Y);
            }

            e.Handled = true;
        }

        private void RgvData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_deletingRow || _selectingRowsProgramatically)
                return;

            UpdateInputs();
        }

        private void RgvData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                _deletingRow = true;

                var selectedElements = SelectedElements();
                selectedElements.ForEach(x => CanvasChart.Children.Remove(x));
                var db = (DbContext)DataContext;
                foreach (var item in RgvData.SelectedItems.Cast<ChartElement>().ToArray())
                    db.ChartElements.Remove(item);

                UpdateInputs();

                _deletingRow = false;
            }
        }

        private void RddlCalculationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedId = ((DdlItem) ((ComboBox) sender).SelectedItem).Index;
            BtnCalculate.IsEnabled = selectedId != -1;

            UpdateInputs();
        }

        private void RddlTestType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedId = ((DdlItem) ((ComboBox) sender).SelectedItem).Index;
            BtnTest.IsEnabled = selectedId != -1;

            UpdateInputs();
        }

        private void UpdateInputs()
        {
            _rddlElementTypeSelectingProgramatically = true;

            if (RnumXStart.IsEnabled == false) RnumXStart.IsEnabled = true;
            if (RnumYStart.IsEnabled == false) RnumYStart.IsEnabled = true;
            if (RnumXEnd.IsEnabled == false) RnumXEnd.IsEnabled = true;
            if (RnumYEnd.IsEnabled == false) RnumYEnd.IsEnabled = true;
            if (BtnModifyElement.IsEnabled == false) BtnModifyElement.IsEnabled = true;
            if (BtnAddElement.IsEnabled == false) BtnAddElement.IsEnabled = true;

            if (RgvData.SelectedItems.Count == 1)
            {
                var selectedChartElement = RgvData.SelectedItems.Cast<ChartElement>().Single();
                RddlElementType.SelectedItem = RddlElementType.Items.Cast<KeyValuePair<string, int>>().Single(x => string.Equals(x.Key, selectedChartElement.ElementTypeName, StringComparison.OrdinalIgnoreCase));
                RddlElementType.IsEnabled = false;
                RnumXStart.GetNumericBox().Value = selectedChartElement.XStartGridRelative.ToDecimal();
                RnumYStart.GetNumericBox().Value = selectedChartElement.YStartGridRelative.ToDecimal();
                RnumXEnd.GetNumericBox().Value = selectedChartElement.XEndGridRelative.ToDecimalN() ?? 0;
                RnumYEnd.GetNumericBox().Value = selectedChartElement.YEndGridRelative.ToDecimalN() ?? 0;
                BtnAddElement.IsEnabled = false;

                if (selectedChartElement.ElementType == ElementType.Point)
                {
                    RnumXEnd.IsEnabled = false;
                    RnumYEnd.IsEnabled = false;
                }
                else if (selectedChartElement.ElementType == ElementType.Polygon)
                {
                    RnumXStart.IsEnabled = false;
                    RnumYStart.IsEnabled = false;
                    RnumXEnd.IsEnabled = false;
                    RnumYEnd.IsEnabled = false;
                    BtnModifyElement.IsEnabled = false;
                }
            }
            else
            {
                RddlElementType.SelectedValue = -1;
                RddlElementType.IsEnabled = true;
                RnumXStart.GetNumericBox().Value = 0;
                RnumYStart.GetNumericBox().Value = 0;
                RnumXEnd.GetNumericBox().Value = 0;
                RnumYEnd.GetNumericBox().Value = 0;
                BtnAddElement.IsEnabled = true;
            }

            LblCalculations.Content = null;
            CanvasChart.Children.OfType<Rectangle>().ToList().ForEach(x => CanvasChart.Children.Remove(x));
            CanvasChart.Children.OfType<Line>().Where(x => x.Name == "distanceLine").ToList().ForEach(x => CanvasChart.Children.Remove(x));
            CanvasChart.Children.OfType<Path>().Where(x => x.Name == "intersectionPoint").ToList().ForEach(x => CanvasChart.Children.Remove(x));

            var chartSelected = RgvData.SelectedItems.Cast<ChartElement>().Select(el => el.PhysicalRepresentation).ToList();
            CanvasChart.Children.OfType<Line>().Where(x => !new[] { "gridLineVertical", "gridLineHorizontal", "distanceLine" }.Contains(x.Name)).ToList().ForEach(x => x.Stroke = chartSelected.Contains(x) ? new SolidColorBrush(Color.FromRgb(255, 97, 97)) : Brushes.White);
            CanvasChart.Children.OfType<Path>().Where(x => x.Name != "intersectionPoint").ToList().ForEach(x =>
            {
                x.Fill = chartSelected.Contains(x) ? new SolidColorBrush(Color.FromRgb(255, 97, 97)) : Brushes.Yellow;
                x.Data = new EllipseGeometry(((EllipseGeometry) x.Data).Center, 2.5, 2.5);
            });
            CanvasChart.Children.OfType<Polygon>().ToList().ForEach(x =>
            {
                x.Fill = chartSelected.Contains(x) ? new SolidColorBrush(Color.FromArgb(16, 128, 128, 128)) : new SolidColorBrush(Color.FromArgb(16, 0, 0, 255));
                x.Stroke = chartSelected.Contains(x) ? new SolidColorBrush(Color.FromRgb(255, 97, 97)) : Brushes.LightBlue;
            });

            _rddlElementTypeSelectingProgramatically = false;
        }

        private void TxtLoadFromFile_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) 
                ? DragDropEffects.Copy 
                : DragDropEffects.None;
        }

        private void TxtLoadFromFile_Drop(object sender, DragEventArgs e)
        {
            var fmt = new NumberFormatInfo
            {
                NegativeSign = "-"
            };

            var db = (DbContext)DataContext;
            var txt = (TextBox) sender;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            var url = files[0];
            txt.Text = url;

            try
            {
                var chartElements = new ObservableCollection<ChartElement>();
                var uiElements = new C5.HashSet<UIElement>();

                var doc = XDocument.Load(url);
                var root = doc.Element("ChartData");
                var xSegments = root?.Elements("Segment").ToList();
                var xPoints = root?.Elements("Point").ToList();
                var xPolygons = root?.Elements("Polygon").ToList();
                var totalElements = xPoints?.Count + xSegments?.Count + xPolygons?.Count;
                if (totalElements == null)
                    throw new XmlException("Brak danych");
                var dupes = 0;
                var tooBig = 0;

                foreach (var xs in xSegments)
                {
                    var xEndPoints = xs.Elements("EndPoint").ToList();
                    if (xEndPoints.Count != 2)
                        throw new XmlException("Niepoprawna liczba punktów dla odcinka");

                    var xP1 = xEndPoints.First();
                    var xP2 = xEndPoints.Last();

                    var p1x = Math.Round(Convert.ToDouble(xP1.Attribute("x").Value, fmt), 2);
                    var p1y = Math.Round(Convert.ToDouble(xP1.Attribute("y").Value, fmt), 2);
                    var p2x = Math.Round(Convert.ToDouble(xP2.Attribute("x").Value, fmt), 2);
                    var p2y = Math.Round(Convert.ToDouble(xP2.Attribute("y").Value, fmt), 2);

                    if (db.ChartElements.Count(x => x.XStartGridRelative.Eq(p1x) && x.YStartGridRelative.Eq(p1y)) != 0)
                    {
                        dupes++;
                        continue;
                    }

                    if (new[] { p1x, p1y, p2x, p2y }.Any(coord => Math.Abs(coord) > 50))
                    {
                        tooBig++;
                        continue;
                    }

                    var segment = new Line
                    {
                        Stroke = Brushes.White,
                        StrokeThickness = 3,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        SnapsToDevicePixels = true,
                        X1 = _middlePoint.X + p1x * _gridDensity,
                        Y1 = _middlePoint.Y - p1y * _gridDensity,
                        X2 = _middlePoint.X + p2x * _gridDensity,
                        Y2 = _middlePoint.Y - p2y * _gridDensity
                    };
                    segment.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                    Panel.SetZIndex(segment, 11);
                    segment.MouseDown += Line_MouseDown;

                    chartElements.Add(new ChartElement
                    {
                        ElementType = ElementType.Line,
                        XStart = _middlePoint.X + p1x * _gridDensity,
                        YStart = _middlePoint.Y - p1y * _gridDensity,
                        XStartGridRelative = p1x,
                        YStartGridRelative = p1y,
                        XEnd = _middlePoint.X + p2x * _gridDensity,
                        YEnd = _middlePoint.Y - p2y * _gridDensity,
                        XEndGridRelative = p2x,
                        YEndGridRelative = p2y,
                        PhysicalRepresentation = segment
                    });
                    uiElements.Add(segment);
                }

                foreach (var xp in xPoints)
                {
                    var x = Math.Round(Convert.ToDouble(xp.Attribute("x").Value, fmt), 2);
                    var y = Math.Round(Convert.ToDouble(xp.Attribute("y").Value, fmt), 2);
                        
                    if (db.ChartElements.Count(el => el.XStartGridRelative.Eq(x) && el.YStartGridRelative.Eq(y)) != 0)
                    {
                        dupes++;
                        continue;
                    }

                    if (new[] { x, y }.Any(coord => Math.Abs(coord) > 50))
                    {
                        tooBig++;
                        continue;
                    }

                    var pointPosition = new Point(
                        _middlePoint.X + x * _gridDensity,
                        _middlePoint.Y - y * _gridDensity);
                    var geometry = new EllipseGeometry(pointPosition, 2.5, 2.5);

                    var point = new Path
                    {
                        Fill = Brushes.Yellow,
                        StrokeThickness = 0,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Data = geometry,
                        SnapsToDevicePixels = true
                    };
                    point.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                    Panel.SetZIndex(point, 21);
                    point.MouseDown += Point_MouseDown;

                    chartElements.Add(new ChartElement
                    {
                        ElementType = ElementType.Point,
                        XStart = _middlePoint.X + x * _gridDensity,
                        YStart = _middlePoint.Y - y * _gridDensity,
                        XStartGridRelative = x,
                        YStartGridRelative = y,
                        PhysicalRepresentation = point
                    });
                    uiElements.Add(point);
                }

                foreach (var xPoly in xPolygons)
                {
                    var xVertices = xPoly.Elements("Vertex").ToList();
                    if (xVertices.Count < 3)
                        throw new XmlException("Niepoprawna liczba wierzchołków dla wielokąta");

                    var vertices = new List<CGPoint>();
                    foreach (var v in xVertices)
                    {
                        var px = Math.Round(Convert.ToDouble(v.Attribute("x").Value, fmt), 2);
                        var py = Math.Round(Convert.ToDouble(v.Attribute("y").Value, fmt), 2);
                        vertices.Add(new CGPoint(px, py));
                    }

                    if (db.ChartElements.Count(x => x.ElementType == ElementType.Polygon && x.Vertices.SequenceEqual(vertices)) != 0)
                    {
                        dupes++;
                        continue;
                    }

                    if (vertices.SelectMany(v => new[] { v.X, v.Y }).Any(coord => Math.Abs(coord) > 50))
                    {
                        tooBig++;
                        continue;
                    }

                    var polygon = new Polygon
                    {
                        Stroke = Brushes.LightBlue,
                        Fill = new SolidColorBrush(Color.FromArgb(16, 0, 0, 255)),
                        StrokeThickness = 3,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        SnapsToDevicePixels = true,
                        Points = new PointCollection(vertices.Select(v => new Point(
                                _middlePoint.X + v.X * _gridDensity,
                                _middlePoint.Y - v.Y * _gridDensity
                            )))
                    };
                    polygon.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                    Panel.SetZIndex(polygon, 11);
                    polygon.MouseDown += Polygon_MouseDown;

                    chartElements.Add(new ChartElement
                    {
                        ElementType = ElementType.Polygon,
                        XStart = _middlePoint.X + vertices[0].X * _gridDensity,
                        YStart = _middlePoint.Y - vertices[0].Y * _gridDensity,
                        XStartGridRelative = vertices[0].X,
                        YStartGridRelative = vertices[0].Y,
                        PhysicalRepresentation = polygon,
                        Vertices = vertices
                    });
                    uiElements.Add(polygon);
                }

                var polygons = chartElements.Where(ce => ce.ElementType == ElementType.Polygon).Select(cp =>
                    new Models.Polygon(cp.Vertices)).Concat(db.ChartElements.Where(ce => ce.ElementType == ElementType.Polygon).Select(cp =>
                    new Models.Polygon(cp.Vertices))).ToList();
                for (var i = 0; i < polygons.Count - 1; i++)
                    for (var j = i + 1; j < polygons.Count; j++)
                        if (polygons[i].Contains(polygons[j]) || polygons[j].Contains(polygons[i]))
                            throw new Exception("Wielokąty nie mogą się w sobie zawierać");
                var edges = polygons.SelectMany(poly => poly.Edges(true)).Distinct().ToList();
                var endpoints = edges.SelectMany(edge => edge.EndPoints()).Distinct().ToList();
                if (GeometryCalculations.IntersectionsBentleyOttmannPoints(edges).Count(ip => !endpoints.Contains(ip)) > 0)
                    throw new XmlException("Wielokąty nie mogą się przecinać");
                
                foreach (var ce in chartElements)
                    db.ChartElements.Add(ce);
                foreach (var uie in uiElements)
                    CanvasChart.Children.Add(uie);

                RgvData.ItemsSource = db.ChartElements;
                RgvData.SelectedItems.Clear();
                UpdateInputs();

                LblCalculations.Content = new TextBlock
                {
                    Text = "Successfully added elements from file to the Chart.\n" +
                           $"Added: {totalElements - dupes - tooBig}\n" +
                           (dupes != 0 ? $"Skipped Duplicates: {dupes}\n" : string.Empty) +
                           (tooBig != 0 ? $"Outside of the Chart: {tooBig}" : string.Empty)
                };
            }
            catch (Exception ex) // when (ex is XmlException || ex is IOException)
            {
                LblCalculations.Content = new TextBlock
                {
                    Text = ex.Message
                };
            }
        }

        private void TxtLoadFromFile_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void TxtSaveToFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is FrameworkElement txtSaveToFile)) return;

            var root = new XElement("ChartData");
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            var db = (DbContext)DataContext;

            foreach (var ce in db.ChartElements.OrderBy(ce => ce.ElementType))
            {
                if (ce.ElementType == ElementType.Point)
                {
                    root.Add(new XElement("Point",
                        new XAttribute("x", ce.XStartGridRelative),
                        new XAttribute("y", ce.YStartGridRelative)));
                }
                else if (ce.ElementType == ElementType.Line && ce.XEndGridRelative != null && ce.YEndGridRelative != null)
                {
                    root.Add(new XElement("Segment",
                        new XElement("EndPoint",
                            new XAttribute("x", ce.XStartGridRelative),
                            new XAttribute("y", ce.YStartGridRelative)),
                        new XElement("EndPoint",
                            new XAttribute("x", ce.XEndGridRelative),
                            new XAttribute("y", ce.YEndGridRelative))
                        )
                    );
                }
                else if (ce.ElementType == ElementType.Polygon)
                {
                    var xPolygon = new XElement("Polygon");
                    foreach (var v in ce.Vertices)
                        xPolygon.Add(new XElement("Vertex",
                            new XAttribute("x", v.X),
                            new XAttribute("y", v.Y)));
                    root.Add(xPolygon);
                }
            }

            var enc = Encoding.UTF8;
            var m = new MemoryStream();
            using (var tx = XmlWriter.Create(m, new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                ConformanceLevel = ConformanceLevel.Document,
                Encoding = enc,
                Indent = true,
            }))
            {
                doc.WriteTo(tx);
            }

            var fd = new FileDescriptor
            {
                Name = "ChartData.xml",
                Contents = m.ToArray()
            };
            var fullPath = SaveFIleToTemp(fd);
            var dragObj = new DataObject();
            dragObj.SetFileDropList(new StringCollection { fullPath });
            DragDrop.DoDragDrop(txtSaveToFile, dragObj, DragDropEffects.Copy);
        }

        private static string SaveFIleToTemp(FileDescriptor file)
        {
            var tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), file.Name);
            Stream fs = File.Create(tempFilePath);
            new BinaryWriter(fs).Write(file.Contents);
            fs.Close();
            return tempFilePath;
        }

        private List<UIElement> SelectedElements()
        {
            return RgvData.SelectedItems.Cast<ChartElement>().Select(el => el.PhysicalRepresentation).ToList();
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {  }

        private void btnMinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(1500);
        }

        private void btnMinimizeToTray_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = new SolidColorBrush(Color.FromRgb(0, 0, 180));
        }

        private void btnMinimizeToTray_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = Brushes.Transparent;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMinimize_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = new SolidColorBrush(Color.FromRgb(76, 76, 76));
        }

        private void btnMinimize_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = Brushes.Transparent;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnClose_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = new SolidColorBrush(Color.FromRgb(76, 76, 76));
            ((Button)sender).Foreground = Brushes.Black;
        }

        private void btnClose_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = Brushes.Transparent;
            ((Button)sender).Foreground = Brushes.White;
        }

        private bool _restoreForDragMove;

        private void gridTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (ResizeMode != ResizeMode.CanResize && ResizeMode != ResizeMode.CanResizeWithGrip)
                    return;

                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                _restoreForDragMove = WindowState == WindowState.Maximized;
                DragMove();
            }
        }

        private void gridTitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_restoreForDragMove && e.LeftButton == MouseButtonState.Pressed)
            {
                _restoreForDragMove = false;

                var wndMousePos = e.MouseDevice.GetPosition(this);
                var screenMousePos = this.WindowPointToScreen(wndMousePos);

                Left = screenMousePos.X - Width / (ActualWidth / wndMousePos.X);
                Top = screenMousePos.Y - Height / (ActualHeight / wndMousePos.Y);

                WindowState = WindowState.Normal;

                DragMove();
            }
        }

        private void gridTitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _restoreForDragMove = false;
        }

        private void gridTitleBar_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Grid)sender).Highlight(((SolidColorBrush)FindResource("MouseOverTitleBarBrush")).Color);
        }

        private void gridTitleBar_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Grid)sender).Highlight(((SolidColorBrush)FindResource("DefaultWindowBrush")).Color);
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            _notifyIcon.Visible = false;
            WindowState = WindowState.Normal;

            if (IsVisible)
                Activate();
            else
                Show();
        }
    }

    public enum TestType
    {
        None = -1,
        PointsClosestPairSpeedTest,
        IntersectionsSpeedTest
    }

    public enum CalculationType
    {
        None = -1,
        PointOrientationTest,
        PointMembershipTest,
        LineRectangularBoundsIntersection,
        LineIntersection,
        PointsClosestPairIterative,
        PointsClosestPairRecursive,
        PointsClosestPairNaive,
        PointsFarthestPair,
        AngularSorting,
        ConvexHullGraham,
        ConvehHullJarvis,
        IntersectionsBentleyOttmann,
        IntersectionsNaive,
        TriangulationDnQ,
        TriangulationIterative,
        IsPointInsidePolygon,
        IsPointInsideConvexPolygon,
        PolygonCenter,
        Voronoi
    }
}
