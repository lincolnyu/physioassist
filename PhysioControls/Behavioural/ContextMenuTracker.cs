using System.Windows;
using System.Windows.Controls;

namespace PhysioControls.Behavioural
{
    /// <summary>
    ///  A class that provides attached properties to record mouse click location when the context menu pops up
    ///  for XML use
    /// </summary>
    /// <remarks>
    ///  This is following what was suggested by the excellent stackoverflow post which provided a couple of
    ///  approaches to tracking context menu click for XAML to fire command with parameters,
    ///   http://stackoverflow.com/questions/1009571/passing-origin-of-contextmenu-into-wpf-command
    /// </remarks>
    public static class ContextMenuTracker
    {
        #region Fields

        /// <summary>
        ///  Attached property indicating whether tracking is enabled, which needs to be switched on
        ///  at the context menu definition tag in the XAML
        /// </summary>
        public static readonly DependencyProperty TrackOpenLocationProperty =
            DependencyProperty.RegisterAttached("TrackOpenLocation", typeof (bool), typeof (ContextMenuTracker),
                                                new UIPropertyMetadata(false, TrackOpenLocationChangedCallback));

        /// <summary>
        ///  Attached property indicating where the mouse clicked and thus brought up the context menu
        ///  which needs to be obtained at the related menu items and passed on as command parameter
        /// </summary>
        public static readonly DependencyProperty OpenLocationProperty =
            DependencyProperty.RegisterAttached("OpenLocation", typeof (Point), typeof (ContextMenuTracker),
                                                new UIPropertyMetadata(new Point()));

        #endregion

        #region Methods

        /// <summary>
        ///  Getting method for TrackOpenLocation attached property
        /// </summary>
        /// <param name="cm">The context menu it is attached to</param>
        /// <returns>The value of the property</returns>
        public static bool GetTrackOpenLocation(ContextMenu cm)
        {
            return (bool)cm.GetValue(TrackOpenLocationProperty);
        }

        /// <summary>
        ///  Setting method for TrackOpenLocation attached property
        /// </summary>
        /// <param name="cm">The contesxt menu it is attached to</param>
        /// <param name="value">The value of the property</param>
        public static void SetTrackOpenLocation(ContextMenu cm, bool value)
        {
            cm.SetValue(TrackOpenLocationProperty, value);
        }
        
        /// <summary>
        ///  Getting method for OpenLocation attached property
        /// </summary>
        /// <param name="cm">The context menu it is attached to</param>
        /// <returns>The value of the property</returns>
        public static Point GetOpenLocation(ContextMenu cm)
        {
            return (Point) cm.GetValue(OpenLocationProperty);
        }

        /// <summary>
        ///  Setting method for OpenLocation attached property
        /// </summary>
        /// <param name="cm">The contesxt menu it is attached to</param>
        /// <param name="value">The value of the property</param>
        public static void SetOpenLocation(ContextMenu cm, Point value)
        {
            cm.SetValue(OpenLocationProperty, value);
        }

        /// <summary>
        ///  The handler that responds to the event when the TrackOpenLocation is set by which the 
        ///  class sets up and enables the tracking.
        /// </summary>
        /// <param name="dependencyObject">The context menu that it is attached to and should switch it on for tracking</param>
        /// <param name="e">The event arguments</param>
        private static void TrackOpenLocationChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var menu = (ContextMenu) dependencyObject;

            if (menu == null)
            {
                return; // Can't do anything
            }

            if (!(e.NewValue is bool))
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                menu.Opened += MenuOpened;
            }
            else
            {
                menu.Opened -= MenuOpened;
            }
        }

        /// <summary>
        ///  Handler that is hooked up to the context menu by TrackOpenLocationChangedCallback() and 
        ///  responds to the event that fires when the context menu is opened by which the point
        ///  of the mouse click is recorded to the OpenLocation property
        /// </summary>
        /// <param name="sender">The context menu that fires this event</param>
        /// <param name="e">The event arguments</param>
        private static void MenuOpened(object sender, RoutedEventArgs e)
        {
            if (!ReferenceEquals(sender, e.OriginalSource))
            {
                return;
            }

            var cm = e.OriginalSource as ContextMenu;
            if (cm != null)
            {
                // Either of the following has the same aim, but the second one is more accurate
                // and less susceptible to fast mouse movement
                //SetOpenLocation(cm, Mouse.GetPosition(cm.PlacementTarget));
                SetOpenLocation(cm, cm.TranslatePoint(new Point(0,0), cm.PlacementTarget));
            }
        }

        #endregion
    }
}
