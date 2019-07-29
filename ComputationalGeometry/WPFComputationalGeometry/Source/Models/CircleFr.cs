namespace WPFComputationalGeometry.Source.Models
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
