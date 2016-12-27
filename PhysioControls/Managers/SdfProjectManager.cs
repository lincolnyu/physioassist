using System.IO;
using PhysioControls.EntityDataModel;
using PhysioControls.Utilities;
using PhysioControls.ViewModel;

namespace PhysioControls.Managers
{
    public class SdfProjectManager : FileProjectManager, IFileProjectManager
    {
        #region Events

        #region IFileProjectManager Members

        public event ManagerRequest InitOnNew;

        public event ManagerRequest InitOnLoad;

        public event ManagerRequest ClearOnClose;

        #endregion

        #region FileProjectManager members

        protected override DirtChecker DirtChecker
        {
            get { return _projectPersister; }
            set { }
        }

        #endregion

        #endregion

        #region Methods

        #region FileProjectManager members

        protected override void CreateFile()
        {
            ProjectViewModel = new ProjectViewModel(new Project());
            FilePath = null;

            _projectPersister = new ProjectPersister(ProjectViewModel);

            if (InitOnNew != null)
            {
                GlobalViewModel.IsTrackingEnabled = false;
                InitOnNew();
                GlobalViewModel.IsTrackingEnabled = true;
            }

            // resets the changesets (commands) so that it clears up and shows an initial changeset
            GlobalViewModel.Instance.ResetChangesets();
        }

        protected override void CloseFile()
        {
            _projectPersister.Dispose();
            _projectPersister = null;

            if (ClearOnClose != null)
            {
                ClearOnClose();
            }

            GlobalViewModel.Instance.ResetChangesets();
        }

        protected override void LoadFile()
        {
            var edmConnection = EdmHelper.CreateSqlCeConnection(FilePath);
            _projectPersister = new ProjectPersister(edmConnection);

            ProjectViewModel = _projectPersister.ProjectViewModel;

            if (InitOnLoad != null)
            {
                GlobalViewModel.IsTrackingEnabled = false;
                InitOnLoad();
                GlobalViewModel.IsTrackingEnabled = true;
            }

            // resets the changesets (commands) so that it clears up and shows an initial changeset
            GlobalViewModel.Instance.ResetChangesets();
        }

        protected override void SaveFile()
        {
            _projectPersister.Save();
        }

        protected override void SaveFileAs()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            var connection = EdmHelper.CreateSqlCeConnection(FilePath);
            _projectPersister.SaveAs(connection);
        }

        #endregion

        #endregion

        #region Fields

        private ProjectPersister _projectPersister;

        #endregion
    }
}
