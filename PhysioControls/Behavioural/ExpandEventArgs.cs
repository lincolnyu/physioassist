using System.Windows;

namespace PhysioControls.Behavioural
{
    public class ExpandEventArgs
    {
        #region Properties

        public object HitViewModel { get; set; }
        public UIElement HitElement { get; set; }

        #endregion
    }
}
