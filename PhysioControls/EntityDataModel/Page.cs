using System.Collections.Generic;
using System.Collections.Specialized;

namespace PhysioControls.EntityDataModel
{
    public partial class Page
    {
        #region Properties

        public int DataObjectCount
        {
            get { return DataObjects.Count; }
        }

        #endregion

        #region Constructors

        public Page()
        {
            _dataObjectsObserver = new EntityCollectionObserver<DataObject>(DataObjects);
            _dataObjectsObserver.CollectionChanged += DataObjectsCollectionChanged;
        }

        #endregion

        #region Methods

        public void DataObjectsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // TODO review if it's make it reentrant
            // TODO also review why the caller may interrupt this event handler/add objects with duplicate keys
            lock(this)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (DataObject dataObject in e.NewItems)
                        {
                            _dataObjectsDict[dataObject.Name] = dataObject;
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (DataObject dataObject in e.OldItems)
                        {
                            if (_dataObjectsDict.ContainsKey(dataObject.Name))
                            {
                                _dataObjectsDict.Remove(dataObject.Name);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        // TODO review this and make sure it works as expected
                        _dataObjectsDict.Clear();
                        foreach (var dataObject in DataObjects)
                        {
                            _dataObjectsDict[dataObject.Name] = dataObject; // use assignment so the new one always takes the place
                        }
                        break;
                }
            }
        }

        public void AddDataObject(DataObject dataObject)
        {
            DataObjects.Add(dataObject);
            // NOTE _dataObjectsDict is to be updated internally
        }

        public void RemoveDataObject(DataObject dataObject)
        {
            DataObjects.Remove(dataObject);
            // NOTE _dataObjectsDict is to be updated internally
        }

        public void ClearDataObjects()
        {
            DataObjects.Clear();
            //_dataObjectsDict.Clear();
        }

        public bool ContainsDataObject(DataObject dataObject)
        {
            return _dataObjectsDict.ContainsKey(dataObject.Name);
        }

        public bool ContainsDataObjectName(string name)
        {
            return _dataObjectsDict.ContainsKey(name);
        }

        #endregion

        #region Fields

        private readonly EntityCollectionObserver<DataObject> _dataObjectsObserver;
        private readonly IDictionary<string, DataObject> _dataObjectsDict = new Dictionary<string, DataObject>();

        #endregion
    }
}
