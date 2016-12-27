using System.Windows;
using PhysioControls.ViewModel;

namespace PhysioControls.Managers
{
    public abstract class FileProjectManager
    {
        #region Properties

        public string FilePath
        {
            get { return _filePath; }
            protected set
            {
                if (value == _filePath) return;
                if (_eventArgs == null) _eventArgs = new ManagerEventArgs();
                _eventArgs.AddFileChange(_filePath, value);
                _filePath = value;
            }
        }

        public ProjectViewModel ProjectViewModel { get; protected set; }

        public ManagerStateEnum State
        {
            get { return _state; }
            protected set
            {
                if (value == _state) return;
                if (_eventArgs == null) _eventArgs = new ManagerEventArgs();
                _eventArgs.AddStateChange(_state, value);
                _state = value;
            }
        }

        public bool IsDirty
        {
            get { return State == ManagerStateEnum.NewDirty || State == ManagerStateEnum.SyncedDirty; }
        }

        protected abstract DirtChecker DirtChecker { get; set; }

        #endregion

        #region Events

        public event QuitPromptEvent QuitPrompt;

        public event FileNamePromptEvent LoadPrompt;

        public event FileNamePromptEvent SavePrompt;

        public event ManagerEvent StateChanged;

        #endregion

        #region Methods

        #region IFileProjectManager members

        public virtual void New()
        {
            if (!Quit(QuitPromptEventType.PromptToSaveOnNew))
            {
                return;
            }

            // application specific implementation
            CreateFile();

            DirtChecker.ProjectDirty += OnProjectDirty;

            State = ManagerStateEnum.New;

            FireEvent();
        }

        public virtual void Open()
        {
            if (!Quit(QuitPromptEventType.PromptToSaveOnLoad))
            {   // open failed/canceled state remains unchanged.
                return;
            }

            if (LoadPrompt == null)
            {   // cannot load file since there is no load method
                return;
            }

            var filePath = LoadPrompt();
            if (string.IsNullOrEmpty(filePath) || filePath == FilePath)
            {
                return;
            }
            if (!ValidatePathToLoad(filePath))
            {
                return;
            }

            FilePath = filePath;

            // application specific implementation
            LoadFile();

            DirtChecker.ProjectDirty += OnProjectDirty;

            State = ManagerStateEnum.Synced;

            FireEvent();
        }

        /// <summary>
        ///  called when a save project application command is conducted
        /// </summary>
        /// <returns>true if the saving is successful</returns>
        public virtual bool Save()
        {
            if (State == ManagerStateEnum.Closed)
            {   // nothing to save
                return false;
            }

            if (State == ManagerStateEnum.New || State == ManagerStateEnum.NewDirty)
            {
                return SaveAs();
            }

            // application specific implementation
            SaveFile();

            State = ManagerStateEnum.Synced;
            FireEvent();

            return true;
        }

        /// <summary>
        ///  called when a save-as project application command is conducted
        /// </summary>
        /// <returns>true if the saving is successful</returns>
        public virtual bool SaveAs()
        {
            if (SavePrompt == null)
            {   // save failed/canceled
                return false;
            }

            var filePath = SavePrompt();

            if (string.IsNullOrEmpty(filePath))
            {   // save failed/canceled
                return false;
            }

            if (filePath != FilePath)
            {
                if (!ValidatePathToSave(filePath))
                {
                    return false;
                }

                FilePath = filePath;

                SaveFileAs();
            }
            else
            {
                SaveFile();
            }

            State = ManagerStateEnum.Synced;
            FireEvent();

            return true;
        }

        public virtual bool Close()
        {
            var result = Quit(QuitPromptEventType.PromptToSaveOnClose);
            if (result)
            {
                DirtChecker = null;
            }
            return result;
        }

        #endregion

        protected virtual bool ValidatePathToLoad(string path)
        {
            return true;
        }

        protected virtual bool ValidatePathToSave(string path)
        {
            return true;
        }

        /// <summary>
        ///  General way how to close the currently opened filer
        /// </summary>
        /// <param name="type">The type of the quit prompt event</param>
        /// <returns>true if the file is closed</returns>
        protected virtual bool Quit(QuitPromptEventType type)
        {
            if (State == ManagerStateEnum.Closed)
            {
                return true;
            }

            if (!IsDirty)
            {
                return true;
            }

            var res = QuitPrompt(type);
            if (res == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (res == MessageBoxResult.Yes)
            {
                return Save();
            }

            // application specific implementation
            CloseFile();

            return true;
        }

        protected void FireEvent()
        {
            if (_eventArgs == null) return;
            if (StateChanged == null) return;
            StateChanged(_eventArgs);
            _eventArgs = null;
        }

        private void OnProjectDirty()
        {
            System.Diagnostics.Trace.Assert(DirtChecker != null);
            switch (State)
            {
                case ManagerStateEnum.New:
                case ManagerStateEnum.NewDirty:
                    State = DirtChecker.IsDirty
                                ? ManagerStateEnum.NewDirty
                                : ManagerStateEnum.New;
                    break;
                case ManagerStateEnum.Synced:
                case ManagerStateEnum.SyncedDirty:
                    State = DirtChecker.IsDirty
                                ? ManagerStateEnum.SyncedDirty
                                : ManagerStateEnum.Synced;
                    break;
            }

            FireEvent();
        }


        #region Application specific methods

        protected abstract void CreateFile();
        protected abstract void CloseFile();
        protected abstract void LoadFile();
        protected abstract void SaveFile();
        protected abstract void SaveFileAs();

        #endregion

        #endregion

        #region Fields

        private ManagerStateEnum _state;
        private string _filePath;
        private ManagerEventArgs _eventArgs;

        #endregion
    }
}
