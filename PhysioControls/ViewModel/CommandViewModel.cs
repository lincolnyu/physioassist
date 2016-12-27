using System;
using System.Windows.Input;

namespace PhysioControls.ViewModel
{
    public class CommandViewModel : ViewModelBase<ICommand>
    {
        #region Properties

        public new ICommand Model { get { return ModelAs<ICommand>(); } }

        public ICommand Command { get { return Model; } }

        public bool IsCheckable { get; set; }

        public bool IsChecked { get; set; }

        #endregion

        #region Constructors

        public CommandViewModel(ICommand command, string displayName)
            : base(command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            base.DisplayName = displayName;

            IsCheckable = false;
            IsChecked = false;
        }

        #endregion
    }
}
