using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;
using PhysioControls.ViewModel;

namespace PhysioControls.Printing
{
    public class PagePrinter : IEnumerable<PageViewModel>
    {
        #region Methods

        #region IEnumerable<PageViewModel> members

        public IEnumerator<PageViewModel> GetEnumerator()
        {
            return _pages.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public PagePrinter Add(PageViewModel page)
        {
            _pages.Add(page);
            return this;
        }

        public void Print(Size size, Size pageSize)
        {
            var fd = GetFixedDocument(size, pageSize);
            
            var pd = new PrintDialog();
            pd.PrintDocument(fd.DocumentPaginator, "Print as document");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"> </param>
        /// <param name="pageSize"></param>
        /// <param name="title"> </param>
        /// <param name="tempXps"></param>
        /// <remarks>
        ///  References:
        ///   http://stackoverflow.com/questions/2322064/how-can-i-produce-a-print-preview-of-a-flowdocument-in-a-wpf-application
        /// </remarks>
        public void PrintPreview(Size size, Size pageSize, string title, string tempXps)
        {
            try
            {
                using (var doc = new XpsDocument(tempXps, FileAccess.ReadWrite))
                {
                    var writer = XpsDocument.CreateXpsDocumentWriter(doc);
                    var fd = GetFixedDocument(size, pageSize);
                    writer.Write(fd.DocumentPaginator);
                }
                using (var doc = new XpsDocument(tempXps, FileAccess.Read))
                {
                    var fds = doc.GetFixedDocumentSequence();

                    var s = PreviewWindowXaml;
                    s = s.Replace("@@TITLE", title.Replace("'", "&apos;"));

                    using (var reader = new System.Xml.XmlTextReader(new StringReader(s)))
                    {
                        var preview = XamlReader.Load(reader) as Window;
                        if (preview == null)
                        {
                            throw new ApplicationException("Unable to create preview");
                        }
                        var dv1 = LogicalTreeHelper.FindLogicalNode(preview, "dv1") as DocumentViewer;
                        if (dv1 == null)
                        {
                            throw new ApplicationException("Unable to create document viewer");
                        }
                        dv1.Document = fds;
                        preview.ShowDialog();
                    }
                }
            }
            catch (ApplicationException ae)
            {
                var msg = string.Format("Error printing: '{0}'", ae.Message);
                MessageBox.Show(msg, "Physio Assist");
            }
            finally
            {
                if (File.Exists(tempXps))
                {
                    try
                    {
                        File.Delete(tempXps);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error closing temporary XPS file", "Physio Assist");
                    }
                }
            }
        }

        private FixedDocument GetFixedDocument(Size size, Size pageSize)
        {
            var fd = new FixedDocument();
            fd.DocumentPaginator.PageSize = pageSize;

            foreach (var page in _pages)
            {
                var pp = new PhysioPage
                {
                    PageViewModel = page,
                    Width = size.Width,
                    Height = size.Height
                };

                var fp = new FixedPage
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height
                };
                fp.Children.Add(pp);
                fp.Measure(size);
                fp.Arrange(new Rect(new Point(), size));
                fp.UpdateLayout();

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fp);
                fd.Pages.Add(pageContent);
            }

            return fd;
        }

        public static Size GetPixelSize(Size sizeInMM, double dpiX, double dpiY)
        {
            const double incPerMM = 0.0394;
            return new Size(sizeInMM.Width * incPerMM * dpiX, sizeInMM.Height * incPerMM * dpiY);
        }

        #endregion

        #region Fields

        private readonly IList<PageViewModel> _pages = new List<PageViewModel>();

        private const string PreviewWindowXaml =
              @"<Window xmlns ='http://schemas.microsoft.com/netfx/2007/xaml/presentation'"
            + @" xmlns:x ='http://schemas.microsoft.com/winfx/2006/xaml'"
            + @" Title ='Print Preview - @@TITLE'"
            + @" Height ='200'"
            + @" Width ='300'"
            + @" WindowStartupLocation ='CenterOwner'>"
            + @" <DocumentViewer Name='dv1'/>" 
            + @" </Window>"; 

        #endregion
    }
}
