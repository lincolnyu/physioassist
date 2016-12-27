using System;
using System.Collections.Specialized;
using PhysioControls.ChangeTracking;
using PhysioControls.Collections;
using PhysioControls.EntityDataModel;
using Page = PhysioControls.EntityDataModel.Page;

namespace PhysioControls.ViewModel
{
    public class ProjectViewModel : ViewModelBase<Project>
    {
        #region Evennts

        public delegate void CurrentPageChangedEvent(ProjectViewModel sender);

        public event CurrentPageChangedEvent CurrentPageChanged;

        #endregion

        #region Properties

        public new Project Model
        {
            get { return ModelAs<Project>(); }
        }

        public EnhancedObservableCollection<PageViewModel> Pages
        {
            get
            {
                if (_pages == null)
                {
                    _pages = new EnhancedObservableCollection<PageViewModel>();
                    _pages.CollectionChanged += OnPagesCollectionChanged;
                    _pages.CollectionClearing += OnPagesCollectionClearing;
                }

                return _pages;
            }
        }

        public PageViewModel CurrentPage
        {
            get { return CurrentPageIndex < Pages.Count ? Pages[CurrentPageIndex] : null; }
        }

        public int CurrentPageIndex
        {
            get { return _currentPageIndex; }
            set
            {
                if (value == _currentPageIndex) return;
                using (StartPropertyChangeRegion(value))
                {
                    var oldPage = CurrentPage;
                    _currentPageIndex = value;
                    var newPage = CurrentPage;
                    if (oldPage != null) oldPage.OnCurrentPageChanged();
                    if (newPage != null) newPage.OnCurrentPageChanged();
                    OnCurrentPageChanged();
                }
            }
        }

        #endregion

        #region Constructors

        public ProjectViewModel(Project project)
            : base(project)
        {
            InitialiseFromData();
            RegisterCollection(Pages);
        }

        #endregion

        #region Methods

        private void InitialiseFromData()
        {
            Pages.CollectionChanged -= OnPagesCollectionChanged;
            foreach (var page in Model.Pages)
            {
                var pageViewModel = new PageViewModel(page, this);
                Pages.Add(pageViewModel);
            }
            Pages.CollectionChanged += OnPagesCollectionChanged;
        }

        private void OnCurrentPageChanged()
        {
            if (CurrentPageChanged != null)
            {
                CurrentPageChanged(this);
            }
        }

        public PageViewModel AddPage(Page page)
        {
            using (ChangesetManager.Instance.StartChangeset("Adding page"))
            {
                var pageViewModel = new PageViewModel(page, this);
                Pages.Add(pageViewModel);
                CurrentPageIndex = Pages.Count - 1;
                ChangesetManager.Instance.Commit();
                return pageViewModel;
            }
        }

        public bool RemoveCurrentPage()
        {
            if (Pages.Count == 1)
                return false;
            RemovePageAt(CurrentPageIndex);
            return true;
        }

        public void RemovePageAt(int index)
        {
            if (index > Pages.Count)
            {
                throw new IndexOutOfRangeException("Index of the page to remove is out of range");
            }
            if (Pages.Count == 1)
            {
                throw new InvalidOperationException("The last page of the project is not allowed to be removed");
            }
            using (ChangesetManager.Instance.StartChangeset("Remove page"))
            {
                var needNotifyCurrentPageChange = false;
                if (index < CurrentPageIndex || index == CurrentPageIndex && CurrentPageIndex >= Pages.Count - 1)
                {
                    CurrentPageIndex--;
                }
                else if (index == CurrentPageIndex)
                {
                    needNotifyCurrentPageChange = true;
                }
                Pages.Remove(Pages[index]);
                if (needNotifyCurrentPageChange) OnCurrentPageChanged();
                ChangesetManager.Instance.Commit();
            }
        }

        private void OnPagesCollectionClearing(EnhancedObservableCollection<PageViewModel> sender)
        {
            foreach (PageViewModel page in sender)
            {
                RemovePageModel(page);
            }
        }

        private void OnPagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PageViewModel page in e.NewItems)
                    {
                        AddPageModel(page);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (PageViewModel page in e.OldItems)
                    {
                        RemovePageModel(page);
                    }
                    break;
            }
            UpdatePageNumber();
        }

        private void AddPageModel(PageViewModel pageViewModel)
        {
            var page = pageViewModel.Model;
            page.Project = Model;
            Model.Pages.Add(page);
            Model.Persister.AddDataObject(page);
        }

        private void RemovePageModel(PageViewModel pageViewModel)
        {
            var page = pageViewModel.Model;
            page.Project = null;
            Model.Pages.Remove(page);
            Model.Persister.RemoveDataObject(page);
        }

        internal void DeselectAll()
        {
            foreach (var page in Pages)
            {
                page.DeselectAll();
            }
        }

        /// <summary>
        ///  Used by update from single-select control on node hit
        /// </summary>
        /// <param name="node">Node that is hit from the control</param>
        internal void UpdateHit(BaseNodeViewModel node)
        {
            System.Diagnostics.Trace.Assert(node != null);
            DeselectAll();
            node.IsSelected = true;
            // add the selected node to the selected view model collection the select handler is working on with the page view model
            node.Page.SelectedViewModels.Add(node); 
            if (_hitNode != node && _hitNode != null)
            {
                _hitNode.IsHitInternal = false;
            }
        }

        /// <summary>
        ///  Used by update from internal control on node hit 
        /// </summary>
        /// <param name="node">Node that is hit or dehit from the internal control</param>
        internal void UpdateHitInternal(BaseNodeViewModel node)
        {
            if (node == null || node.IsHitInternal)
            {
                if (_hitNode != node && _hitNode != null)
                {
                    _hitNode.IsHitInternal = false;
                }
                _hitNode = node;
            }
            else // !node.IsHitInternal
            {
                _hitNode = null;
            }
        }

        public void SetCurrentTo(PageViewModel page)
        {
            for (var i = 0; i < Pages.Count; i++)
            {
                if (Pages[i] != page) continue;
                CurrentPageIndex = i;
                return;
            }
        }

        void UpdatePageNumber()
        {
            var pageNo = 1;
            foreach (var page in Pages)
            {
                page.PageNo = pageNo++;
            }
        }


        #endregion

        #region Fields

        private EnhancedObservableCollection<PageViewModel> _pages;
        private int _currentPageIndex;
        private BaseNodeViewModel _hitNode;

        #endregion
    }
}
