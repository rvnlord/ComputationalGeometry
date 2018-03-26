using System.Collections.ObjectModel;

namespace WPFDemo.Models
{
    public class DbContext
    {
        public ObservableCollection<ChartElement> ChartElements
        {
            get { return ChartElement.AllChartElements; }
            set { ChartElement.AllChartElements = value; }
        }

        public DbContext()
        {
            ChartElements = ChartElements == null || ChartElements.Count <= 0
                ? new ObservableCollection<ChartElement>()
                : ChartElement.AllChartElements;
        }
    }
}
