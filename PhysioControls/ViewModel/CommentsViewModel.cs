using System;
using System.ComponentModel;
using System.Windows;

namespace PhysioControls.ViewModel
{
    public class CommentsViewModel : ViewModelBase<object>, IDraggableViewModel
    {
        #region Properties

        #region IDraggableViewModel Members

        /// <summary>
        ///  gets and sets the location of the comments on canvas
        /// </summary>
        /// <remarks>
        ///  The setting should not be used for binding with view element, it's only used 
        ///  by internal units such as drag handler; that's why it doesn't have a property
        ///  notification
        /// </remarks>
        public Point LocationOnCanvas
        {
            get { return new Point(Left, Top); }
            set 
            { 
                Left = value.X;
                Top = value.Y;
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        #endregion

        public BaseNodeViewModel Node { get { return _node; } }

        public virtual Visibility Visibility
        {
            get
            {
                if (string.IsNullOrEmpty(Comments))
                {
                    return Visibility.Collapsed;
                }
                return _visibility;
            }
            set
            {
                if (value == _visibility) return;
                _visibility = value;
                OnPropertyChanged("Visibility");
            }
        }

        public string Comments
        {
            get { return Node.Model.Comments; }
            set
            {
                if (value == Node.Model.Comments) return;

                using (StartPropertyChangeRegion(value))
                {
                    Node.Model.Comments = value;
                    OnPropertyChanged("Comments");
                    OnPropertyChanged("Visibility");
                }
            }
        }

        public double Left
        {
            get { return Node.LocationOnCanvas.X + XRelativeToObject; }
            set
            {
                var newValueOfDx = value - Node.LocationOnCanvas.X;
                if (newValueOfDx == XRelativeToObject) return;
                XRelativeToObject = newValueOfDx;
                OnPropertyChanged("Left");
                UpdateLineProperty();
            }
        }

        public double Top
        {
            get { return Node.LocationOnCanvas.Y + YRelativeToObject; }
            set 
            {
                var newValueOfDy = value - Node.LocationOnCanvas.Y;
                if (newValueOfDy == YRelativeToObject) return;
                YRelativeToObject = newValueOfDy;
                OnPropertyChanged("Top");
                UpdateLineProperty();
            }
        }

        // TODO review and remove some redudant property notification
        public double XRelativeToObject
        {
            get { return Node.Model.CommentsLocation.X; }
            set
            {
                if (value == Node.Model.CommentsLocation.X) return;
                using (StartPropertyChangeRegion(value))
                {
                    Node.Model.CommentsLocation.X = value;
                    OnPropertyChanged("Left");
                    UpdateLineProperty();
                }
            }
        }

        // TODO review and remove some redudant property notification
        public double YRelativeToObject
        {
            get { return Node.Model.CommentsLocation.Y; }
            set
            {
                if (value == Node.Model.CommentsLocation.Y) return;
                using (StartPropertyChangeRegion(value))
                {
                    Node.Model.CommentsLocation.Y = value;
                    OnPropertyChanged("Top");
                    UpdateLineProperty();
                }
            }
        }

        public double LineX1
        {
            get { return _lineX1; }
            set
            {
                if (value == _lineX1) return;
                _lineX1 = value;
                OnPropertyChanged("LineX1");
            }
        }

        public double LineY1
        {
            get { return _lineY1; }
            set
            {
                if (value == _lineY1) return;
                _lineY1 = value;
                OnPropertyChanged("LineY1");
            }
        }

        public double LineX2
        {
            get { return _lineX2; }
            set
            {
                if (value == _lineX2) return;
                _lineX2 = value;
                OnPropertyChanged("LineX2");
            }
        }

        public double LineY2
        {
            get { return _lineY2; }
            set
            {
                if (value == _lineY2) return;
                _lineY2 = value;
                OnPropertyChanged("LineY2");
            }
        }

        #endregion 

        #region Constructors

        public CommentsViewModel(BaseNodeViewModel node)
        {
            // TODO load some of the properties from the persistence
            _node = node;
            _node.PropertyChanged += NodeOnPropertyChanged;
            Visibility = Visibility.Visible;
            UpdateLineProperty();
        }

        #endregion

        /// <summary>
        ///  I'm being lazy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NodeOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LocationOnCanvas")
            {
                OnPropertyChanged("Left");
                OnPropertyChanged("Top");
                UpdateLineProperty();
            }
        }

        private void UpdateLineProperty()
        {
            // TODO figure out the appropriate end points
            LineX1 = Left;
            LineY1 = Top;            
            LineX2 = Node.LocationOnCanvas.X;
            LineY2 = Node.LocationOnCanvas.Y;
        }

        #region Fields

        private readonly BaseNodeViewModel _node;
        private bool _isSelected;
        private double _lineX1, _lineY1, _lineX2, _lineY2;
        private Visibility _visibility;

        #endregion
    }
}
