using System;

namespace PhysioControls.ChangeTracking
{
    public class PropertyChangeMarker : IDisposable
    {
        #region Constructors

        public PropertyChangeMarker(object owner, object targetValue)
        {
            ChangesetManager.Instance.TrackPropertyChangeBegin(owner, targetValue);
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            ChangesetManager.Instance.TrackPropertyChangeEnd();
        }

        #endregion

        #endregion
    }
}
