using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhysioControls.ChangeTracking;
using PhysioControls.Collections;
using PhysioControls.EntityDataModel;
using PhysioControls.Properties;
using PhysioControls.Utilities;
using PhysioControls.Windows;
using PhysioControls.Behavioural;
using Page = PhysioControls.EntityDataModel.Page;

namespace PhysioControls.ViewModel
{
    /// <summary>
    ///  View model for pages
    /// </summary>
    public class PageViewModel : ViewModelBase<Page>, IPickAndDragHandler, IExpandHandler, IZoomAndPanHandler
    {
        #region Nested types

        /// <summary>
        ///  Arguments of event that fires when background image changes
        /// </summary>
        public class BgImageChangedEventArgs
        {
            public ImageSource OldImage { get; internal set; }
            public ImageSource NewImage { get; internal set; }
        }

        #endregion

        #region Delegates

        /// <summary>
        ///  Delegate of which instance fires when canvas changes
        /// </summary>
        public delegate void CanvasChangedDelegate();

        /// <summary>
        ///  Delegate of which instance fires when backgraound image changes
        /// </summary>
        /// <param name="e"></param>
        public delegate void BgImageChangedDelegate(BgImageChangedEventArgs e);

        #endregion

        #region Properties

        #region Properties accessed by Views

        /// <summary>
        ///  returns the page number assigned to the page this view model models
        /// </summary>
        /// <remarks>
        ///  As page number is part of the page's default display name, so it calls UpdateDisplayName()
        ///  to update the DisplayName property
        /// </remarks>
        public int PageNo
        {
            get { return Model.PageNo; }
            set
            {
                if (value == Model.PageNo) return;
                Model.PageNo = value;
                OnPropertyChanged("PageNo");
                UpdateDisplayName();
            }
        }

        /// <summary>
        ///  returns the client region within which the page is displayed
        /// </summary>
        /// <remarks>
        ///  As it's a readonly property, notification made upon changes of affecting members
        /// </remarks>
        public Rect ClientRect
        {
            get
            {
                return new Rect(0, 0, Canvas.ActualWidth, Canvas.ActualHeight);
            }
        }

        /// <summary>
        ///  All direct child data objects of the page
        /// </summary>
        public EnhancedObservableCollection<DataObjectViewModel> DataObjects
        {
            get
            {
                if (_dataObjects == null)
                {
                    _dataObjects = new EnhancedObservableCollection<DataObjectViewModel>();
                    _dataObjects.CollectionChanged += OnDataObjectsChanged;
                    _dataObjects.CollectionClearing += OnDataObjectsClearing;
                }
                return _dataObjects;
            }
        }

        /// <summary>
        ///  All currently available commands to the page
        /// </summary>
        public EnhancedObservableCollection<CommandViewModel> AvailableCommands
        {
            get
            {
                if (_avaliableCommands == null)
                {
                    _avaliableCommands = new EnhancedObservableCollection<CommandViewModel>();
                    RenewAvailabeCommands();
                }
                return _avaliableCommands;
            }
        }

        /// <summary>
        ///  The property that is two-way bound to the tree view which is only single selectable
        /// </summary>
        /// <remarks>
        ///  TODO has to call it this for now, which is absolutely ugly; correct this once figure out a way to separate the style application in XAML
        /// </remarks>
        public bool IsSelectedOneWay
        {
            get { return Project != null && Project.CurrentPage == this; }
            set { Project.SetCurrentTo(this); }
        }

        /// <summary>
        ///  The flag that indicates if the context menu should be enabled on the page
        /// </summary>
        public bool IsContextMenuEnabled
        {
            get { return _isContextMenuEnabled;  }
            set
            {
                if (value == _isContextMenuEnabled) return;
                _isContextMenuEnabled = value;
                OnPropertyChanged("IsContextMenuEnabled");
            }
        }

        /// <summary>
        ///  The flag that indicates how the context menu is visible on the page
        /// </summary>
        public Visibility ContextMenuVisibility
        {
            get { return _contextMenuVisibility; }
            set
            {
                if (value == _contextMenuVisibility) return;
                _contextMenuVisibility = value;
                OnPropertyChanged("ContextMenuVisibility");
            }
        }

        /// <summary>
        ///  returns the background image as ImageSource
        /// </summary>
        public ImageSource BackgroundImage
        {
            get
            {
                if (BackgroundImageUri == null) return null;
                var imageSource = BackgroundImageUri.UriToImageSource();
                if (Math.Abs(CanvasCoverage.Width - imageSource.Width) < GeometryHelper.Epsilon
                    && Math.Abs(CanvasCoverage.Height - imageSource.Height) < GeometryHelper.Epsilon)
                    return imageSource;
                
                if (Math.Abs(CanvasCoverage.Width) < GeometryHelper.Epsilon || Math.Abs(CanvasCoverage.Height) < GeometryHelper.Epsilon)
                    return imageSource;

                var sourceBitmap = imageSource as BitmapImage;
                if(sourceBitmap == null) return imageSource; // unfortunately the image is not processable for now

                var rx = sourceBitmap.DpiX / 96;
                var ry = sourceBitmap.DpiY / 96;
                var targetWidth = (int)Math.Round(CanvasCoverage.Width * rx);
                var targetHeight = (int)Math.Round(CanvasCoverage.Height * ry);
                var x = (int)Math.Round(CanvasCoverage.X * rx);
                var y = (int)Math.Round(CanvasCoverage.Y * ry);
                var sourceRect = new Int32Rect(x, y, targetWidth, targetHeight);
                var bpp = sourceBitmap.Format.BitsPerPixel;
                var stride = targetWidth * ((bpp + 7) / 8);
                var bufSize = stride * targetHeight;
                var buf = Marshal.AllocHGlobal(bufSize);
                sourceBitmap.CopyPixels(sourceRect, buf, bufSize, stride);

                var targetRect = new Int32Rect(0, 0, targetWidth, targetHeight);
                var tempBmp = new WriteableBitmap(targetWidth, targetHeight,
                                                    sourceBitmap.DpiX, sourceBitmap.DpiY, sourceBitmap.Format, sourceBitmap.Palette);
                tempBmp.WritePixels(targetRect, buf, bufSize, stride);

                var frame = BitmapFrame.Create(tempBmp);
                var encoder = new BmpBitmapEncoder();
                var memoryStream = new MemoryStream();
                encoder.Frames.Add(frame);
                encoder.Save(memoryStream);

                var target = new BitmapImage();
                target.BeginInit();
                target.DecodePixelWidth = targetWidth;
                target.DecodePixelHeight = targetHeight;
                target.StreamSource = new MemoryStream(memoryStream.ToArray());
                target.EndInit();

                Marshal.FreeHGlobal(buf);

                return target;
            }
        }

        /// <summary>
        ///  The flag to get and set to indicate the visibility of selecting box
        /// </summary>
        public Visibility SelectBoxVisibility
        {
            get { return _selectBoxVisibility;  } 
            private set
            {
                if (value == _selectBoxVisibility) return;
                _selectBoxVisibility = value;
                OnPropertyChanged("SelectBoxVisibility");
            }
        }

        /// <summary>
        ///  The rectangle that specifies the location and size of the select box
        /// </summary>
        public Rect SelectBox
        {
            get { return _selectBox; }
            private set
            {
                if (value == _selectBox) return;
                _selectBox = value;
                OnPropertyChanged("SelectBox");
            }
        }


        #endregion

        #region Properties accessed by ViewModels

        /// <summary>
        ///  The view model of the project that owns the page this view model models
        /// </summary>
        public virtual ProjectViewModel Project
        {
            get { return _project; }
            set
            {
                if (value == _project) return;

                using (StartPropertyChangeRegion(value))
                {
                    _project = value;
                }
            }
        }

        /// <summary>
        ///  A flag to set and get to indicate if the page is in edit mode
        /// </summary>
        public bool IsInEditMode
        {
            get { return _isInEditMode; }
            set
            {
                if (value == _isInEditMode) return;
                _isInEditMode = value;
                RenewAvailabeCommands();
            }
        }

        /// <summary>
        ///  All the commands that can be used
        /// </summary>
        IDictionary<string, CommandViewModel> AllCommands
        {
            get
            {
                return _allCommands ?? (_allCommands = new Dictionary<string, CommandViewModel>
                    {
                        {
                            Strings.WorkspaceViewModel_Command_AddNode,
                            new CommandViewModel(new RelayCommand(AddNode), 
                                Strings.WorkspaceViewModel_Command_AddNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_AddSubNode,
                            new CommandViewModel(new RelayCommand(AddSubNode),
                                Strings.WorkspaceViewModel_Command_AddSubNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_DeleteNode,
                            new CommandViewModel(new RelayCommand(DeleteNode),
                                Strings.WorkspaceViewModel_Command_DeleteNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_DeleteSubNode,
                            new CommandViewModel(new RelayCommand(DeleteSubNode),
                                Strings.WorkspaceViewModel_Command_DeleteSubNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_EditNode,
                            new CommandViewModel(new RelayCommand(EditNode),
                                Strings.WorkspaceViewModel_Command_EditNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_EditSubNode,
                            new CommandViewModel(new RelayCommand(EditSubNode),
                                Strings.WorkspaceViewModel_Command_EditSubNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_DeleteNodes,
                            new CommandViewModel(new RelayCommand(DeleteNodes),
                                Strings.WorkspaceViewModel_Command_DeleteNodes)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_ViewNode,
                            new CommandViewModel(new RelayCommand(ViewNode),
                                Strings.WorkspaceViewModel_Command_ViewNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_ViewSubNode,
                            new CommandViewModel(new RelayCommand(ViewSubNode),
                                Strings.WorkspaceViewModel_Command_ViewSubNode)
                        },
                        {
                            Strings.WorkspaceViewModel_Command_DisplayComments,
                            new CommandViewModel(new RelayCommand(DisplayComments), 
                                Strings.WorkspaceViewModel_Command_DisplayComments) 
                                { IsCheckable = true }
                        }
                    });
            }
        }

        /// <summary>
        ///  The canvas the page is drawn on
        /// </summary>
        public Canvas Canvas
        {
            get { return _canvas; }
            set
            {
                if (Equals(value, _canvas))
                {
                    return;
                }
                if (_canvas != null)
                {
                    _canvas.SizeChanged -= CanvasOnSizeChanged;
                }
                
                _canvas = value;
                if (_canvas != null)
                {
                    _canvas.SizeChanged += CanvasOnSizeChanged;
                }
                OnCanvasChanged();
            }
        }

        public Rect CanvasCoverage
        {
            get { return _canvasCoverage; }
            set
            {
                if (value == _canvasCoverage)
                {
                    return;
                }
                _canvasCoverage = value;
                OnCanvasChanged();
                OnPropertyChanged("CanvasCoverage");
                OnPropertyChanged("BackgroundImage");
            }
        }

        #endregion

        public ISet<object> SelectedViewModels
        {
            get { return PickAndDragInfo.SelectedViewModels; }
        }

        public object HitViewModel
        {
            get { return PickAndDragInfo.HitViewModel; }
        }

        public string BackgroundImageUri
        {
            get { return Model.BgImageUri; }
            set
            {
                var oldImageUri = Model.BgImageUri;
                if (Equals(value, oldImageUri))
                {
                    return;
                }

                using (StartPropertyChangeRegion(value))
                {
                    Model.BgImageUri = value;
                    OnPropertyChanged("BackgroundImageUri");

                    var oldImage = oldImageUri != null ? oldImageUri.UriToImageSource() : null;
                    var newImage = value != null ? value.UriToImageSource() : null;
                    var e = new BgImageChangedEventArgs
                    {
                        OldImage = oldImage,
                        NewImage = newImage
                    };
                    OnBgImageChanged(e);

                    CanvasCoverage = newImage != null ? new Rect(0, 0, newImage.Width, newImage.Height) : new Rect();
                    OnPropertyChanged("BackgroundImage");
                }
            }
        }

        #endregion

        #region Constructors

        public PageViewModel(Page page, ProjectViewModel project)
            : base(page)
        {
            _project = project;
            SelectBoxVisibility = Visibility.Collapsed;
            DisplayContextMenu(false);  // initialised to not displaying
            UpdateDisplayName();
            InitialiseFromData();
            RegisterCollection(DataObjects);
        }

        #endregion

        #region Events

        public event CanvasChangedDelegate CanvasChanged;

        public event BgImageChangedDelegate BgImageChanged;

        #endregion

        #region Methods

        #region IPickAndDragHandler Members

        public PickAndDragInfo PickAndDragInfo
        {
            get
            {
                if (_pickAndDragInfo == null)
                {
                    _pickAndDragInfo = new PickAndDragInfo(this);
                    _pickAndDragInfo.HitViewModelSet +=
                        e =>
                            {
                                var node = e.NewValue as BaseNodeViewModel;
                                if (node != null)
                                {
                                    node.IsHitInternal = true;
                                }
                                else
                                {
                                    Project.UpdateHitInternal(null);
                                }
                            };
                }
                return _pickAndDragInfo;
            }
        }
        
        public bool IsDraggable(object candidateViewModel)
        {
            if (!IsInEditMode) return false;

            return candidateViewModel is IDraggableViewModel; 
        }

        public double DragThreshold(object candidateViewModel)
        {
            return 5.0;
        }

        public void DragBegin()
        {
            var draggedNode = PickAndDragInfo.HitViewModel as IDraggableViewModel;
            if (draggedNode == null)
            {
                return;
            }
            foreach (var selected in PickAndDragInfo.SelectedViewModels)
            {
                if (!(selected is IDraggableViewModel)) continue;
                _dragStarts[selected] = ((IDraggableViewModel)selected).LocationOnCanvas;
            }            
        }

        public void DragMove()
        {
            foreach (var selected in PickAndDragInfo.SelectedViewModels)
            {
                if (!(selected is IDraggableViewModel)) continue;

                if (!_dragging)
                {
                    string dragged = selected is BaseNodeViewModel ? "nodes" : "comments";
                    ChangesetManager.Instance.StartChangeset("Dragging " + dragged);
                    _dragging = true;
                }

                var move = PickAndDragInfo.CurrentPoint - PickAndDragInfo.StartPoint;
                ((IDraggableViewModel)selected).LocationOnCanvas = _dragStarts[selected] + move;
            }
        }

        public void DragEnd()
        {
            _dragStarts.Clear();
            if (_dragging)
            {
                _dragging = false;
                ChangesetManager.Instance.Commit();
            }
        }

        public bool IsSelectable(object candidateViewModel)
        {
            if (!(candidateViewModel is ISelectableViewModel))
                return false;

            Debug.Assert(candidateViewModel is BaseNodeViewModel || candidateViewModel is CommentsViewModel,
                "Unexpected selectable object");

            var first = SelectedViewModels.FirstOrDefault();
            if (first == null) return true;

            if (first is BaseNodeViewModel)
            {
                return candidateViewModel is BaseNodeViewModel;
            }
            if (first is CommentsViewModel)
            {
                return candidateViewModel is CommentsViewModel;
            }

            return false;
        }

        public void UpdateSelect()
        {
            lock(this)
            {
                _updateSelectFromPickAndDrag = true;
                foreach (var oldSelected in PickAndDragInfo.OldViewModels)
                {
                    var selectable = oldSelected as ISelectableViewModel;
                    if (selectable != null)
                    {
                        selectable.IsSelected = false;
                    }
                }
                foreach (var newSelected in PickAndDragInfo.NewViewModels)
                {
                    var selecatable = newSelected as ISelectableViewModel;
                    if (selecatable != null)
                    {
                        selecatable.IsSelected = true;
                    }
                }
                RenewAvailabeCommands();
                _updateSelectFromPickAndDrag = false;
            }
        }

        public bool IsBoxable(object candidateViewModel)
        {
            return candidateViewModel == this && IsInEditMode;
        }

        public void BoxBegin()
        {
            SelectBoxVisibility = Visibility.Visible;
            SelectBox = PickAndDragInfo.Box; 
        }

        public void BoxResize(bool updateBoxedObjectList)
        {
            SelectBox = PickAndDragInfo.Box;
            if (updateBoxedObjectList)
            {
                PickAndDragInfo.BoxedObjects = new List<object>();
                foreach (var dataObject in _dataObjects)
                {
                    var node = dataObject as NodeViewModel;
                    if (node != null && SelectBox.Contains(node.LocationOnCanvas))
                    {
                        PickAndDragInfo.BoxedObjects.Add(node);
                    }
                }
            }
        }

        public void BoxEnd(bool updateBoxedObjectList)
        {
            SelectBoxVisibility = Visibility.Collapsed;
            if (updateBoxedObjectList)
            {
                PickAndDragInfo.BoxedObjects = new List<object>();
                foreach (var dataObject in _dataObjects)
                {
                    var node = dataObject as NodeViewModel;
                    if (node != null && SelectBox.Contains(node.LocationOnCanvas))
                    {
                        PickAndDragInfo.BoxedObjects.Add(node);
                    }
                }
            }
        }

        #endregion

        #region IExpandHandler Members

        public void ToggleExpansion(ExpandEventArgs info)
        {
            var node = info.HitViewModel as BaseNodeViewModel;
            if (node == null) return;
            node.IsExpanded = !node.IsExpanded;
        }

        #endregion

        #region IZoomAndPanHandler Members

        public void ExposeInfo(ZoomAndPanInfo info)
        {
        }

        public void WheelChanged(Point point, double delta)
        {
            Trace.WriteLine(string.Format("Delta = {0}", delta));
            if (BackgroundImageUri == null) return;
            const double baseDelta = 120;
            var imageSource = BackgroundImageUri.UriToImageSource();
            var imageWidth =  imageSource.Width;
            var imageHeight = imageSource.Height;
            var canvasWidth = Canvas.ActualWidth;
            var canvasHeight = Canvas.ActualHeight;
            var sizeDelta = 1 - 0.1 * delta / baseDelta;
            var left = CanvasCoverage.Left + (1 - sizeDelta) * point.X * CanvasCoverage.Width / canvasWidth;
            var right = CanvasCoverage.Left + CanvasCoverage.Width * point.X / canvasWidth +
                        sizeDelta * CanvasCoverage.Width * (1 - point.X / canvasWidth);
            var top = CanvasCoverage.Top + (1 - sizeDelta) * point.Y * CanvasCoverage.Height / canvasHeight;
            var bottom = CanvasCoverage.Top + CanvasCoverage.Height * point.Y / canvasHeight +
                         sizeDelta * CanvasCoverage.Height * (1 - point.Y / canvasHeight);

            if (left < 0)
            {
                var w = right - left;
                left = 0;
                right = w;
            }
            if (right > imageWidth)
            {
                var w = right - left;
                right = imageWidth;
                left = right - w;
                if (left < 0) left = 0;
            }
            if (top < 0)
            {
                var h = bottom - top;
                top = 0;
                bottom = h;
            }
            if (bottom > imageHeight)
            {
                var h = bottom - top;
                bottom = imageHeight;
                top = bottom - h;
                if (top < 0) top = 0;
            }
            CanvasCoverage = new Rect(left, top, right - left, bottom - top);
        }

        public bool IsPanEnabled(MouseButtonEventArgs e)
        {
            return !IsInEditMode;   // _dragStarts.Count == 0;
        }

        public void PanBegin(Point point)
        {
            if (BackgroundImageUri == null) return;
            if (_panning) return;
            _panning = true;
            _savedCursor = Canvas.Cursor;
            Canvas.Cursor = Cursors.Hand;
            _panStart = point;
            _topLeftAtPanStart = CanvasCoverage.Location;
        }

        public void PanMove(Point point)
        {
            if (BackgroundImageUri == null)
            {
                _panning = false;
                return;
            }

            var v = point - _panStart;
            v.X *= CanvasCoverage.Size.Width/Canvas.ActualWidth;
            v.Y *= CanvasCoverage.Size.Height/Canvas.ActualHeight;
            var newCoverage = new Rect(_topLeftAtPanStart - v, CanvasCoverage.Size);

            var image = BackgroundImageUri.UriToImageSource();
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            if (newCoverage.X < 0)
            {
                newCoverage.X = 0;
            }
            else if (newCoverage.Right > imageWidth)
            {
                newCoverage.X = imageWidth - newCoverage.Width;
            }
            if (newCoverage.Y < 0)
            {
                newCoverage.Y = 0;
            }
            else if (newCoverage.Bottom > imageHeight)
            {
                newCoverage.Y = imageHeight - newCoverage.Height;
            }

            CanvasCoverage = newCoverage;
        }

        public void PanEnd(Point point)
        {
            _panning = false;
            Canvas.Cursor = _savedCursor;
        }

        #endregion

        private void InitialiseFromData()
        {
            if (BackgroundImage != null)
            {
                CanvasCoverage = new Rect(0, 0, BackgroundImage.Width, BackgroundImage.Height);
            }

            // we used to add nodes by clearing it all from view model and adding newly created one to it
            // and that was defintely just an expedient

            // don't need to worry about clearing event
            DataObjects.CollectionChanged -= OnDataObjectsChanged;

            foreach (var dataObject in Model.DataObjects)
            {
                var node = dataObject as Node;
                if (node == null) continue;
                var nodeViewModel = new NodeViewModel(node, this);
                if (!node.HasSize)
                {
                    nodeViewModel.SizeMode = NodeViewModel.SizeModeEnum.AbsoluteView;
                    nodeViewModel.SizeValue = new Size(15, 15);
                }
                CanvasChanged += nodeViewModel.OnCanvasChanged;
                BgImageChanged += nodeViewModel.OnBgImageChanged;
                if (Canvas != null) nodeViewModel.OnCanvasChanged();
                DataObjects.Add(nodeViewModel);
            }

            DataObjects.CollectionChanged += OnDataObjectsChanged;
        }

        private bool GetAreAllCommentsDisplayed()
        {
            bool allVisible = false; 
            foreach (object model in SelectedViewModels)
            {
                var node = model as BaseNodeViewModel;
                if (node != null)
                {
                    if (node.Comments.Visibility != Visibility.Visible) 
                        return false;
                    allVisible = true;
                }
            }
            return allVisible;
        }

        private void RenewAvailabeCommands()
        {
            AvailableCommands.Clear();
            var selectedNodeCount = SelectedViewModels.Count(x => x is BaseNodeViewModel);
            var selectedCommentsCount = SelectedViewModels.Count(x => x is CommentsViewModel);
            var displayCommentsCommand = AllCommands[Strings.WorkspaceViewModel_Command_DisplayComments];
            if (!IsInEditMode)
            {
                if (selectedNodeCount == 1)
                {
                    displayCommentsCommand.IsChecked = GetAreAllCommentsDisplayed();
                    if (HitViewModel is NodeViewModel)
                    {
                        AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_ViewNode]);
                        AvailableCommands.Add(displayCommentsCommand);
                    }
                    else if (HitViewModel is SubNodeViewModel)
                    {
                        AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_ViewSubNode]);
                        AvailableCommands.Add(displayCommentsCommand);
                    }
                }
                else if (selectedCommentsCount > 0)
                {
                    displayCommentsCommand.IsChecked = true;
                    AvailableCommands.Add(displayCommentsCommand);
                }
            }
            else if (selectedNodeCount == 1)
            {
                displayCommentsCommand.IsChecked = GetAreAllCommentsDisplayed();
                if (HitViewModel is NodeViewModel)
                {
                    AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_AddSubNode]);
                    AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_EditNode]);
                    AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_DeleteNode]);
                    AvailableCommands.Add(displayCommentsCommand);
                }
                else if (HitViewModel is SubNodeViewModel)
                {   // NOTE that in this case the number of selected nodes tracked by the pick-and-drag processor is still 1
                    AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_AddSubNode]);
                    AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_EditSubNode]);
                    AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_DeleteSubNode]);
                    AvailableCommands.Add(displayCommentsCommand);
                }
            }
            else if (selectedCommentsCount > 0)
            {
                displayCommentsCommand.IsChecked = true;
                AvailableCommands.Add(displayCommentsCommand);
            }
            else if (selectedNodeCount == 0)
            {
                AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_AddNode]);
            }
            else // if (selectedNodeCount > 1)
            {
                AvailableCommands.Add(AllCommands[Strings.WorkspaceViewModel_Command_DeleteNodes]);
            }
            DisplayContextMenu(AvailableCommands.Count > 0);
        }

        private void BaseNodeViewModelOnIsSelectedChanged(BaseNodeViewModel sender)
        {
            if (_updateSelectFromPickAndDrag) return;
            lock (this)
            {
                if (sender.IsSelected)
                    PickAndDragInfo.Select(sender);
                else
                    PickAndDragInfo.Deselect(sender);
            }
        }


        void AddNode(object param)
        {
            using (ChangesetManager.Instance.StartChangeset("Adding node"))
            {
                var point = (Point)param;
                // TODO initialise Node better, at least give it a valid ID
                var node = new Node();
                var nodeViewModel = new NodeViewModel(node, this)
                {
                    LocationOnCanvas = point,
                    //SizeMode = NodeViewModel.SizeModeEnum.RelativeToMinimum,
                    //SizeValue = new Size(0.05, 0.05)
                    SizeMode = NodeViewModel.SizeModeEnum.AbsoluteView,
                    SizeValue = new Size(15, 15)
                };

                nodeViewModel.IsSelectedChanged += BaseNodeViewModelOnIsSelectedChanged;
                DataObjects.Add(nodeViewModel);

                ChangesetManager.Instance.Commit();
            }
        }

        void AddSubNode(object param)
        {
            using (ChangesetManager.Instance.StartChangeset("Adding sub-node"))
            {
                var hitViewModel = PickAndDragInfo.HitViewModel;
                var baseNodeViewModel = (BaseNodeViewModel) hitViewModel;
                var subNode = baseNodeViewModel.AddSubNode(0.8);
                subNode.IsSelectedChanged += BaseNodeViewModelOnIsSelectedChanged;
                ChangesetManager.Instance.Commit();
            }
        }

        void DeleteNode(object param)
        {
            using (ChangesetManager.Instance.StartChangeset("Deleting node"))
            {
                var hitViewModel = PickAndDragInfo.HitViewModel;
                var nodeViewModel = hitViewModel as NodeViewModel;
                if (nodeViewModel == null)
                {
                    // TODO give error message or throw an appropriate exception
                    return;
                }
                DeleteNode(nodeViewModel);
                ChangesetManager.Instance.Commit();
            }
        }

        void DeleteSubNode(object param)
        {
            using (ChangesetManager.Instance.StartChangeset("Deleting sub-node"))
            {
                var hitViewModel = PickAndDragInfo.HitViewModel;
                var subNodeViewModel = hitViewModel as SubNodeViewModel;
                if (subNodeViewModel == null)
                {
                    // TODO give error message or throw an appropriate exception
                    return;
                }
                DeleteSubNode(subNodeViewModel);
                ChangesetManager.Instance.Commit();
            }
        }

        private void DeleteNodes(object obj)
        {
            using (ChangesetManager.Instance.StartChangeset("Deleting nodes"))
            {
                var toDeleteList = new List<BaseNodeViewModel>();
                foreach (var selected in SelectedViewModels)
                {
                    var nodeViewModel = selected as NodeViewModel;
                    if (nodeViewModel != null)
                    {
                        toDeleteList.Add(nodeViewModel);
                    }
                    var subNodeViewModel = selected as SubNodeViewModel;
                    if (subNodeViewModel != null && !subNodeViewModel.IsAncestorSelected)
                    {
                        toDeleteList.Add(subNodeViewModel);
                    }
                }
                foreach (var toDelete in toDeleteList)
                {
                    var nodeViewModel = toDelete as NodeViewModel;
                    if (nodeViewModel != null)
                    {
                        DeleteNode(nodeViewModel);
                    }
                    var subNodeViewModel = toDelete as SubNodeViewModel;
                    if (subNodeViewModel != null)
                    {
                        DeleteSubNode(subNodeViewModel);
                    }
                }
                ChangesetManager.Instance.Commit();
            }
        }

        private void DeleteNode(NodeViewModel nodeViewModel)
        {
            nodeViewModel.IsSelected = false;
            nodeViewModel.IsSelectedChanged -= BaseNodeViewModelOnIsSelectedChanged;
            DataObjects.Remove(nodeViewModel);
        }

        private void DeleteSubNode(SubNodeViewModel subNodeViewModel)
        {
            subNodeViewModel.IsSelected = false;
            subNodeViewModel.Parent.IsSelected = true;
            subNodeViewModel.IsSelectedChanged -= BaseNodeViewModelOnIsSelectedChanged;
            subNodeViewModel.Parent.RemoveSubNode(subNodeViewModel);
        }

        void EditNode(object param)
        {
            var hitViewModel = PickAndDragInfo.HitViewModel;
            var node = hitViewModel as NodeViewModel;
            if (node == null)
            {
                // TODO give error message or throw an appropriate exception
                return;
            }
            using (ChangesetManager.Instance.StartChangeset("Editing node"))
            {
                var window = new NodePropertyWindow(node);
                var owner = Canvas.FindAnsestralTreeObject<Window>();
                if (owner != null)
                {
                    window.Owner = owner;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                window.ShowDialog();
                ChangesetManager.Instance.Commit(true);
            }
        }

        void EditSubNode(object param)
        {
            var hitViewModel = PickAndDragInfo.HitViewModel;
            var subNode = hitViewModel as SubNodeViewModel;
            if (subNode == null)
            {
                // TODO give error message or throw an appropriate exception
                return;
            }
            using (ChangesetManager.Instance.StartChangeset("Editing sub-node"))
            {
                var window = new NodePropertyWindow(subNode);
                var owner = Canvas.FindAnsestralTreeObject<Window>();
                if (owner != null)
                {
                    window.Owner = owner;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                window.ShowDialog();
                ChangesetManager.Instance.Commit();
            }
        }

        private void ViewSubNode(object obj)
        {
            // TODO implement it
            //throw new NotImplementedException();
        }

        private void ViewNode(object obj)
        {
            // TODO implement it
            //throw new NotImplementedException();
        }

        private void DisplayComments(object obj)
        {
            // TODO enclose it with changeset

            var displayCommentsCommand = AllCommands[Strings.WorkspaceViewModel_Command_DisplayComments];

            // toggles visibility
            var visibility = displayCommentsCommand.IsChecked ? Visibility.Visible : Visibility.Collapsed;

            foreach (var selected in SelectedViewModels)
            {
                var comments = selected as CommentsViewModel;
                if (comments == null)
                {
                    var node = selected as BaseNodeViewModel;
                    if (node != null)
                    {
                        comments = node.Comments;
                    }
                }
                if (comments == null) continue;
                comments.Visibility = visibility;
            }
        }

        private void AddDataObjectModel(DataObjectViewModel dataObject)
        {
            dataObject.DisplayName = Model.GetValidName(dataObject.DisplayName);
            Model.AddDataObject(dataObject.Model);
            Project.Model.Persister.AddDataObject(dataObject.Model);
        }

        private void RemoveDataObjectModel(DataObjectViewModel dataObject)
        {
            if (!Model.ContainsDataObject(dataObject.Model))
            {
                throw new ArgumentException("Data object to remove doesn't exist");
            }
            Model.RemoveDataObject(dataObject.Model);
            Project.Model.Persister.RemoveDataObject(dataObject.Model);
        }

        void OnDataObjectsClearing(EnhancedObservableCollection<DataObjectViewModel> sender)
        {
            foreach (var dataObject in sender)
            {
                RemoveDataObjectModel(dataObject);
                CanvasChanged -= dataObject.OnCanvasChanged;
                BgImageChanged -= dataObject.OnBgImageChanged;
            }
        }

        void OnDataObjectsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (DataObjectViewModel dataObject in e.NewItems)
                    {
                        AddDataObjectModel(dataObject);
                        CanvasChanged += dataObject.OnCanvasChanged;
                        BgImageChanged += dataObject.OnBgImageChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (DataObjectViewModel dataObject in e.OldItems)
                    {
                        RemoveDataObjectModel(dataObject);
                        CanvasChanged -= dataObject.OnCanvasChanged;
                        BgImageChanged -= dataObject.OnBgImageChanged;
                    }
                    break;
            }
        }

        private void CanvasOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            OnCanvasChanged();
            OnPropertyChanged("ClientRect");
        }

        void OnCanvasChanged()
        {
            if (CanvasChanged != null)
            {
                CanvasChanged();
            }
        }

        void OnBgImageChanged(BgImageChangedEventArgs e)
        {
            if (BgImageChanged != null)
            {
                BgImageChanged(e);
            }
        }

        public void DeselectAll()
        {
            var selectedNodes = SelectedViewModels.OfType<BaseNodeViewModel>().ToList();
            foreach (var node in selectedNodes)
            {
                node.IsSelected = false;
            }
        }

        public void OnCurrentPageChanged()
        {
            OnPropertyChanged("IsSelectedOneWay");
        }

        private void DisplayContextMenu(bool enable)
        {
            IsContextMenuEnabled = enable;
            ContextMenuVisibility = enable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateDisplayName()
        {
            DisplayName = string.Format("Page {0}", PageNo);
        }

        #endregion

        #region Fields

        private ProjectViewModel _project;
        private Canvas _canvas;
        private EnhancedObservableCollection<DataObjectViewModel> _dataObjects;
        private IDictionary<string, CommandViewModel> _allCommands;
        private EnhancedObservableCollection<CommandViewModel> _avaliableCommands;

        private Rect _canvasCoverage;

        private bool _isInEditMode;
        private bool _isContextMenuEnabled;
        private Visibility _contextMenuVisibility;

        #region Selection Related

        private PickAndDragInfo _pickAndDragInfo;
        private bool _dragging;
        private readonly IDictionary<object, Point> _dragStarts = new Dictionary<object, Point>();
        /// <summary>
        ///  A flag that is set on when the pick and drag module is updating selection status
        ///  to this view model to prevent a reverse select update
        /// </summary>
        private bool _updateSelectFromPickAndDrag;

        #endregion

        #region Zoom/Pan Related

        private Point _panStart;
        private Point _topLeftAtPanStart;
        private Cursor _savedCursor;
        private bool _panning;

        #endregion

        #region Select Box Related

        private Visibility _selectBoxVisibility;

        /// <summary>
        ///  The backing field for property SelectBox
        /// </summary>
        private Rect _selectBox;

        #endregion

        #endregion
    }
}
