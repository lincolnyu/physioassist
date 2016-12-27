using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using PhysioControls.EntityDataModel;
using PhysioControls.Utilities;

namespace PhysioControls.ViewModel
{
    public class NodeViewModel : BaseNodeViewModel, IDraggableViewModel
    {
        #region Properties

        #region Properties most likely accessed by Views

        #region Members of BaseNodeViewModel

        public override Point LocationOnCanvas
        {
            get { return _locationOnCanvas; }
            set
            {
                if (value == _locationOnCanvas)
                {
                    return;
                }
                _locationOnCanvas = value;
                OnPropertyChanged("LocationOnCanvas");
                OnChangedLocationOrSizeOnCanvas();

                if (SuppressPropagation) return;
                SuppressPropagation = true;
                Location = LocationCanvasToImage(LocationOnCanvas);
                SuppressPropagation = false;
            }
        }

        public override Size SizeOnCanvas
        {
            get
            {
                return _sizeOnCanvas;
            }
            set
            {
                if (value == _sizeOnCanvas)
                {
                    return;
                }
                _sizeOnCanvas = value;
                OnPropertyChanged("SizeOnCanvas");
                OnChangedLocationOrSizeOnCanvas();

                if (!HasSize) return;
                if (SuppressPropagation) return;
                SuppressPropagation = true;
                Size = SizeCanvasToImage(SizeOnCanvas);
                SuppressPropagation = false;
            }
        }

        #endregion

        public double Left 
        {
            get { return LocationOnCanvas.X - SizeOnCanvas.Width/2; }
            set 
            { 
                var currentRight = Right;
                SizeOnCanvas = new Size(currentRight - value, SizeOnCanvas.Height);
                LocationOnCanvas = new Point((value + Right)/2, LocationOnCanvas.Y);
                OnPropertyChanged("Left");
            }
        }

        public double Right
        {
            get { return LocationOnCanvas.X + SizeOnCanvas.Width/2; }
            set
            {
                var currentLeft = Left;
                SizeOnCanvas = new Size(value - currentLeft, SizeOnCanvas.Height);
                LocationOnCanvas = new Point((Left + value)/2, LocationOnCanvas.Y);
                OnPropertyChanged("Right");
            }
        }

        public double Top
        {
            get { return LocationOnCanvas.Y - SizeOnCanvas.Height/2; }
            set
            {
                var currentBottom = Bottom;
                SizeOnCanvas = new Size(SizeOnCanvas.Width, currentBottom - value);
                LocationOnCanvas = new Point(LocationOnCanvas.X, (value + Bottom)/2);
                OnPropertyChanged("Top");
            }
        }

        public double Bottom
        {
            get { return LocationOnCanvas.Y + SizeOnCanvas.Height/2; }
            set 
            {
                var currentTop = Top;
                SizeOnCanvas = new Size(SizeOnCanvas.Width, value - currentTop);
                LocationOnCanvas = new Point(LocationOnCanvas.X, (Top + value)/2);
                OnPropertyChanged("Bottom");
            }
        }

        public double Width
        {
            get { return Right - Left; }
        }

        public double Height
        {
            get { return Bottom - Top;  }
        }

        public Point Location
        {
            get { return Model.Location.EdmVectorToPoint(); }
            set
            {
                if (value.ConsideredSame(Model.Location.EdmVectorToPoint()))
                {
                    return;
                }

                using (StartPropertyChangeRegion(value))
                {
                    System.Diagnostics.Trace.WriteLine("location changed");

                    Model.Location = value.PointToEdmVector();
                    OnPropertyChanged("Location");

                    if (SuppressPropagation)
                    {
                        return;
                    }
                    SuppressPropagation = true;
                    LocationOnCanvas = LocationImageToCanvas(Location);
                    SuppressPropagation = false;
                }
            }
        }

        public Size Size
        {
            get { return Model.Size.EdmVectorToSize(); }
            set
            {
                if (!HasSize)
                {
                    HasSize = true;
                }
                else if (value.ConsideredSame(Model.Size.EdmVectorToSize()))
                {
                    return;
                }

                using (StartPropertyChangeRegion(value))
                {
                    Model.Size = value.SizeToEdmVector();
                    OnPropertyChanged("Size");

                    if (SuppressPropagation) return;
                    SuppressPropagation = true;
                    SizeOnCanvas = SizeImageToCanvas(Size);
                    SuppressPropagation = false;
                }
            }
        }

        public bool HasSize
        {
            get { return Model.HasSize; }
            set
            {
                if (value == Model.HasSize) return;

                using (StartPropertyChangeRegion(value))
                {
                    Model.HasSize = value;
                    OnPropertyChanged("HasSize");
                }   // right point to stop tracking?

                if (HasSize)
                {
                    SuppressPropagation = true; // prevent updating back to Size
                    SizeOnCanvas = SizeImageToCanvas(Size);
                    SuppressPropagation = false;
                    OnChangedLocationOrSizeOnCanvas();
                }
            }
        }

        public SizeModeEnum SizeMode
        {
            get
            {
                if (HasSize) return SizeModeEnum.Absolute;
                return _sizeMode;
            }
            set
            {
                var modeChanged = value != _sizeMode;
                var hasSize = (value == SizeModeEnum.Absolute);
                var hasSizeChanged = hasSize != HasSize;

                if (modeChanged || hasSizeChanged)
                {
                    using (StartPropertyChangeRegion(value))
                    {
                        if (!hasSize)
                        {
                            _sizeMode = value;
                        }
                        HasSize = hasSize;
                        OnPropertyChanged("SizeMode");
                        UpdateNodeSize();
                    }
                }
            }
        }

        #endregion

        #region Properties accessed by ViewModels

        public new Node Model
        {
            get { return ModelAs<Node>(); }
        }

        /// <summary>
        ///  Size relative to canvas in the way specified by SizeMode
        /// </summary>
        public Size SizeValue
        {
            get { return _sizeValue; }
            set
            {
                if (value == _sizeValue)
                    return;
                _sizeValue = value;
                UpdateNodeSize();
            }
        }

        private void UpdateNodeSize()
        {
            switch (SizeMode)
            {
                case SizeModeEnum.Absolute:
                    SuppressPropagation = true; // prevent it from updating back the node size
                    SizeOnCanvas = SizeImageToCanvas(Size);
                    SuppressPropagation = false;
                    break;
                case SizeModeEnum.AbsoluteView:
                    SizeOnCanvas = new Size(SizeValue.Width, SizeValue.Height);
                    break;
                case SizeModeEnum.RelativeToMinimum:
                    var min = Math.Min(Canvas.ActualWidth, Canvas.ActualHeight);
                    SizeOnCanvas = new Size(min * SizeValue.Width, min * SizeValue.Height);
                    break;
                case SizeModeEnum.RelativeToBothXAndY:
                    SizeOnCanvas = new Size(Canvas.ActualWidth*SizeValue.Width,
                                            Canvas.ActualHeight*SizeValue.Height);
                    break;
            }
        }

        public Rect CanvasCoverage
        {
            get { return Page.CanvasCoverage; }
        }

        protected Canvas Canvas
        {
            get { return Page.Canvas; }
        }

        #endregion

        #endregion

        #region Constructors

        public NodeViewModel(Node node, PageViewModel page)
            : base(node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            Page = page;
            
            InitialiseFromData();
        }

        #endregion

        #region Methods

        #region Members of BaseNodeViewModels

        #region Data initialisation

        public new void InitialiseFromData()
        {
            base.InitialiseFromData();
        }

        #endregion

        public override void PreviewUpdateExpand()
        {
            WorkOutSubNodeGeometry();
            UpdateExpand(); // Notifies the children of update
        }

        #endregion

        protected void OnChangedLocationOrSizeOnCanvas()
        {
            OnPropertyChanged("Left");
            OnPropertyChanged("Right");
            OnPropertyChanged("Top");
            OnPropertyChanged("Bottom");

            if (IsExpanded)
            {
                PreviewUpdateExpand();
            }
        }

        protected Point LocationCanvasToImage(Point locationOnCanvas)
        {
            var canvasWidth = Canvas.ActualWidth;
            var canvasHeight = Canvas.ActualHeight;
            var x = locationOnCanvas.X * CanvasCoverage.Width / canvasWidth + CanvasCoverage.Left;
            var y = locationOnCanvas.Y * CanvasCoverage.Height / canvasHeight + CanvasCoverage.Top;
            return new Point(x, y);
        }

        protected Point LocationImageToCanvas(Point locationOnImage)
        {
            var canvasWidth = Canvas.ActualWidth;
            var canvasHeight = Canvas.ActualHeight;
            var x = canvasWidth * (locationOnImage.X - CanvasCoverage.Left) / CanvasCoverage.Width;
            var y = canvasHeight * (locationOnImage.Y - CanvasCoverage.Top) / CanvasCoverage.Height;
            return new Point(x, y);
        }

        protected Size SizeCanvasToImage(Size sizeOnCanvas)
        {
            var canvasWidth = Canvas.ActualWidth;
            var canvasHeight = Canvas.ActualHeight;
            var width = sizeOnCanvas.Width*CanvasCoverage.Width/canvasWidth;
            var height = sizeOnCanvas.Height*CanvasCoverage.Height/canvasHeight;
            return new Size(width, height);
        }

        protected Size SizeImageToCanvas(Size sizeOnImage)
        {
            var canvasWidth = Canvas.ActualWidth;
            var canvasHeight = Canvas.ActualHeight;
            var width = sizeOnImage.Width * canvasWidth / CanvasCoverage.Width;
            var height = sizeOnImage.Height * canvasHeight / CanvasCoverage.Height;
            return new Size(width, height);
        }

        public override void OnCanvasChanged()
        {
            SuppressPropagation = true;
            LocationOnCanvas = LocationImageToCanvas(Location);
            SuppressPropagation = false;
            UpdateNodeSize();
        }

        public override void OnBgImageChanged(PageViewModel.BgImageChangedEventArgs e)
        {
            var oldImage = e.OldImage;
            var newImage = e.NewImage;
            var oldWidth = oldImage.Width;
            var oldHeight = oldImage.Height;
            var newWidth = newImage.Width;
            var newHeight = newImage.Height;
            var oldX = Location.X;
            var oldY = Location.Y;
// ReSharper disable CompareOfFloatsByEqualityOperator
            var newX = oldWidth == newWidth ? oldX : oldX*newWidth/oldWidth;
            var newY = oldHeight == newHeight ? oldY : oldY*newHeight/oldHeight;
// ReSharper restore CompareOfFloatsByEqualityOperator

            Location = new Point(newX, newY); 
        }

        private void WorkOutSubNodeGeometry()
        {
            if (Page.Canvas == null) return;
            var canvasWidth = Page.Canvas.ActualWidth;
            var canvasHeight = Page.Canvas.ActualHeight;
            var depth = SubNodesOrganiser.GetDepth(this);
            const double att = 0.5;
            const double lastRadiusToDim = 2;
            SubNodesOrganiser sno;

            switch (SizeMode)
            {
                case SizeModeEnum.RelativeToMinimum:    // circular
                    var min = Math.Min(canvasWidth, canvasHeight);
                    var init = SubNodesOrganiser.GetApproximateInitialRadius(min/2, att, depth);
                    var last = SubNodesOrganiser.GetSmallestRadius(init, att, depth);
                    if (last > lastRadiusToDim * Width) init = init*lastRadiusToDim*Width/last;
                    sno = new SubNodesOrganiser(this, init, att, true);
                    break;
                case SizeModeEnum.RelativeToBothXAndY:
                case SizeModeEnum.AbsoluteView:
                case SizeModeEnum.Absolute: // elliptical
                    var minR = Math.Min(canvasWidth/Width, canvasHeight/Height);
                    var initX = SubNodesOrganiser.GetApproximateInitialRadius(Width*minR, att, depth);
                    var initY = SubNodesOrganiser.GetApproximateInitialRadius(Height*minR, att, depth);
                    var lastX = SubNodesOrganiser.GetSmallestRadius(initX, att, depth);
                    if (lastX > lastRadiusToDim * Width)
                    {
                        initX = initX*lastRadiusToDim*Width/lastX;
                        initY = initY*lastRadiusToDim*Width/lastX;
                    }
                    sno = new SubNodesOrganiser(this, initX, initY, att, true);
                    break;
                default:
                    throw new ArgumentException("Unexpected size mode");
            }
            sno.Organise();
        }

        public virtual void DragBegin()
        {
            
        }


        #endregion

        #region Enumerations

        public enum SizeModeEnum
        {
            Absolute, 
            AbsoluteView,
            RelativeToMinimum,
            RelativeToBothXAndY,
        }

        #endregion

        #region Delegates

        public delegate Shape SpawnShapeDelegate();

        #endregion

        #region Fields

        private Point _locationOnCanvas;
        private Size _sizeOnCanvas;
 
        private SizeModeEnum _sizeMode;
        private Size _sizeValue;

        #endregion
    }
}
