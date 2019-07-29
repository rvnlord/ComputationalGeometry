using System.Collections.ObjectModel;

namespace WPFComputationalGeometry.Source.Models
{
    public class DbContext
    {
        public ObservableCollection<ChartElement> ChartElements
        {
            get => ChartElement.AllChartElements;
            set => ChartElement.AllChartElements = value;
        }

        public DbContext()
        {
            ChartElements = ChartElements == null || ChartElements.Count <= 0
                ? new ObservableCollection<ChartElement>()
                : ChartElement.AllChartElements;
        }
    }
}
