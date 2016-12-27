using System.Collections.Generic;
using System.Windows;

namespace PhysioControls.Behavioural
{
    public class PickAndDragInfo
    {
        #region Enumeration

        public enum DragStateEnum
        {
            Undragged,
            Poisted,
            BeingDragged,
            DragDone,
            DragFailed,
        }

        #endregion

        #region Events

        public class HitViewModelSetEventArgs
        {
            public object OldValue { get; internal set; }
            public object NewValue { get; internal set; }
        }

        public delegate void HitViewModelSetEvent(HitViewModelSetEventArgs e);

        public event HitViewModelSetEvent HitViewModelSet;

        #endregion

        #region Properties

        #region Common

        public IPickAndDragHandler PickAndDragHandler { get; private set; }

        public object HitViewModel 
        {
            get { return _hitElement; }
            set
            {
                OnHitViewModelSet(new HitViewModelSetEventArgs() {OldValue = _hitElement, NewValue = value});
                if (value == _hitElement) return;
                _hitElement = value;
            }
        }
        public UIElement HitElement { get; set; }

        #endregion

        #region Dragging information

        public DragStateEnum DragState { get; set; }
        public Point StartPoint { get; set; }
        public Point CurrentPoint { get; set; }
        public UIElement DragBase { get; set; }

        public double Distance
        {
            get { return (CurrentPoint-StartPoint).Length; }
        }

        #endregion

        #region Box information

        public bool IsCreatingBox { get; set; }
        public Point BoxStart { get; set; }
        public Rect OldBox { get; set; }
        public Rect Box { get; set; }

        // handler provided
        public IList<object> OldBoxedObjects { get; set; }
        public IList<object> NewBoxedObjects { get; set; }
        public IList<object> BoxedObjects { get; set; } 

        #endregion

        #endregion

        #region Constructors

        public PickAndDragInfo(IPickAndDragHandler handler)
        {
            PickAndDragHandler = handler;
        }

        #endregion

        #region Methods

        public void Select(object select)
        {
            SelectedViewModels.Add(select);
        }

        public void Deselect(object select)
        {
            SelectedViewModels.Remove(select);
        }

        public void SetSelect(IEnumerable<object> toSelect)
        {
            SelectedViewModels.Clear();
            if (toSelect == null) return;
            foreach (var s in toSelect)
            {
                SelectedViewModels.Add(s);
            }
        }

        void OnHitViewModelSet(HitViewModelSetEventArgs e)
        {
            if (HitViewModelSet != null)
            {
                HitViewModelSet(e);
            }
        }

        #endregion

        #region Fields

        #region Selection information

        public readonly ISet<object> SelectedViewModels = new HashSet<object>();

        public readonly IList<object> NewViewModels = new List<object>();
        public readonly IList<object> OldViewModels = new List<object>();

        private object _hitElement;

        #endregion

        #endregion
    }
}
