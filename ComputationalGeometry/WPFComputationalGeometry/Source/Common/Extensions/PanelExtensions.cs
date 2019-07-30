using System.Windows.Controls;
using WPFComputationalGeometry.Source.Common.Utils;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class PanelExtensions
    {
        public static void ShowLoader(this Panel control)
        {
            AsyncUtils.ShowLoader(control);
        }

        public static void HideLoader(this Panel control)
        {
            AsyncUtils.HideLoader(control);
        }

        public static bool HasLoader(this Panel control)
        {
            return AsyncUtils.HasLoader(control);
        }
    }
}