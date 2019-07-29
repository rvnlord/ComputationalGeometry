namespace WPFComputationalGeometry.Source.Models
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
