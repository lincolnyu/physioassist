namespace PhysioControls.ChangeTracking
{
    public interface IPropertyChange
    {
        #region Methods

        void Redo();
        void Undo();

        #endregion
    }
}
