using System.Windows;
using System.Windows.Input;

namespace PhysioControls.Behavioural
{
    public interface IZoomAndPanHandler
    {
        #region Methods

        void ExposeInfo(ZoomAndPanInfo info);

        void WheelChanged(Point point, double delta);

        bool IsPanEnabled(MouseButtonEventArgs e);
        void PanBegin(Point point);
        void PanMove(Point point);
        void PanEnd(Point point);

        #endregion
    }
}
