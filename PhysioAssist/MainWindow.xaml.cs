using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PhysioAssist.Properties;
using PhysioAssist.Windows;
using PhysioControls.Managers;
using PhysioControls.Printing;
using PhysioControls.ViewModel;
using Page = PhysioControls.EntityDataModel.Page;

namespace PhysioAssist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Properties

        public bool IsInEditMode
        {
            get
            {
                if (PhysioPage == null || PhysioPage.PageViewModel == null) return false;
                return PhysioPage.PageViewModel.IsInEditMode;
            }
            set 
            {
                if (PhysioPage == null || PhysioPage.PageViewModel == null) return;
                if (PhysioPage.PageViewModel.IsInEditMode == value) return;
                PhysioPage.PageViewModel.IsInEditMode = value;
                OnPropertyChanged("IsInEditMode");
            }
        }

        private ProjectViewModel ProjectViewModel
        {
            get { return _projectViewModel; }
            set
            {
                if (value == _projectViewModel) return;
                if (_projectViewModel != null)
                {
                    _projectViewModel.CurrentPageChanged -= ProjectViewModelOnCurrentPageChanged;
                }
                _projectViewModel = value;
                if (_projectViewModel != null)
                {
                    _projectViewModel.CurrentPageChanged += ProjectViewModelOnCurrentPageChanged;
                    ProjectHierarchy.ItemsSource = _projectViewModel.Pages;
                }
                else
                {
                    ProjectHierarchy.ItemsSource = null;    // TODO is this allowed?
                }
            }
        }

        #endregion

        #region Events

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            if (Settings.Default.UseTrackingSdf)
            {
                var currDir = Directory.GetCurrentDirectory();
                var trackingSdfPath = Path.Combine(currDir, "trackingdb.sdf");
                _projectManager = new TrackingSdfManager(trackingSdfPath);
            }
            else
            {
                _projectManager = new SdfProjectManager();
            }

            _projectManager.InitOnLoad += ProjectManagerInitOnLoad;
            _projectManager.InitOnNew += ProjectManagerInitOnNew;
            _projectManager.ClearOnClose += ProjectManagerClear;
            _projectManager.LoadPrompt += ProjectManagerLoadPrompt;
            _projectManager.SavePrompt += ProjectManagerSavePrompt;
            _projectManager.QuitPrompt += ProjectManagerQuitPrompt;
            _projectManager.StateChanged += ProjectManagerStateChanged;

            // changes are only tracked and displayed properly after the DataContext is properly as above
            DataContext = GlobalViewModel.Instance;

            _projectManager.New();

            //LoadDemoPage();
        }

        #endregion

        #region Methods

        #region Handlers of event from project manager

        private void EnableUI(bool enable)
        {
            PhysioPage.IsEnabled = enable;
            ChangeList.IsEnabled = enable;
        }

        private void ProjectManagerInitOnLoad()
        {
            EnableUI(true);

            ProjectViewModel = _projectManager.ProjectViewModel;

            PhysioPage.PageViewModel = ProjectViewModel.Pages[0];

            OnPageChanged();
        }

        private void ProjectManagerInitOnNew()
        {
            EnableUI(true);

            ProjectViewModel = _projectManager.ProjectViewModel;

            LoadInitialPage();
            PhysioPage.PageViewModel = ProjectViewModel.Pages[0];
            
            OnPageChanged();

            IsInEditMode = true;
        }

        private void ProjectManagerClear()
        {
            EnableUI(false);

            ProjectViewModel = null;
            PhysioPage.PageViewModel = null;

            OnPageChanged();
            
            GlobalViewModel.Instance.ResetChangesets();
        }

        private static string ProjectManagerLoadPrompt()
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = ".sdf",
                Filter = "Physio Project Files|*.sdf"
            };
            return ofd.ShowDialog() != true ? null : ofd.FileName;
        }

        private static string ProjectManagerSavePrompt()
        {
            var sfd = new SaveFileDialog
            {
                DefaultExt = ".sdf",
                Filter = "Physio Project Files|*.sdf"
            };
            return sfd.ShowDialog() != true ? null : sfd.FileName;
        }

        private static MessageBoxResult ProjectManagerQuitPrompt(QuitPromptEventType e)
        {
            return MessageBox.Show("Project not saved, do you want to save it?", "PhysioAssist",
                MessageBoxButton.YesNoCancel);
        }

        private void ProjectManagerStateChanged(ManagerEventArgs e)
        {
            UpdateTitle();
        }

        #endregion

        #region Project command handlers

        private void ExitCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (_projectManager.Close())
            {
                Close();
            }
        }

        private void NewCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _projectManager.New();
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _projectManager.Open();
        }

        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _projectManager.Save();
        }

        private void SaveAsCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _projectManager.SaveAs();
        }

        private PrintSettings GetPrinterSettings()
        {
            var settings = new PrintSettings
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (settings.ShowDialog() != true)
            {
                return null;
            }

            return settings;
        }

        private PagePrinter GetPrinter(PrintSettings settings)
        {
            if (settings == null)
            {
                return null;
            }
            var printer = new PagePrinter();
            switch (settings.PageRangeOption)
            {
                case PrintSettings.PageRangeOptions.CurrentPage:
                    printer.Add(PhysioPage.PageViewModel);
                    break;
                case PrintSettings.PageRangeOptions.AllPages:
                    foreach (var page in _projectManager.ProjectViewModel.Pages)
                    {
                        printer.Add(page);
                    }
                    break;
                default:
                    {
                        var pageSet = settings.PageSet;
                        foreach (var page in _projectManager.ProjectViewModel.Pages.Where(page => pageSet.Contains(page.PageNo)))
                        {
                            printer.Add(page);
                        }
                    }
                    break;
            }
            return printer;
        }

        private static Size GetPageSize(PrintSettings settings)
        {
            var sizeInMM = new Size(settings.PaperWidth, settings.PaperHeight);
            return PagePrinter.GetPixelSize(sizeInMM, settings.SelectedDPI, settings.SelectedDPI);
        }

        private void PrintCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var settings = GetPrinterSettings();
            var printer = GetPrinter(settings);
            if (printer == null)
            {
                return;
            }

            var pageSize = GetPageSize(settings);
            var uiSize = settings.MatchSize ? pageSize : new Size(PhysioPage.ActualWidth, PhysioPage.ActualHeight);

            var savedVm = PhysioPage.PageViewModel;
            PhysioPage.PageViewModel = null;
            printer.Print(uiSize, pageSize);
            PhysioPage.PageViewModel = savedVm;
        }

        private void PrintPreviewCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var settings = GetPrinterSettings(); 
            var printer = GetPrinter(settings);
            if (printer == null)
            {
                return;
            }

            var pageSize = GetPageSize(settings);
            var uiSize = settings.MatchSize ? pageSize : new Size(PhysioPage.ActualWidth, PhysioPage.ActualHeight);
            var currDir = Directory.GetCurrentDirectory();
            var xpsPath = Path.Combine(currDir, "preview.xps");

            var savedVm = PhysioPage.PageViewModel;
            PhysioPage.PageViewModel = null;
            printer.PrintPreview(uiSize, pageSize, "Preview", xpsPath);
            PhysioPage.PageViewModel = savedVm;
        }

        #endregion

        #region Edit/view command handlers

        private void NewPageCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var res = (BitmapImage)Resources["Foot"];
            var uri = new Uri(res.BaseUri, res.UriSource);
            var page = new Page { BgImageUri = uri.ToString() };
            ProjectViewModel.AddPage(page);
            IsInEditMode = true;
        }

        private void RemovePageCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ProjectViewModel.RemoveCurrentPage())
            {
                MessageBox.Show("This is the last page in the project, it's not allowed to be removed.", "Notice");
            }
        }

        private void NextPageCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ProjectViewModel.CurrentPageIndex < ProjectViewModel.Pages.Count - 1)
            {
                ProjectViewModel.CurrentPageIndex++;
            }
        }

        private void PrevPageCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ProjectViewModel.CurrentPageIndex <= 0) return;
            ProjectViewModel.CurrentPageIndex--;
            OnPageChanged();
        }

        private void AboutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        #endregion

        #region Window event handlers

        private void MainPhysioWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !_projectManager.Close();
        }

        #endregion

        #region ListView (for undo/redo) event handlers

        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            System.Diagnostics.Trace.Assert(listView != null);
            
            if (listView.SelectedItems.Count != 1) return;

            // undo/redo to this index
            var index = listView.SelectedIndex;
            GlobalViewModel.Instance.RestoreTo(index);
        }

        private void ListViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount <= 1) return;
            var listView = sender as ListView;
            System.Diagnostics.Trace.Assert(listView != null);
            GlobalViewModel.Instance.RemoveFrom(listView.SelectedIndex);
        }

        private void ListViewPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount <= 1) return;
            var listView = sender as ListView;
            System.Diagnostics.Trace.Assert(listView != null);
            GlobalViewModel.Instance.RemoveTo(listView.SelectedIndex);
        }

        #endregion

        #region Other event handlers

        private void UpdateTitle()
        {
            Title = "PhysioAssist";

            if (!string.IsNullOrWhiteSpace(_projectManager.FilePath))
            {
                var fileName = Path.GetFileName(_projectManager.FilePath);
                Title = string.Format("PhysioAssist: {0}", fileName);
            }
            if (_projectManager.IsDirty)
            {
                Title += "*";
            }
        }

        #endregion

        private void LoadInitialPage()
        {
            var res = (BitmapImage)Resources["Foot"];
            var uri = new Uri(res.BaseUri, res.UriSource);
            var page = new Page { BgImageUri = uri.ToString() };

            ProjectViewModel.AddPage(page);
        }
        /*
        private void LoadDemoPage()
        {
            var res = (BitmapImage)Resources["Femur"];
            var uri = new Uri(res.BaseUri, res.UriSource);
            var page = new Page { BgImageUri = uri.ToString() };

            ProjectViewModel.AddPage(page);
        }*/

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        private void ProjectViewModelOnCurrentPageChanged(ProjectViewModel sender)
        {
            PhysioPage.PageViewModel = sender.CurrentPage;
            OnPageChanged();
        }

        private void SetImageCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    DefaultExt = ".jpg",
                    Filter = "Image Files|*.jpg;*.jpeg;*.bmp;*.gif;*.png"
                };
            if (ofd.ShowDialog() == true)
            {
                PhysioPage.BackgroundImageSource = new Uri(ofd.FileName);
            }
        }

        private void OnPageChanged()
        {
            OnPropertyChanged("IsInEditMode");
        }

        private void ProjectHierarchySelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var page = e.NewValue as PageViewModel;

            if (page != null)
            {
                var index = _projectViewModel.Pages.IndexOf(page);
                _projectViewModel.CurrentPageIndex = index;
                return;
            }

            var node = e.NewValue as BaseNodeViewModel;
            if (node != null)
            {
                var index = _projectViewModel.Pages.IndexOf(node.Page);
                _projectViewModel.CurrentPageIndex = index;
                //return;
            }
        }
        
        #endregion

        #region Fields

        private readonly IFileProjectManager _projectManager;
        
        private ProjectViewModel _projectViewModel;

        #endregion

    }
}
