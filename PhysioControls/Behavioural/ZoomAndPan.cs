using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PhysioControls.Behavioural
{
    public static class ZoomAndPan
    {
        #region Attachable properties

        public static readonly DependencyProperty IsZoomAndPannable =
            DependencyProperty.RegisterAttached("IsZoomAndPannable", typeof (bool),
                                                typeof (ZoomAndPan),
                                                new PropertyMetadata(default(bool), IsZoomAndPannableChangedCallback));

        public static readonly DependencyProperty ZoomAndPanHandlerProperty =
            DependencyProperty.RegisterAttached("ZoomAndPanHandler", typeof(IZoomAndPanHandler),
                                                typeof(ZoomAndPan),
                                                new PropertyMetadata(null, ZoomAndPanHandlerChangedCallback));

        #endregion

        #region Getters and setters of attachable properties

        public static bool GetIsZoomAndPannable(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsZoomAndPannable);
        }

        public static void SetIsZoomAndPannable(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsZoomAndPannable, value);
        }

        public static IZoomAndPanHandler GetZoomAndPanHandler(DependencyObject dependencyObject)
        {
            return (IZoomAndPanHandler)dependencyObject.GetValue(ZoomAndPanHandlerProperty);
        }

        public static void SetZoomAndPanHandler(DependencyObject dependencyObject, IZoomAndPanHandler value)
        {
            dependencyObject.SetValue(ZoomAndPanHandlerProperty, value);
        }

        #endregion

        #region Methods

        private static void IsZoomAndPannableChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = (UIElement)d;

            if ((bool)e.NewValue)
            {
                uiElement.PreviewMouseLeftButtonDown += ZoomAndPannableOnPreviewMouseLeftButtonDown;
                uiElement.PreviewMouseLeftButtonUp += ZoomAndPannableOnPreviewMouseLeftButtonUp;
                uiElement.PreviewMouseMove += ZoomAndPannableOnPreviewMouseMove;
                uiElement.PreviewMouseWheel += ZoomAndPannableOnPreviewMouseWheel;
            }
            else
            {
                uiElement.PreviewMouseLeftButtonDown -= ZoomAndPannableOnPreviewMouseLeftButtonDown;
                uiElement.PreviewMouseLeftButtonUp -= ZoomAndPannableOnPreviewMouseLeftButtonUp;
                uiElement.PreviewMouseMove -= ZoomAndPannableOnPreviewMouseMove;
                uiElement.PreviewMouseWheel -= ZoomAndPannableOnPreviewMouseWheel;
            }
        }

        private static void ZoomAndPanHandlerChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = (UIElement)d;

            if (e.NewValue != null)
            {
                if (!_zoomAndPanTargets.ContainsKey(uiElement))
                {
                    _zoomAndPanTargets[uiElement] = new ZoomAndPanInfo();
                }
                var info = _zoomAndPanTargets[uiElement];
                var handler = (IZoomAndPanHandler)e.NewValue;
                info.ZoomAndPanHandler = handler;
                handler.ExposeInfo(info);
            }
            else
            {
                if (_zoomAndPanTargets.ContainsKey(uiElement))
                {
                    _zoomAndPanTargets[uiElement].ZoomAndPanHandler = null;
                    if (!GetIsZoomAndPannable(d))
                    {
                        _zoomAndPanTargets.Remove(uiElement);
                    }
                }
            }
        }

        private static void ZoomAndPannableOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var uiElement = (UIElement) sender;
            var point = e.GetPosition(uiElement);

            var info = _zoomAndPanTargets[uiElement];

            info.ZoomAndPanHandler.WheelChanged(point, e.Delta);
        }

        private static void ZoomAndPannableOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var info = _zoomAndPanTargets[uiElement];

            var handler = info.ZoomAndPanHandler;

            if (!handler.IsPanEnabled(e)) return;

            info.IsPanning = true;
            var point = e.GetPosition(uiElement);
            info.ZoomAndPanHandler.PanBegin(point);
        }

        private static void ZoomAndPannableOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var info = _zoomAndPanTargets[uiElement];
            if (!info.IsPanning) return;

            var point = e.GetPosition(uiElement);
            DoPanEnd(info, point);
        }

        private static void DoPanEnd(ZoomAndPanInfo info, Point point)
        {
            info.ZoomAndPanHandler.PanEnd(point);
            info.IsPanning = false;
        }

        private static void ZoomAndPannableOnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var info = _zoomAndPanTargets[uiElement];
            if (!info.IsPanning) return;

            var point = e.GetPosition(uiElement);
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                DoPanEnd(info, point);
                return;
            }

            info.ZoomAndPanHandler.PanMove(point);
        }

        #endregion

        #region Fields

        /// <summary>
        ///  A map from UI element that contains selectable objects to its corresponding SelectInfo
        /// </summary>
        private static readonly IDictionary<UIElement, ZoomAndPanInfo> _zoomAndPanTargets =
            new Dictionary<UIElement, ZoomAndPanInfo>();


        #endregion
    }
}
