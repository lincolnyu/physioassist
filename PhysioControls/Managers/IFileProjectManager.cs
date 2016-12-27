using System.Windows;
using PhysioControls.ViewModel;

namespace PhysioControls.Managers
{
    public enum ManagerStateEnum
    {
        Closed,
        New,
        NewDirty,
        Synced,
        SyncedDirty
    }

    public enum QuitPromptEventType
    {
        PromptToSaveOnNew,
        PromptToSaveOnLoad,
        PromptToSaveOnClose
    }

    public class ManagerEventArgs
    {

        #region Enumerations 

        public enum ActionEnum
        {
            ChangedState,
            ChangedFilePath,
            ChangedStateAndFilePath
        }

        #endregion

        #region Properties

        public ActionEnum Action { get; private set; }
        
        public ManagerStateEnum OldState { get; private set; }
        
        public ManagerStateEnum NewState { get; private set; }

        public string OldFilePath { get; private set; }

        public string NewFilePath { get; private set; }

        #endregion

        #region Methods

        internal void AddStateChange(ManagerStateEnum oldState, ManagerStateEnum newState)
        {
            OldState = oldState;
            NewState = newState;
            Action = Action == ActionEnum.ChangedFilePath ? ActionEnum.ChangedStateAndFilePath : ActionEnum.ChangedState;
        }

        internal void AddFileChange(string oldFilePath, string newFilePath)
        {
            OldFilePath = oldFilePath;
            NewFilePath = newFilePath;
            Action = Action == ActionEnum.ChangedState ? ActionEnum.ChangedStateAndFilePath : ActionEnum.ChangedFilePath;
        }

        #endregion
    }

    public delegate MessageBoxResult QuitPromptEvent(QuitPromptEventType e);

    public delegate string FileNamePromptEvent();

    public delegate void ManagerRequest();
    public delegate void ManagerEvent(ManagerEventArgs e);


    /// <summary>
    ///  Interface that a common file based project manager should implement
    /// </summary>
    /// <remarks>
    ///  State machine:
    ///                  New()                 Change
    ///   [Closed] ------------------) [New] -----------------) [NewDirty]
    ///          \                      |                      /
    ///            -----       Save (as)|Open()          ----- Save(as)
    ///   Open()         \              v              /
    ///                    -------)  [Synced]  (------
    ///                                 ^ 
    ///                           Save  ||  change         
    ///                                  v
    ///                             [Changed]
    /// </remarks>
    public interface IFileProjectManager
    {
        #region Properties

        /// <summary>
        ///  The path the project is saved at if saved
        /// </summary>
        string FilePath { get; }

        /// <summary>
        ///  The view model that contains the project to persist
        /// </summary>
        /// <remark>
        ///  Its being readonly means it should always the manager that provides
        ///  the project and its view model and the user should use 'NEW' functionality
        ///  of the manager to create a new project
        /// </remark>
        ProjectViewModel ProjectViewModel { get; }

        /// <summary>
        ///  State of the project amanger
        /// </summary>
        ManagerStateEnum State { get; }

        bool IsDirty { get; }

        #endregion

        #region Events

        #region Prompting events

        /// <summary>
        ///  fires when the program closes an unsaved file
        /// </summary>
        event QuitPromptEvent QuitPrompt;

        /// <summary>
        ///  fires when the manager requires the user to specify the path of the file to load
        /// </summary>
        event FileNamePromptEvent LoadPrompt;

        /// <summary>
        ///  fires when the manager requires the user to specify the path to save as
        /// </summary>
        event FileNamePromptEvent SavePrompt;

        #endregion

        #region Functional events

        /// <summary>
        ///  fires when the consumer of the manager is expected to perform initialisation of the newly created project, which is not change tracked 
        /// </summary>
        event ManagerRequest InitOnNew;
        
        /// <summary>
        ///  fires when the consumer of the manager is expected to perform initialisation of the loaded project, which is not change tracked
        /// </summary>
        event ManagerRequest InitOnLoad;

        /// <summary>
        ///  fires when the consumer of the manager is expected to clear up and disable the interface as there is no project opened
        /// </summary>
        event ManagerRequest ClearOnClose;

        /// <summary>
        ///  fires when the manager state has changed
        /// </summary>
        event ManagerEvent StateChanged;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        ///  called when a new project application command is conducted
        /// </summary>
        void New();

        /// <summary>
        ///  called when an open project application command is conducted
        /// </summary>
        void Open();

        /// <summary>
        ///  called when a save project application command is conducted
        /// </summary>
        /// <returns>true if the saving is successful</returns>
        bool Save();

        /// <summary>
        ///  called when a save-as project application command is conducted
        /// </summary>
        /// <returns>true if the saving is successful</returns>
        bool SaveAs();

        /// <summary>
        ///  called when a close project application command is conducted
        /// </summary>
        /// <returns>true if the closing process is performed</returns>
        bool Close();

        #endregion
    }
}
