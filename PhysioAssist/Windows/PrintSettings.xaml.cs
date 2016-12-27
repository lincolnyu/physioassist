using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PhysioAssist.Windows
{
    /// <summary>
    /// Interaction logic for PrintSettings.xaml
    /// </summary>
    public partial class PrintSettings : INotifyPropertyChanged, IDataErrorInfo
    {
        #region Enumerations

        public enum PageRangeOptions
        {
            AllPages,
            CurrentPage,
            CustomPages
        }

        #endregion

        #region Nested types

        public class Dpi
        {
            public string Name { get; set; }
            public double Value { get; set; }

            public Dpi (string name, double value)
            {
                Name = name;
                Value = value;
            }
        }

        #endregion

        #region Events

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion

        #region Properties

        #region IDataErrorInfo members

        public string Error { get; set; }

        public string this[string columnName]
        {
            get
            {
                var errorMessage = string.Empty;
                switch (columnName)
                {
                    case "PageRangeText":
                        if (PageRangeOption != PageRangeOptions.CustomPages) break;
                        if (_pageRange == null)
                        {
                            errorMessage = string.IsNullOrWhiteSpace(PageRangeText) ? "Empty page range string" : "Error page range format";
                        }
                        break;
                }
                Error = errorMessage;
                return errorMessage;
            }
        }

        #endregion

        public PageRangeOptions PageRangeOption
        {
            get { return _pageRangeOption; }

            set
            {
                if (_pageRangeOption == value) return;
                _pageRangeOption = value;
                OnPropertyChanged("PageRangeOption");
                OnPropertyChanged("PageRangeText");
            }
        }

        public string PageRangeText
        {
            get { return _pageRangeText; }
            set
            {
                if (_pageRangeText == value) return;
                _pageRangeText = value;
                _pageRange = GetPageRange(value);
                OnPropertyChanged("PageRangeText");
            }
        }

        public IList<int> PageRange
        {
            get
            {
                return _pageRange ?? new List<int>();
            }
        }

        public ISet<int> PageSet
        {
            get
            {
                var result = new HashSet<int>();
                foreach (var pageNo in PageRange)
                {
                    result.Add(pageNo);
                }
                return result;
            }
        }

        /// <summary>
        ///  All supported paper types
        /// </summary>
        public ReadOnlyObservableCollection<string> AllPaperTypes { get; private set; }

        /// <summary>
        ///  The current selected proeprty type
        /// </summary>
        public string PaperType { 
            get { return _paperType; }
            set
            {
                if (value == _paperType) return;
                _paperType = value;
                OnPropertyChanged("PaperType");
                UpdatePaperSizeAsPerType();
            }
        }

        public double PaperWidth
        {
            get { return _paperWidth; }
            set
            {
                if (_paperWidth == value) return;
                _paperWidth = value;
                OnPropertyChanged("PaperWidth");
            }
        }

        public double PaperHeight
        {
            get { return _paperHeight; }
            set
            {
                if (_paperHeight == value) return;
                _paperHeight = value;
                OnPropertyChanged("PaperHeight");
            }
        }

        public bool MatchSize { get; set; }

        public ReadOnlyObservableCollection<Dpi> AllDPIs { get; private set; }

        public double SelectedDPI
        {
            get { return _selectedDPI; }
            set
            {
                if (_selectedDPI == value) return;
                _selectedDPI = value;
                OnPropertyChanged("SelectedDPI");
            }
        }

        #endregion

        #region Constructors

        public PrintSettings()
        {
            InitializeComponent();

            PageRangeOption = PageRangeOptions.AllPages;

            AllPaperTypes = new ReadOnlyObservableCollection<string>(
                new ObservableCollection<string>
                    {
                        "A4",
                        "Custom"
                    });
            PaperType = "A4";

            AllDPIs = new ReadOnlyObservableCollection<Dpi>(
                new ObservableCollection<Dpi>
                    {
                        new Dpi("96", 96),
                        new Dpi("360", 360)
                    });
            SelectedDPI = 360;
        }

        #endregion

        #region Methods

        private void OkClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Error))
            {
                MessageBox.Show("Some errors occurred, print cancelled.", "PhysioAssist");
                DialogResult = null;
                return;
            }

            DialogResult = true;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static IList<int> GetSegment(string value)
        {
            var split = value.Split('-');

            if (split.Length < 2)
            {
                int val;
                return !int.TryParse(value, out val) ? null : new List<int> { val };
            }
            
            var strsegp = split[0].Trim();
            int begin;
            if (!int.TryParse(strsegp, out begin))
            {
                return null;
            }

            var indexEnd = split.Length >= 3 ? 2 : 1;
            strsegp = split[indexEnd].Trim();
            int end; 
                
            if (!int.TryParse(strsegp, out end))
            {
                return null;
            }

            var inc = 1;
            if (split.Length >= 3)
            {
                strsegp = split[1].Trim();
                if (!int.TryParse(strsegp, out inc))
                {
                    return null;
                }
            }

            var result = new List<int>();
            for (var i = begin; i <= end; i += inc)
            {
                var index = result.BinarySearch(i);
                if (index >= 0) continue;
                index = -index - 1;
                result.Insert(index, i);
            }
            return result;
        }

        private static IList<int> GetPageRange(string value)
        {
            var split = value.Split(',');
            var result = new List<int>();
            foreach (var pnseg in split.Select(strseg => strseg.Trim()).Select(GetSegment))
            {
                if (pnseg == null) return null;
                foreach (var val in pnseg)
                {
                    var index = result.BinarySearch(val);
                    if (index >= 0) continue;
                    index = -index - 1;
                    result.Insert(index, val);
                }
            }
            return result;
        }

        private void UpdatePaperSizeAsPerType()
        {
            switch (PaperType)
            {
                case "A4":
                    PaperWidth = 210;
                    PaperHeight = 297;
                    txtPaperWidth.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                    txtPaperHeight.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                    txtPaperWidth.IsReadOnly = true;
                    txtPaperHeight.IsReadOnly = true;
                    break;
                case "Custom":
                    txtPaperWidth.IsReadOnly = false;
                    txtPaperHeight.IsReadOnly = false;
                    break;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Fields

        private PageRangeOptions _pageRangeOption;
        private string _pageRangeText;
        private IList<int> _pageRange;
        private string _paperType;

        private double _paperWidth;
        private double _paperHeight;
        private double _selectedDPI;

        #endregion
    }
}
