using System.Threading;
using PhysioControls.EntityDataModel;

namespace PhysioControls.ViewModel
{
    public abstract class DataObjectViewModel : ViewModelBase<DataObject>
    {
        #region Properties

        #region Properties accessed by ViewModels

        public override string DisplayName
        {
            get { return Model.Name; }
            set
            {
                if (value == DisplayName) return;

                using(StartPropertyChangeRegion(value))
                {
                    Model.Name = value;
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        public virtual PageViewModel Page
        {
            get { return _page; } 
            set
            {
                if (value == _page) return;

                using (StartPropertyChangeRegion(value))
                {
                    _page = value;
                }
            }
        }

        #endregion

        protected bool SuppressPropagation
        {
            get { return _suppressPropagation; }
            set
            {
                if (value == SuppressPropagation) return;
                if (value)
                {
                    Monitor.Enter(this);
                }
                else
                {
                    Monitor.Exit(this);
                }
                _suppressPropagation = value;
            }
        }

        #endregion

        #region Constructors

        protected DataObjectViewModel()
        {
        }

        protected DataObjectViewModel(DataObject model)
            : base(model)
        {
        }

        #endregion

        #region Methods

        public abstract void OnCanvasChanged();
        public abstract void OnBgImageChanged(PageViewModel.BgImageChangedEventArgs e);

        #endregion

        #region Fields

        private bool _suppressPropagation;
        private PageViewModel _page;

        #endregion
    }
}
