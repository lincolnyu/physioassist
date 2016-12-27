using PhysioControls.ChangeTracking;

namespace PhysioControls.ViewModel
{
    public class ChangesetViewModel : ViewModelBase<Changeset>
    {
        #region Properties

        public string Description
        {
            get { return Model.Description; }
        }

        public new Changeset Model
        {
            get { return ModelAs<Changeset>(); }
        }

        #endregion

        #region Constructors

        public ChangesetViewModel(Changeset changeset)
            : base(changeset)
        {
        }

        #endregion
    }
}
