using System.Windows.Input;
using PhysioAssist.Properties;

namespace PhysioAssist
{
    public class AppCommands
    {
        #region Properties

        public static RoutedUICommand Exit { get; private set; }

        public static RoutedUICommand New { get; private set; }

        public static RoutedUICommand Open { get; private set; }

        public static RoutedUICommand Save { get; private set; }

        public static RoutedUICommand SaveAs { get; private set; }

        public static RoutedUICommand Print { get; private set; }

        public static RoutedUICommand PrintPreview { get; private set; }

        public static RoutedUICommand NewPage { get; private set; }

        public static RoutedUICommand NextPage { get; private set; }

        public static RoutedUICommand PrevPage { get; private set; }

        public static RoutedUICommand SetImage { get; private set; }

        public static RoutedUICommand RemovePage { get; private set; }

        public static RoutedUICommand About { get; private set; }

        #endregion

        #region Constructors

        static AppCommands()
        {
            // AttachPickAndDrag commands
            Exit = new RoutedUICommand(Strings.Application_Command_Exit, "Exit", typeof(AppCommands), null);
            New = new RoutedUICommand(Strings.Application_Command_New, "New", typeof(AppCommands), null);
            Open = new RoutedUICommand(Strings.Application_Command_Open, "Open", typeof(AppCommands), null);
            Save = new RoutedUICommand(Strings.Application_Command_Save, "Save", typeof (AppCommands), null);
            SaveAs = new RoutedUICommand(Strings.Application_Coimmand_SavaAs, "SaveAs", typeof(AppCommands), null);
            Print = new RoutedUICommand(Strings.Application_Command_Print, "Print", typeof(AppCommands), null);
            PrintPreview = new RoutedUICommand(Strings.Application_Command_PrintPreview, "PrintPreview", typeof(AppCommands), null);
            NewPage = new RoutedUICommand(Strings.Application_Command_NewPage, "NewPage", typeof(AppCommands), null);
            RemovePage = new RoutedUICommand(Strings.Application_Command_RemovePage, "RemovePage", typeof(AppCommands), null);
            SetImage = new RoutedUICommand(Strings.Application_Command_SetImage, "SetImage", typeof(AppCommands), null);
            NextPage = new RoutedUICommand(Strings.Application_Command_NextPage, "NextPage", typeof(AppCommands), null);
            PrevPage = new RoutedUICommand(Strings.Application_Command_PrevPage, "PrevPage", typeof(AppCommands), null);
            About = new RoutedUICommand(Strings.Application_Command_About, "About", typeof(AppCommands), null);
        }

        #endregion
    }
}
