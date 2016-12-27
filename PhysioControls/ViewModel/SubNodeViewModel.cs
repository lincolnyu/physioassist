using System;
using System.ComponentModel;
using System.Windows;
using PhysioControls.Utilities;
using PhysioControls.EntityDataModel;

namespace PhysioControls.ViewModel
{
    public class SubNodeViewModel : BaseNodeViewModel, ISelectableViewModel
    {
        #region Properties

        #region Members of BaseNodeViewModel

        public NodeViewModel AncestorNode
        {
            get
            {
                dynamic p;
                for (p = Parent; p != null && !(p is NodeViewModel); p = p.Parent)
                {
                }
                return p;
            }
        }

        /// <summary>
        ///  Sub-node doesn't store a reference to the page that owns its family, so it obtains the page from its ancestor
        ///  it's only used for the pure purpose of getting the owning page
        /// </summary>
        public override PageViewModel Page
        {
            get { return AncestorNode.Page; }
        }

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
            }
        }

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }
            set
            {
                base.IsSelected = value;
                Parent.IsSubNodeSelected = value;
            }
        }

        public override bool IsSubNodeSelected
        {
            get
            {
                return base.IsSubNodeSelected;
            }
            set
            {
                base.IsSubNodeSelected = value;
                Parent.IsSubNodeSelected = value;
            }
        }

        #endregion

        /// <summary>
        ///  Immediate superior
        /// </summary>
        public BaseNodeViewModel Parent
        {
            get { return _parent; }

            set
            {
                if (value == _parent) return;
                
                using (StartPropertyChangeRegion(value))
                {
                    _parent = value;
                    OnPropertyChanged("Parent");
                }
            }
        }

        /// <summary>
        ///  Ancestor
        /// </summary>
        public NodeViewModel OwnerNode
        {
            get {
                var node = Parent;
                for (; !(node is NodeViewModel); node = ((SubNodeViewModel)node).Parent)
                {
                    
                }
                return (NodeViewModel)node;
            }
        }
        
        public double Left
        {
            get { return LocationOnCanvas.X - SizeOnCanvas.Width / 2; }
        }

        public double Right
        {
            get { return LocationOnCanvas.X + SizeOnCanvas.Width / 2; }
        }

        public double Top
        {
            get { return LocationOnCanvas.Y - SizeOnCanvas.Height / 2; }
        }

        public double Bottom
        {
            get { return LocationOnCanvas.Y + SizeOnCanvas.Height / 2; }
        }

        public double SizeRatioToParent
        {
            get { return _sizeRatioToParent; }
            set
            {
                if (value.ConsideredSame(_sizeRatioToParent))
                {
                    return;
                }
                _sizeRatioToParent = value;
                OnPropertyChanged("SizeRatioToParent");

                PreviewUpdateExpand();
            }
        }

        #endregion

        #region Constructors

        public SubNodeViewModel(SubNode subNode, BaseNodeViewModel parent) 
            : base(subNode)
        {
            if (subNode == null)
            {
                throw new ArgumentNullException("subNode");
            }
            Parent = parent;
            InitialiseFromData();
        }

        #endregion

        #region Methods

        #region Members of DataObjectViewModel

        public override void OnCanvasChanged()
        {
            // TODO don't need to do anything?
        }

        public override void OnBgImageChanged(PageViewModel.BgImageChangedEventArgs e)
        {
            // TODO don't need to do anything?
        }

        #endregion

        #region Members of BaseNodeViewModels

        #region Data initialisation

        public new void InitialiseFromData()
        {
            base.InitialiseFromData();
        }

        #endregion

        public override void PreviewUpdateExpand()
        {
            Parent.PreviewUpdateExpand();
        }

        public override void UpdateExpand()
        {
            UpdateSizeOnCanvas();
            base.UpdateExpand();
        }

        #endregion

        public void OnBasePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        protected void OnChangedLocationOrSizeOnCanvas()
        {
            OnPropertyChanged("Left");
            OnPropertyChanged("Right");
            OnPropertyChanged("Top");
            OnPropertyChanged("Bottom");
        }

        protected void UpdateSizeOnCanvas()
        {
            var width = Parent.SizeOnCanvas.Width*SizeRatioToParent;
            var height = Parent.SizeOnCanvas.Height*SizeRatioToParent;
            SizeOnCanvas = new Size(width, height);
        }

        #endregion

        #region Fields

        private BaseNodeViewModel _parent;
        private Point _locationOnCanvas;
        private Size _sizeOnCanvas;
        private double _sizeRatioToParent;

        #endregion
    }
}
