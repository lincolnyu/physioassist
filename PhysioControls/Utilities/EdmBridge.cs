using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EdmVector = PhysioControls.EntityDataModel.Vector;

namespace PhysioControls.Utilities
{
    public static class EdmBridge
    {
        #region Methods

        public static EdmVector PointToEdmVector(this Point point)
        {
            return new EdmVector { X = point.X, Y = point.Y };
        }

        public static Point EdmVectorToPoint(this EdmVector vector)
        {
            return new Point(vector.X, vector.Y);
        }
        
        public static EdmVector SizeToEdmVector(this Size size)
        {
            return new EdmVector {X = size.Width, Y = size.Height};
        }

        public static Size EdmVectorToSize(this EdmVector vector)
        {
            return new Size(vector.X, vector.Y);
        }

        public static ImageSource UriToImageSource(this string uriString)
        {
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.UriSource = new Uri(uriString);
            imageSource.EndInit();
            return imageSource;
        }

        #endregion
    }
}
