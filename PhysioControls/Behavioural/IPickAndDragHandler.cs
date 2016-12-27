namespace PhysioControls.Behavioural
{
    public interface IPickAndDragHandler
    {
        #region Properties

        PickAndDragInfo PickAndDragInfo { get; }

        #endregion


        #region Methods

        #region Dragging related

        bool IsDraggable(object candidateViewModel);
        bool IsBoxable(object candidateViewModel);
        double DragThreshold(object candidateViewModel);
        void DragBegin();
        void DragMove();
        void DragEnd();

        #endregion

        #region Selection related

        bool IsSelectable(object candidateViewModel);
        void UpdateSelect();

        #endregion

        #region Box related

        void BoxBegin();
        void BoxResize(bool updateBoxedObjectList);
        void BoxEnd(bool updateBoxedObjectList);

        #endregion

        #endregion
    }
}
