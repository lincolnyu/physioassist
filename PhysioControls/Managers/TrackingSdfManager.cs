using System.IO;
using System.Windows;
using PhysioControls.EntityDataModel;
using PhysioControls.Utilities;
using PhysioControls.ViewModel;

namespace PhysioControls.Managers
{
    public class TrackingSdfManager : FileProjectManager, IFileProjectManager
    {
        #region Events

        public event ManagerRequest InitOnNew;

        public event ManagerRequest InitOnLoad;

        public event ManagerRequest ClearOnClose;

        #endregion

        #region Properties

        protected override DirtChecker DirtChecker
        {
            get { return _dirtChecker ?? (_dirtChecker = new DirtChecker()); }
            set { _dirtChecker = value; }
        }

        #endregion

        #region Methods

        #region Constructors

        public TrackingSdfManager(string trackingSdlPath)
        {
            _trackingSdlPath = trackingSdlPath;
        }

        #endregion

        #region Application specific methods

        protected override void CreateFile()
        {
            ProjectViewModel = new ProjectViewModel(new Project());
            FilePath = null;

            // have to delete the existing tracking SDL file
            if (File.Exists(_trackingSdlPath))
            {
                File.Delete(_trackingSdlPath);
            }

            var edmConnection = EdmHelper.CreateSqlCeConnection(_trackingSdlPath);
            _trackingSdlPersister = new ProjectPersister(edmConnection, ProjectViewModel);
            _trackingSdlPersister.ProjectDirty += TrackingSdlOnProjectDirty;

            if (InitOnNew != null)
            {
                GlobalViewModel.IsTrackingEnabled = false;
                InitOnNew();
                GlobalViewModel.IsTrackingEnabled = true;
            }

            // resets the changesets (commands) so that it clears up and shows an initial changeset
            GlobalViewModel.Instance.ResetChangesets();
        }

        protected override void LoadFile()
        {
            System.Diagnostics.Trace.Assert(File.Exists(FilePath));

            File.Copy(FilePath, _trackingSdlPath, true);

            var edmConnection = EdmHelper.CreateSqlCeConnection(_trackingSdlPath);
            _trackingSdlPersister = new ProjectPersister(edmConnection);
            _trackingSdlPersister.ProjectDirty += TrackingSdlOnProjectDirty;

            ProjectViewModel = _trackingSdlPersister.ProjectViewModel;

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
            if (!IsDirty)
            {
                return;
            }

            File.Copy(_trackingSdlPath, FilePath, true);
            DirtChecker.IsDirty = false;
        }

        protected override void SaveFileAs()
        {
            File.Copy(_trackingSdlPath, FilePath, true);
            DirtChecker.IsDirty = false;
        }

        protected override void CloseFile()
        {
            _trackingSdlPersister.Dispose();
            _trackingSdlPersister = null;
            DirtChecker.IsDirty = false;

            if (ClearOnClose != null)
            {
                ClearOnClose();
            }

            GlobalViewModel.Instance.ResetChangesets();
        }

        #endregion

        protected override bool ValidatePathToLoad(string path)
        {
            if (path == _trackingSdlPath)
            {
                MessageBox.Show("Cannot open the internal tracking database", "PhysioAssist");
                return false;
            }

            return true;
        }

        protected override bool ValidatePathToSave(string path)
        {
            if (path == _trackingSdlPath)
            {
                MessageBox.Show("Cannot write to the internal tracking database", "PhysioAssist");
                return false;
            }

            return true;
        }

        private void TrackingSdlOnProjectDirty()
        {
            System.Diagnostics.Trace.Assert(_trackingSdlPersister != null);

            // may need to save at a specific lower rate
            if (_trackingSdlPersister.IsDirty)
            {
                _trackingSdlPersister.Save();
            }
        }

        #endregion

        #region Properties

        private readonly string _trackingSdlPath;
        private ProjectPersister _trackingSdlPersister;

        private DirtChecker _dirtChecker;

        #endregion
    }
}
