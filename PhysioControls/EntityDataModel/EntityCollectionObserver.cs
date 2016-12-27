using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Objects.DataClasses;
using System.Linq;

namespace PhysioControls.EntityDataModel
{
    /// <summary>
    ///  collection observer that's hooked up to a DB collection
    /// </summary>
    /// <typeparam name="T">Type of the entities in the collection</typeparam>
    /// <remark>
    ///  References:
    ///  http://stackoverflow.com/questions/5502698/entityframework-entitycollection-observing-collectionchanged
    /// </remark>
    public class EntityCollectionObserver<T> : ObservableCollection<T> where T : class
    {
        private static readonly List<Tuple<IBindingList, EntityCollection<T>, EntityCollectionObserver<T>>> InnerLists
            = new List<Tuple<IBindingList, EntityCollection<T>, EntityCollectionObserver<T>>>();

        public EntityCollectionObserver(EntityCollection<T> entityCollection)
            : base(entityCollection)
        {
            var l = ((IBindingList)((IListSource)entityCollection).GetList());
            l.ListChanged += OnInnerListChanged;


            foreach (var x in InnerLists.Where(x => x.Item2 == entityCollection && x.Item1 != l))
            {
                x.Item3.ObserveThisListAswell(x.Item1);
            }
            InnerLists.Add(new Tuple<IBindingList, EntityCollection<T>, EntityCollectionObserver<T>>(l, entityCollection, this));
        }

        private void ObserveThisListAswell(IBindingList l)
        {
            l.ListChanged += OnInnerListChanged;
        }

        private void OnInnerListChanged(object sender, ListChangedEventArgs e)
        {
            // TODO review if we can make use of finer grained change types
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

}
