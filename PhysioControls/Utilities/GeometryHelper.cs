using System;
using System.Windows;

namespace PhysioControls.Utilities
{
    public static class GeometryHelper
    {
        #region Constants

        public const double Epsilon = 0.0001;

        #endregion

        #region Methods

        public static double DistanceTo(this Point point1, Point point2)
        {
            var dx = point1.X - point2.X;
            var dy = point1.Y - point2.Y;
            return Math.Sqrt(dx*dx + dy*dy);
        }

        public static bool ConsideredSame(this Point point1, Point point2, double epsilon = Epsilon)
        {
            return point1.DistanceTo(point2) < epsilon;
        }

        public static bool ConsideredSame(this Size size1, Size size2, double epsilon = Epsilon)
        {
            var dx = size1.Width - size2.Width;
            var dy = size1.Height - size2.Height;
            return Math.Sqrt(dx * dx + dy * dy) < epsilon;
        }

        public static bool ConsideredSame(this double value1, double value2, double epsilon = Epsilon)
        {
            return Math.Abs(value1 - value2) < epsilon;
        }

        #endregion
    }
}
