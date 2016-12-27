using System;
using System.Windows;
using PhysioControls.ChangeTracking;
using PhysioControls.ViewModel;
using Page = PhysioControls.EntityDataModel.Page;

namespace PhysioControls
{
    /// <summary>
    /// Interaction logic for PhysioPage.xaml
    /// </summary>
    public partial class PhysioPage
    {
        #region Properties

        #region Properties accessed by users

        // TODO re-evaluate if it's the appropriate way to specify the background image
        public static readonly DependencyProperty BackgroundImageSourceProperty =
            DependencyProperty.Register("BackgroundImageUri", typeof(Uri), typeof(PhysioPage),
                                        new PropertyMetadata(default(Uri), OnBackgroundImageChanged));

        public static readonly DependencyProperty PageModelProperty =
            DependencyProperty.Register("PageModel", typeof (Page), typeof (PhysioPage),
                                        new PropertyMetadata(default(Page), OnPageModelChanged));

        #endregion

        public Uri BackgroundImageSource
        {
            get { return (Uri)GetValue(BackgroundImageSourceProperty); }
            set { SetValue(BackgroundImageSourceProperty, value); }
        }

        public Page PageModel
        {
            get { return (Page) GetValue(PageModelProperty); }
            set { SetValue(PageModelProperty, value); }
        }

        /// <summary>
        ///  view model of the page the control is currently presenting
        /// </summary>
        /// <remarks>
        ///  It allows a temporary assignment of null view model for allowing the view model 
        ///  to be taken off from the control for i.e. printing purposes
        /// </remarks>
        public PageViewModel PageViewModel
        {
            get { return _pageViewModel; }
            set 
            {
                if (value == _pageViewModel) return;
                _pageViewModel = value;
                if (_pageViewModel != null)
                {
                    _pageViewModel.Canvas = canvas;
                }
                DataContext = value;
                _settingPageViewModel = true;
                PageModel = _pageViewModel != null? _pageViewModel.Model : null;
                _settingPageViewModel = false;
            }
        }

        #endregion

        #region Constructors

        public PhysioPage()
        {
            // TODO create a new workspace just for now
            //PageViewModel = new PageViewModel(new Page(), null) {Canvas = canvas};
            InitializeComponent();
        }

        #endregion

        #region Methods

        private static void OnBackgroundImageChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var page = (PhysioPage) sender;
            if (page.PageViewModel.BackgroundImageUri != null)
            {
                var newValue = ((Uri) e.NewValue).AbsoluteUri;
                if (newValue != page.PageViewModel.BackgroundImageUri)
                {
                    using (ChangesetManager.Instance.StartChangeset("Changing Image"))
                    {
                        page.PageViewModel.BackgroundImageUri = ((Uri) e.NewValue).AbsoluteUri;
                        ChangesetManager.Instance.Commit();
                    }
                }
            }
            else
            {   // don't undo the change from a null image
                page.PageViewModel.BackgroundImageUri = ((Uri)e.NewValue).AbsoluteUri;
            }
        }

        private static void OnPageModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var page = (PhysioPage) sender;
            if (page._settingPageViewModel) return;
            page.PageViewModel = new PageViewModel((Page)e.NewValue, null);
        }

        #endregion

        #region Fields

        private PageViewModel _pageViewModel;
        private bool _settingPageViewModel;

        #endregion
    }
}
