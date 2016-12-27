using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PhysioControls.Collections
{
    public class EnhancedObservableCollection<T> : ObservableCollection<T>
    {

        #region Events

        public delegate void CollectionClearingEvent(EnhancedObservableCollection<T> sender);

        public event CollectionClearingEvent CollectionClearing;

        #endregion

        #region Methods

        protected override void ClearItems()
        {
            var list = new List<T>();
            list.AddRange(this);

            if (CollectionClearing != null)
            {
                CollectionClearing(this);
            }
            
            base.ClearItems();
        }

        #endregion
    }
}
