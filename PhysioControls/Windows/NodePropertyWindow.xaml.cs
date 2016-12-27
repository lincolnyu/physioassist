using System.ComponentModel;
using PhysioControls.ViewModel;

namespace PhysioControls.Windows
{
    /// <summary>
    /// Interaction logic for NodePropertyWindow.xaml
    /// </summary>
    /// <remarks>
    /// Currently this Window class also serves as the ViewModel for the Window itself;
    /// Should separate this functionality from the class once it gets big enough
    /// </remarks>
    public partial class NodePropertyWindow : INotifyPropertyChanged
    {
        #region Properties

        public string WindowTitle
        {
            get { return "Node '" + Node.DisplayName + "'";  }
        }

        #endregion

        #region Constructors

        public NodePropertyWindow(BaseNodeViewModel node)
        {
            DataContext = this;
            Node = node;

            Node.PropertyChanged += (sender, args) =>
                {
                    if (PropertyChanged != null && args.PropertyName == "DisplayName")
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("WindowTitle"));
                    }
                };

            InitializeComponent();
        }

        #endregion

        #region Property

        public BaseNodeViewModel Node { get; set; }

        #endregion

        #region Members of INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        #endregion
    }
}
