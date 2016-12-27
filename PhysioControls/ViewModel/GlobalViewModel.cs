using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PhysioControls.ChangeTracking;

namespace PhysioControls.ViewModel
{
    public class GlobalViewModel : ViewModelBase<object>
    {
        #region Properties

        public static GlobalViewModel Instance
        {
            get { return _instance ?? (_instance = new GlobalViewModel()); }
        }

        public ObservableCollection<ChangesetViewModel> Changesets
        {
            get { return _changesets ?? (_changesets = new ObservableCollection<ChangesetViewModel>()); }
        }

        public int SelectedChangesetIndex
        {
            get { return ChangesetManager.Instance.CurrentChangeSetIndex; }
        }

        public static bool IsTrackingEnabled
        {
            get { return ChangesetManager.Instance.IsTrackingEnabled; }
            set { ChangesetManager.Instance.IsTrackingEnabled = value; }
        }

        #endregion

        #region Constructors

        public GlobalViewModel()
        {
            ChangesetManager.Instance.Changesets.CollectionChanged += ChangesetCollectionChanged;
            ChangesetManager.Instance.ChangeSetIndexChanged += OnChangeSetIndexChanged;
            ClearChangesetViewModels();
        }

        #endregion

        #region Methods

        public void RestoreTo(int target)
        {
            if (target >= 0)
            {
                for (; ChangesetManager.Instance.CurrentChangeSetIndex < target; )
                {
                    ChangesetManager.Instance.Redo();
                }
                for (; ChangesetManager.Instance.CurrentChangeSetIndex > target; )
                {
                    ChangesetManager.Instance.Undo();   
                }
            }
        }

        public void RemoveFrom(int index)
        {
            ChangesetManager.Instance.RemoveFrom(index);
        }

        public void RemoveTo(int index)
        {
            ChangesetManager.Instance.RemoveTo(index);
        }

        public void ResetChangesets()
        {
            ChangesetManager.Instance.RemoveAll();
        }

        private void ClearChangesetViewModels()
        {
            Changesets.Clear();
            Changesets.Add(new ChangesetViewModel(new Changeset("Initial")));
        }

        private void ChangesetCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        Changesets.Add(new ChangesetViewModel((Changeset)item));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var index = e.OldStartingIndex + 1;
                    for (var c = 0; c < e.OldItems.Count; c++)
                    {
                        Changesets.RemoveAt(index);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearChangesetViewModels();
                    break;
            }
        }

        private void OnChangeSetIndexChanged()
        {
            OnPropertyChanged("SelectedChangesetIndex");
        }

        #endregion

        #region Fields

        #region Change Tracking related

        private static GlobalViewModel _instance;
        private ObservableCollection<ChangesetViewModel> _changesets;

        #endregion

        #endregion

    }
}
