using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using PhysioControls.Collections;

namespace PhysioControls.ChangeTracking
{
    public class Changeset : IDisposable
    {
        #region Properties

        // It doens't have to be an observable collection
        public IList<IPropertyChange> Changes
        {
            get { return _changes ?? (_changes = new List<IPropertyChange>()); }
        }

        public string Description { get; private set; }

        #endregion

        #region Constructors

        public Changeset(string description)
        {
            Description = description;
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            // do nothing
        }

        #endregion

        public void TrackPropertyChangeBegin(object owner, object targetValue)
        {
            if (_undoredoing) return;
            
            if (_trackingDepth++ > 0)
            {
                return;
            }

            var type = owner.GetType();
            PropertyInfo property = null;
            var stackTrace = new StackTrace();
            for (var i = 1; property == null && i < stackTrace.FrameCount; i++)
            {
                var methodName = stackTrace.GetFrame(i).GetMethod().Name;
                if (methodName.Length < 4 || methodName.Substring(0, 4) != "set_") continue;
                var propertyName = methodName.Substring(4);   // must be prefixed by "set_"
                property = type.GetProperty(propertyName);
            }
            
            if (property == null)
            {
                _trackingDepth--;
                return;
            }

            var oldValue = property.GetValue(owner, null);

            Trace.WriteLine(String.Format("Change '{0}' to '{1}'", oldValue!=null?oldValue.ToString():"null",
                targetValue!=null?targetValue.ToString():"null"));

            AddChange(new PropertyChange
            {
                Owner = owner,
                Property = property,
                NewValue = targetValue,
                OldValue = oldValue
            });
        }

        public void TrackPropertyChangeEnd()
        {
            if (_trackingDepth > 0)
            {
                _trackingDepth--;
            }
        }

        public void OnCollectionChanged<T>(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_undoredoing) return;

            var change = new CollectionChange<T>
            {
                Collection = (ICollection<T>)sender,
                Action = e.Action,
                NewItems = e.NewItems,
                OldItems = e.OldItems,
                NewStartingIndex = e.NewStartingIndex,
                OldStartingIndex = e.OldStartingIndex
            };

            AddChange(change);
        }

        public void OnCollectionClearing<T>(EnhancedObservableCollection<T> collection)
        {
            if (_undoredoing) return;

            var list = new List<T>();
            list.AddRange(collection);
            var change = new CollectionChange<T>
            {
                Collection = collection,
                Action = NotifyCollectionChangedAction.Reset,
                NewItems = null,
                OldItems = list,
                NewStartingIndex = -1,
                OldStartingIndex = -1
            };

            AddChange(change);
        }

        protected void AddChange(IPropertyChange change)
        {
            Changes.Add(change);
        }

        public void Undo()
        {
            _undoredoing = true;
            for (var i = Changes.Count - 1; i >= 0; i--)
            {
                var change = Changes[i];
                change.Undo();
            }
            _undoredoing = false;
        }

        public void Redo()
        {
            _undoredoing = true;
            foreach (var change in Changes)
            {
                change.Redo();
            }
            _undoredoing = false;
        }

        #endregion

        #region Fields

        private List<IPropertyChange> _changes;

        private bool _undoredoing;

        private int _trackingDepth;

        #endregion
    }
}
