using PhysioControls.ChangeTracking;

namespace PhysioControls.Managers
{
    public class DirtChecker
    {
        #region Events

        public delegate void ProjectDirtyEvent();

        public event ProjectDirtyEvent ProjectDirty;

        #endregion

        #region Properties

        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                if (value == _isDirty) return;
                _isDirty = value;
                if (!value)
                {
                    _savedIndex = ChangesetManager.Instance.CurrentChangeSetIndex;
                }
                if (ProjectDirty != null)
                {
                    ProjectDirty();
                }
            }
        }

        #endregion

        #region Constructors

        public DirtChecker()
        {
            ChangesetManager.Instance.ChangeSetIndexChanged +=
                () =>
                    {
                        IsDirty = _savedIndex !=
                                  ChangesetManager.Instance.
                                      CurrentChangeSetIndex;
                    };

            ChangesetManager.Instance.ChangeSetRemovedRange +=
                (index, count) =>
                    {
                        var end = index + count;
                        if (end <= _savedIndex) _savedIndex -= count;
                        else if (index < _savedIndex && end > _savedIndex) _savedIndex = -1;
                        IsDirty = _savedIndex != ChangesetManager.Instance.CurrentChangeSetIndex;
                    };
        }

        #endregion

        #region Fields

        /// <summary>
        ///  Backing field for property IsDirty
        /// </summary>
        private bool _isDirty;

        /// <summary>
        ///  The index at which the project is saved
        /// </summary>
        /// <remarks>
        ///  It is naturally initialised to zero which reflects the saved index on open or initial load
        /// </remarks>
        private int _savedIndex;

        #endregion
    }
}
