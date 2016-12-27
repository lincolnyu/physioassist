using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PhysioControls.Behavioural
{
    public static class PickAndDrag
    {
        #region Fields

        public const double DefaultDragThreshold = -1.0;

        #region Attachable properties

        public static readonly DependencyProperty IsPickAndDraggableProperty =
            DependencyProperty.RegisterAttached("IsPickAndDraggable", typeof (bool),
                                                typeof (PickAndDrag),
                                                new PropertyMetadata(default(bool), IsPickAndDraggableChangedCallback));

        public static readonly DependencyProperty IsPickableWithBoxProperty =
            DependencyProperty.RegisterAttached("IsPickableWithBox", typeof(bool), typeof(PickAndDrag),
                                                new PropertyMetadata(default(bool), IsPickableWithBoxChangedCallback));

        public static readonly DependencyProperty PickAndDragHandlerProperty =
            DependencyProperty.RegisterAttached("PickAndDragHandler", typeof (IPickAndDragHandler),
                                                typeof (PickAndDrag),
                                                new PropertyMetadata(null, PickAndDragHandlerChangedCallback));

        /// <summary>
        ///  A value indicating how much the user needs to drag the draggable element away from the original point 
        ///  before the drag is considered to be activated. The default is a negative value which means a value 
        ///  if any found on a finer grained element should be used or there's no threshold at all
        /// </summary>
        public static readonly DependencyProperty DragThresholdProperty =
            DependencyProperty.RegisterAttached("DragThreshold", typeof (double), typeof (DependencyProperty),
                                                new PropertyMetadata(DefaultDragThreshold));

        /// <summary>
        ///  A map from UI element that contains selectable objects to its corresponding SelectInfo
        /// </summary>
        private static readonly IDictionary<UIElement, PickAndDragInfo> PickAndDragGroups =
            new Dictionary<UIElement, PickAndDragInfo>();

        #endregion

        #endregion

        #region Methods

        #region Getters and setters of attachable properties

        public static bool GetIsPickAndDraggable(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsPickAndDraggableProperty);
        }

        public static void SetIsPickAndDraggable(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsPickAndDraggableProperty, value);
        }

        public static IPickAndDragHandler GetPickAndDragHandler(DependencyObject dependencyObject)
        {
            return (IPickAndDragHandler)dependencyObject.GetValue(PickAndDragHandlerProperty);
        }

        public static void SetPickAndDragHandler(DependencyObject dependencyObject, IPickAndDragHandler value)
        {
            dependencyObject.SetValue(PickAndDragHandlerProperty, value);
        }

        public static double GetDragThreshold(DependencyObject dependencyObject)
        {
            return (double) dependencyObject.GetValue(DragThresholdProperty);
        }

        public static void SetDragThreshold(DependencyObject dependencyObject, double value)
        {
            dependencyObject.SetValue(DragThresholdProperty, value);
        }

        public static bool GetIsPickableWithBox(DependencyObject dependencyObject)
        {
            return (bool) dependencyObject.GetValue(IsPickableWithBoxProperty);
        }

        public static void SetIsPickableWithBox(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsPickableWithBoxProperty, value);
        }

        #endregion

        #region Change handers of attachable properties

        private static void IsPickAndDraggableChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = (UIElement) d;

            if ((bool)e.NewValue)
            {   // Makes the dependency object draggable
                uiElement.PreviewMouseLeftButtonDown += PickAndDraggableOnPreviewMouseLeftButtonDown;
                uiElement.PreviewMouseLeftButtonUp += PickAndDraggableOnPreviewMouseLeftButtonUp;
                uiElement.PreviewMouseMove += PickAndDraggableOnPreviewMouseMove;
                uiElement.PreviewMouseRightButtonDown += PickAndDraggableOnPreviewMouseRightButtonDown;
            }
            else
            {   // Makes the dependency object undraggable
                uiElement.PreviewMouseLeftButtonDown -= PickAndDraggableOnPreviewMouseLeftButtonDown;
                uiElement.PreviewMouseLeftButtonUp -= PickAndDraggableOnPreviewMouseLeftButtonUp;
                uiElement.PreviewMouseMove -= PickAndDraggableOnPreviewMouseMove;
                uiElement.PreviewMouseRightButtonDown -= PickAndDraggableOnPreviewMouseRightButtonDown;
                if (PickAndDragGroups.ContainsKey(uiElement))
                {
                    if (GetPickAndDragHandler(d) == null)
                    {
                        PickAndDragGroups.Remove(uiElement);
                    }
                }
            }
        }

        private static void IsPickableWithBoxChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Nothing needs to be done
        }

        private static void PickAndDragHandlerChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = (UIElement)d;

            if (e.NewValue != null)
            {
                var handler = (IPickAndDragHandler)e.NewValue;
                PickAndDragGroups[uiElement] = handler.PickAndDragInfo;
            }
            else
            {
                if (PickAndDragGroups.ContainsKey(uiElement))
                {
                    if (!GetIsPickAndDraggable(d))
                    {
                        PickAndDragGroups.Remove(uiElement);
                    }
                }
            }
        }

        #endregion

        private static void DoMouseLeftButtonUp(PickAndDragInfo info)
        {
            var handler = info.PickAndDragHandler;

            var wasPoisted = info.DragState == PickAndDragInfo.DragStateEnum.Poisted;
            var controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) > 0;

            info.DragState = PickAndDragInfo.DragStateEnum.DragDone;

            handler.DragEnd();

            if (wasPoisted && controlPressed)
            {
                var candidateViewModel = info.HitViewModel;
                if (info.SelectedViewModels.Contains(candidateViewModel))
                {
                    info.OldViewModels.Add(candidateViewModel);
                    info.SelectedViewModels.Remove(candidateViewModel);
                }
                UpdateSelect(info);
            }
        }

        private static void UpdateSelect(PickAndDragInfo info)
        {
            info.PickAndDragHandler.UpdateSelect();
            info.NewViewModels.Clear();
            info.OldViewModels.Clear();
        }

        private static double GetDragThreshold(PickAndDragInfo info)
        {
            var threshold = info.PickAndDragHandler.DragThreshold(info.HitViewModel);
            if (threshold > 0) return threshold;

            threshold = GetDragThreshold(info.HitElement);
            if (threshold > 0) return threshold;

            threshold = GetDragThreshold(info.DragBase);
            return threshold > 0 ? threshold : DefaultDragThreshold;
        }

        private static void TryDrag(UIElement dragBase, Point startPoint, PickAndDragInfo info)
        {
            var handler = info.PickAndDragHandler;
            var candidateViewModel = info.HitViewModel;
            // If the view model is unavailable, it must not be a valid select draggable object
            var draggable = candidateViewModel != null && handler.IsDraggable(candidateViewModel);

            if (!draggable) return;

            info.DragBase = dragBase;
            info.StartPoint = startPoint;
            info.HitViewModel = candidateViewModel;
            info.DragState = GetDragThreshold(info) > 0
                                 ? PickAndDragInfo.DragStateEnum.Poisted
                                 : PickAndDragInfo.DragStateEnum.BeingDragged;

            handler.DragBegin();
        }

        private static void TrySelect(PickAndDragInfo info)
        {
            var handler = info.PickAndDragHandler;
            // If the view model is unavailable, it must not be a valid selectable object
            var candidateViewModel = info.HitViewModel;
            var selectable = candidateViewModel != null && handler.IsSelectable(candidateViewModel);
            var controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) > 0;

            // Clear event data
            info.NewViewModels.Clear();
            info.OldViewModels.Clear();
            if (controlPressed && selectable)
            {
                if (info.SelectedViewModels.Contains(candidateViewModel))
                {
                    info.OldViewModels.Add(candidateViewModel);
                    info.SelectedViewModels.Remove(candidateViewModel);
                }
                else
                {
                    info.NewViewModels.Add(candidateViewModel);
                    info.SelectedViewModels.Add(candidateViewModel);
                }
            }
            else if (!controlPressed)
            {
                // deselect all
                foreach (var vm in info.SelectedViewModels)
                {
                    info.OldViewModels.Add(vm);
                }
                info.SelectedViewModels.Clear();

                // select the one hit
                if (selectable)
                {
                    info.SelectedViewModels.Add(candidateViewModel);
                    info.NewViewModels.Add(candidateViewModel);
                }
            }

            UpdateSelect(info);
        }

        private static void TryCreateBox(PickAndDragInfo info, Point point)
        {
            var handler = info.PickAndDragHandler;
            var controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
            var shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

            if (!controlPressed && !shiftPressed)
            {
                // deselect all
                foreach (var vm in info.SelectedViewModels)
                {
                    info.OldViewModels.Add(vm);
                }
                info.SelectedViewModels.Clear();
            }

            info.BoxStart = point;
            info.Box = new Rect(info.BoxStart, info.BoxStart);
            info.IsCreatingBox = true;
            handler.BoxBegin();
        }

        private static void UpdateBox(PickAndDragInfo info)
        {
            info.OldBox = info.Box;
            info.Box = new Rect(info.BoxStart, info.CurrentPoint);

            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                EndCreatingBox(info);
            }

            info.PickAndDragHandler.BoxResize(false);
        }

        private static void EndCreatingBox(PickAndDragInfo info)
        {
            info.PickAndDragHandler.BoxEnd(true);
            var controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) > 0;

            if (info.OldBoxedObjects != null && info.NewBoxedObjects != null)
            {
                foreach (var unboxed in info.OldViewModels)
                {
                    if (controlPressed && info.SelectedViewModels.Contains(unboxed))
                    {   // toggle selection
                        info.NewViewModels.Add(unboxed);
                        info.SelectedViewModels.Add(unboxed);
                    }
                    else if (!info.SelectedViewModels.Contains(unboxed))
                    {
                        info.OldViewModels.Add(unboxed);
                        info.SelectedViewModels.Remove(unboxed);
                    }
                }
                foreach (var boxed in info.NewBoxedObjects)
                {
                    if (controlPressed && info.SelectedViewModels.Contains(boxed))
                    {   // toggle selection
                        info.OldViewModels.Add(boxed);
                        info.SelectedViewModels.Remove(boxed);
                    }
                    else if (!info.SelectedViewModels.Contains(boxed))
                    {
                        info.NewViewModels.Add(boxed);
                        info.SelectedViewModels.Add(boxed);
                    }
                }
            }
            else if (info.BoxedObjects != null)
            {
                foreach (var boxed in info.BoxedObjects)
                {
                    if (controlPressed && info.SelectedViewModels.Contains(boxed))
                    {   // toggle selection
                        info.OldViewModels.Add(boxed);
                        info.SelectedViewModels.Remove(boxed);
                    }
                    else if (!info.SelectedViewModels.Contains(boxed))
                    {
                        info.NewViewModels.Add(boxed);
                        info.SelectedViewModels.Add(boxed);
                    }
                }
            }

            UpdateSelect(info);

            info.IsCreatingBox = false;
        }

        private static void PickAndDraggableOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var point = e.GetPosition(uiElement);
            var hitElement = uiElement.InputHitTest(point);

            if (hitElement == null)
            {   // may happen like when a message box is clicked on
                return;
            }

            var hitViewModel = ((FrameworkElement)hitElement).DataContext;

            // TODO may allow view controlled element pick-and-draggable check from here

            var info = PickAndDragGroups[uiElement];
            var wasSelected = info.SelectedViewModels.Contains(hitViewModel);

            info.HitElement = hitElement as UIElement;
            info.HitViewModel = hitViewModel;

            if (GetIsPickableWithBox(uiElement) && info.PickAndDragHandler.IsBoxable(hitViewModel))
            {
                TryCreateBox(info, point);
            }
            else if (wasSelected && info.PickAndDragHandler.IsDraggable(hitViewModel))
            {
                TryDrag(uiElement, point, info);
            }
            else
            {
                TrySelect(info);
            }

            // can't finalise the event as other behavioral entities such as expand operator may be waiting on it
        }

        private static void PickAndDraggableOnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var info = PickAndDragGroups[uiElement];

            if (info.IsCreatingBox)
            {
                info.CurrentPoint = e.GetPosition(uiElement);
                UpdateBox(info);
                return;
            }

            if (info.DragState != PickAndDragInfo.DragStateEnum.BeingDragged 
                && info.DragState != PickAndDragInfo.DragStateEnum.Poisted)
                return;

            info.CurrentPoint = e.GetPosition(info.DragBase);

            if (info.DragState == PickAndDragInfo.DragStateEnum.Poisted)
            {
                var threshold = GetDragThreshold(info);
                var move = info.CurrentPoint - info.StartPoint;
                var distance = move.Length;
                if (distance >= threshold)
                {
                    info.DragState = PickAndDragInfo.DragStateEnum.BeingDragged;
                }
            }

            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                DoMouseLeftButtonUp(info);
                return;
            }

            if (info.DragState == PickAndDragInfo.DragStateEnum.Poisted)
            {
                return;
            }

            var dragHandler = info.PickAndDragHandler;
            dragHandler.DragMove();
        }

        private static void PickAndDraggableOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var info = PickAndDragGroups[uiElement];

            if (info.IsCreatingBox)
            {
                EndCreatingBox(info);
                return;
            }

            if (info.DragState != PickAndDragInfo.DragStateEnum.BeingDragged 
                && info.DragState != PickAndDragInfo.DragStateEnum.Poisted)
                return;

            info.CurrentPoint = e.GetPosition(info.DragBase);

            DoMouseLeftButtonUp(info);
        }

        private static void PickAndDraggableOnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var uiElement = (UIElement)sender;
            var point = e.GetPosition(uiElement);
            var hitElement = uiElement.InputHitTest(point);

            if (hitElement == null)
            {   // may happen like when a message box is clicked on
                return;
            }

            var hitViewModel = ((FrameworkElement)hitElement).DataContext;

            // TODO may allow view controlled element pick-and-draggable check from here

            var info = PickAndDragGroups[uiElement];
            var wasSelected = info.SelectedViewModels.Contains(hitViewModel);

            info.HitElement = hitElement as UIElement;
            info.HitViewModel = hitViewModel;

            // TODO make sure no other party is waiting on it
            e.Handled = true;   // stop passing the event

            if (wasSelected)
            {   // TODO may behave differently depending on the user preference, e.g. deselecting all other than the hit
                return;
            }

            foreach (var vm in info.SelectedViewModels)
            {
                info.OldViewModels.Add(vm);
            }
            info.SelectedViewModels.Clear();
            UpdateSelect(info);
        }

        #endregion
    }
}
