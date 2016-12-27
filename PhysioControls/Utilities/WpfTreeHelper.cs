using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PhysioControls.Utilities
{
    public static class WpfTreeHelper
    {
        #region Methods

        public static T FindAnsestralTreeObject<T>(this DependencyObject initial) where T : class
        {
            var current = initial;

            while (current != null)
            {
                if (current is Visual || current is Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    // If we're in Logical Land then we must walk 
                    // up the logical tree until we find a 
                    // Visual/Visual3D to get us back to Visual Land.
                    current = LogicalTreeHelper.GetParent(current);
                }
                var result = current as T;
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static T FindChildWithName<T>(this DependencyObject initial, string name) where T : FrameworkElement
        {
            var q = new Queue<DependencyObject>();
            var o = initial;

            while (true)
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
                {
                    var c = VisualTreeHelper.GetChild(o, i);
                    if (c == null) continue;
                    var f = c as T;
                    if (f != null && f.Name == name)
                    {
                        return f;
                    }
                    
                    q.Enqueue(c);
                }

                foreach (var c in LogicalTreeHelper.GetChildren(o))
                {
                    var d = c as DependencyObject;
                    if (d == null) continue;
                    var f = c as T;
                    if (f != null && f.Name == name)
                    {
                        return f;
                    }
                    q.Enqueue(d);
                }

                if (q.Count > 0)
                {
                    o = q.Dequeue();
                }
                else
                {
                    return null;
                }
            }
        }


        public static T GetPropertyWhereAvailable<TElement, T>(this DependencyObject dependencyObject, 
            DependencyProperty dependencyProperty) where TElement : DependencyObject where T : class
        {
            var uiElement = dependencyObject as TElement ?? dependencyObject.FindAnsestralTreeObject<TElement>();

            while (uiElement != null)
            {
                var value = uiElement.GetValue(dependencyProperty) as T;
                if (value != null) return value;
                uiElement = uiElement.FindAnsestralTreeObject<TElement>();
            }
            return null;
        }

        #endregion
    }
}
