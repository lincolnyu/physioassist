using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PhysioControls.Utilities;

namespace PhysioControls.Behavioural
{
    public static class Expand
    {
        #region Fields

        public static readonly DependencyProperty IsExpandableProperty =
            DependencyProperty.RegisterAttached("IsExpandable", typeof (bool), typeof (Expand),
                                                new PropertyMetadata(false, IsExpandableChangedCallback));

        public static readonly DependencyProperty ExpandHandlerProperty =
            DependencyProperty.RegisterAttached("ExpandHandler", typeof(IExpandHandler), typeof(Expand));

        #endregion

        #region Methods

        public static bool GetIsExpandable(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsExpandableProperty);
        }

        public static void SetIsExpandable(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsExpandableProperty, value);
        }

        public static IExpandHandler GetExpandHandler(DependencyObject dependencyObject)
        {
            return (IExpandHandler) dependencyObject.GetValue(ExpandHandlerProperty);
        }

        public static void SetExpandHandler(DependencyObject dependencyObject, IExpandHandler value)
        {
            dependencyObject.SetValue(ExpandHandlerProperty, value);
        }

        private static IExpandHandler GetExpandHandler2(DependencyObject dependencyObject)
        {
            return dependencyObject.GetPropertyWhereAvailable<Canvas, IExpandHandler>(ExpandHandlerProperty);
        }

        private static void IsExpandableChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = (UIElement)dependencyObject;
            
            if ((bool)e.NewValue)
            {
                uiElement.PreviewMouseLeftButtonDown += ExpandableOnPreviewMouseLeftButtonDown;
            }
            else
            {
                uiElement.PreviewMouseLeftButtonDown -= ExpandableOnPreviewMouseLeftButtonDown;
            }
        }

        private static void ExpandableOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;

            var uiElement = (UIElement)sender;
            var point = e.GetPosition(uiElement);
            var hitElement = uiElement.InputHitTest(point);
            var hitViewModel = ((FrameworkElement)hitElement).DataContext;

            var handler = GetExpandHandler2(uiElement);

            var args = new ExpandEventArgs
                {
                    HitElement = hitElement as UIElement,
                    HitViewModel = hitViewModel,
                };

            handler.ToggleExpansion(args);
        }

        #endregion

    }
}
