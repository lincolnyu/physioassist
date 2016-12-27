using System.Reflection;

namespace PhysioControls.ChangeTracking
{
    public class PropertyChange : IPropertyChange
    {
        #region Properties

        public object Owner { get; set; }
        public PropertyInfo Property { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }

        #endregion

        #region Methods

        #region IPropertyChange Members

        public void Redo()
        {
            Property.SetValue(Owner, NewValue, null);
        }

        public void Undo()
        {
            Property.SetValue(Owner, OldValue, null);
        }

        #endregion

        #endregion
    }
}
