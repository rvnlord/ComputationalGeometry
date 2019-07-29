using System.Windows;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class DependencyObjectExtensions
    {
        public static T LogicalAncestor<T>(this DependencyObject child) where T : DependencyObject
        {
            while (true)
            {
                var parentObject = LogicalTreeHelper.GetParent(child);
                if (parentObject == null) return null;
                if (parentObject is T parent) return parent;
                child = parentObject;
            }
        }
    }
}
