using System.Collections.Specialized;
using System.Windows;
using PhysioControls.Collections;
using PhysioControls.EntityDataModel;

namespace PhysioControls.ViewModel
{
    public abstract class BaseNodeViewModel : DataObjectViewModel
    {
        #region Delegates

        public delegate void IsSelectedChangedEvent(BaseNodeViewModel sender);

        #endregion

        #region Constants

        public const double DefaultSizeAttenuation = 0.8;

        #endregion 

        #region Properties

        public new BaseNode Model
        {
            get { return ModelAs<BaseNode>(); }
        }

        public abstract Point LocationOnCanvas { get; set; }

        public abstract Size SizeOnCanvas { get; set; }

        public virtual bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value == IsExpanded) return;
                _isExpanded = value;
                OnPropertyChanged("IsExpanded");
                SubNodeVisibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
                PreviewUpdateExpand();
            }
        }

        public CommentsViewModel Comments { get; private set; }

        public virtual bool HasSubNodes
        {
            get { return SubNodes.Count > 0; }
        }

        public virtual bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == IsSelected)
                    return;
                _isSelected = value;
                OnPropertyChanged("IsSelected");
                OnPropertyChanged("IsHit");
                OnPropertyChanged("IsSelectedOneWay");
                OnIsSelectedChanged();
                foreach (var subNode in SubNodes)
                {
                    subNode.IsAncestorSelected = value;
                }
            }
        }

        /// <summary>
        ///  For binding to a single-select control so that the control can pick up one of the view models
        ///  while multiple-selection of view models from the side other than the control can potentially 
        ///  be reflected in the control as well
        /// </summary>
        public virtual bool IsSelectedOneWay
        {
            get { return IsSelected; }
            set { IsHit = value; }
        }

        /// <summary>
        ///  For binding to a single-select control so that the control can pick up one of the view models
        ///  while single-selection of view models from the side other than the control can be reflected
        ///  in the control
        /// </summary>
        public virtual bool IsHit
        {
            get { return IsHitInternal && IsSelected; }
            set
            {
                if (value)
                {
                    _isHitInternal = true;
                    Page.Project.UpdateHit(this);
                }
                OnPropertyChanged("IsHit");
                OnPropertyChanged("IsSelectedOneWay");
            }
        }

        /// <summary>
        ///  For updating hit status internally such as from the pick handler
        /// </summary>
        internal bool IsHitInternal
        {
            get { return _isHitInternal;  }
            set 
            {
                _isHitInternal = value;
                Page.Project.UpdateHitInternal(this);
                OnPropertyChanged("IsHit");
                OnPropertyChanged("IsSelectedOneWay");
            }
        }

        public virtual bool IsSubNodeSelected
        {
            get { return _isSubNodeSelected; }
            set
            {
                if (value == _isSubNodeSelected) return;
                _isSubNodeSelected = value;
                OnPropertyChanged("IsSubNodeSelected");
            }
        }

        public virtual bool IsAncestorSelected
        {
            get { return _isAncestorSelected; }
            set
            {
                if (value == _isAncestorSelected) return;
                _isAncestorSelected = value;
                OnPropertyChanged("IsAncestorSelected");
                foreach (var subNode in SubNodes)
                {
                    subNode.IsAncestorSelected = value;
                }
            }
        }

        public virtual Visibility SubNodeVisibility
        {
            get { return _subNodeVisibility; }
            set
            {
                if (value == _subNodeVisibility) return;
                _subNodeVisibility = value;
                OnPropertyChanged("SubNodeVisibility");
            }
        }

        public EnhancedObservableCollection<SubNodeViewModel> SubNodes
        {
            get
            {
                if (_subNodes == null)
                {
                    _subNodes = new EnhancedObservableCollection<SubNodeViewModel>();
                    _subNodes.CollectionChanged += OnSubNodesCollectionChanged;
                    _subNodes.CollectionClearing += OnSubNodesCollectionClearing;
                }
                return _subNodes;
            }
        }
       
        #endregion

        #region Fields

        public event IsSelectedChangedEvent IsSelectedChanged;

        #endregion

        #region Constructors

        protected BaseNodeViewModel(BaseNode model) 
            : base(model)
        {
            Comments = new CommentsViewModel(this); 
            RegisterCollection(SubNodes);
        }

        #endregion

        #region Methods

        public abstract void PreviewUpdateExpand();

        public void InitialiseFromData()
        {
            // The previous implementation with which subnodes are removed and added back
            // in is deleted for tidiness

            SubNodes.CollectionChanged -= OnSubNodesCollectionChanged;
            foreach (var subNode in Model.SubNodes)
            {
                var subNodeViewModel = new SubNodeViewModel(subNode, this);
                SubNodes.Add(subNodeViewModel);
                subNodeViewModel.SizeRatioToParent = DefaultSizeAttenuation;
                subNodeViewModel.IsAncestorSelected = IsSelected || IsAncestorSelected;
                PropertyChanged += subNodeViewModel.OnBasePropertyChanged;
            }
            SubNodes.CollectionChanged += OnSubNodesCollectionChanged;
        }

        public virtual void UpdateExpand()
        {
            foreach (var subNode in SubNodes)
            {
                subNode.UpdateExpand();
            }
        }

        public SubNodeViewModel AddSubNode(double sizeAttenuation)
        {
            // TODO create sub node better, at least give it a valid ID
            var subNode = new SubNode();
            return AddSubNode(subNode, sizeAttenuation);
        }

        public SubNodeViewModel AddSubNode(SubNode subNode, double sizeAttenuation)
        {
            var subNodeViewModel = new SubNodeViewModel(subNode, this);
            SubNodes.Add(subNodeViewModel);
            subNodeViewModel.SizeRatioToParent = sizeAttenuation;
            subNodeViewModel.IsAncestorSelected = IsSelected || IsAncestorSelected;
            IsExpanded = true;

            PreviewUpdateExpand();
            return subNodeViewModel;
        }

        public void RemoveSubNode(SubNodeViewModel subNodeViewModel)
        {
            SubNodes.Remove(subNodeViewModel);
            if (SubNodes.Count == 0) IsExpanded = false;
            PreviewUpdateExpand();
        }

        protected internal virtual void OnSubNodesCollectionClearing(EnhancedObservableCollection<SubNodeViewModel> sender)
        {
            foreach (var subNode in sender)
            {
                RemoveSubNodeModel(subNode);
                PropertyChanged -= subNode.OnBasePropertyChanged;
            }
            OnPropertyChanged("HasSubNodes");
        }

        protected internal virtual void OnSubNodesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (SubNodeViewModel subNode in e.NewItems)
                    {
                        AddSubNodeModel(subNode);
                        PropertyChanged += subNode.OnBasePropertyChanged;
                    }
                    if (SubNodes.Count == e.NewItems.Count) OnPropertyChanged("HasSubNodes");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (SubNodeViewModel subNode in e.OldItems)
                    {
                        RemoveSubNodeModel(subNode);
                        PropertyChanged -= subNode.OnBasePropertyChanged;
                    }
                    if (SubNodes.Count == 0) OnPropertyChanged("HasSubNodes");
                    break;
            }
        }

        void AddSubNodeModel(SubNodeViewModel subNodeViewModel)
        {
            var subNodeModel = (SubNode)subNodeViewModel.Model;
            subNodeModel.Parent = Model;
            Model.SubNodes.Add(subNodeModel);
            Page.Project.Model.Persister.AddDataObject(subNodeModel);
        }

        void RemoveSubNodeModel(SubNodeViewModel subNodeViewModel)
        {
            var subNodeModel = (SubNode)subNodeViewModel.Model;
            subNodeModel.Parent = null;
            Model.SubNodes.Remove(subNodeModel);
            Page.Project.Model.Persister.RemoveDataObject(subNodeModel);
        }

        void OnIsSelectedChanged()
        {
            if (IsSelectedChanged != null)
            {
                IsSelectedChanged(this);
            }
        }

        #endregion

        #region Fields

        private bool _isExpanded;
        private bool _isSelected;
        private Visibility _subNodeVisibility = Visibility.Collapsed;   // consistent with the default value of IsExpanded
        private EnhancedObservableCollection<SubNodeViewModel> _subNodes;
        private bool _isSubNodeSelected;
        private bool _isAncestorSelected;
        private bool _isHitInternal;

        #endregion
    }
}
