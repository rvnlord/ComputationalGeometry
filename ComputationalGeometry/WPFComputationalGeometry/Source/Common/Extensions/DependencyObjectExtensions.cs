using System.Collections.Generic;
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

        public static IEnumerable<T> LogicalDescendants<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            foreach (var rawChild in LogicalTreeHelper.GetChildren(depObj))
            {
                if (!(rawChild is DependencyObject depObjRawChild)) continue;
                var child = depObjRawChild;
                if (child is T tChild)
                    yield return tChild;

                foreach (var childOfChild in LogicalDescendants<T>(child))
                    yield return childOfChild;
            }
        }
    }
}
