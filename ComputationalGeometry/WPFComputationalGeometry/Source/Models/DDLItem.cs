namespace WPFComputationalGeometry.Source.Models
{
    public class DdlItem
    {
        public int Index { get; set; }
        public string Text { get; set; }

        public DdlItem(int index, string text)
        {
            Index = index;
            Text = text;
        }
    }
}
