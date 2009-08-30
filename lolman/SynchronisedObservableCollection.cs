using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace LanOfLegends.lolman
{
    public class SynchronisedObservableCollection<T> : ICollection<T>,
            INotifyCollectionChanged, INotifyPropertyChanged
    {
        private ICollection<T> collection;
        private Dispatcher dispatcher;

        public SynchronisedObservableCollection(ICollection<T> collection)
        {
            if (collection == null ||
                collection as INotifyCollectionChanged == null ||
                collection as INotifyPropertyChanged == null)
            {
                throw new ArgumentException("Collection must support ICollection, INotifyCollectionChanged " +
                    " and INotifyPropertyChanged interfaces.");
            }

            this.collection = collection;
            this.dispatcher = Dispatcher.CurrentDispatcher;

            INotifyCollectionChanged collectionChanged = collection as INotifyCollectionChanged;
            collectionChanged.CollectionChanged += delegate(Object sender, NotifyCollectionChangedEventArgs e)
            {
                dispatcher.Invoke(DispatcherPriority.Normal,
                    new RaiseCollectionChangedEventHandler(RaiseCollectionChangedEvent), e);
            };

            INotifyPropertyChanged propertyChanged = collection as INotifyPropertyChanged;
            propertyChanged.PropertyChanged += delegate(Object sender, PropertyChangedEventArgs e)
            {
                dispatcher.Invoke(DispatcherPriority.Normal,
                    new RaisePropertyChangedEventHandler(RaisePropertyChangedEvent), e);
            };
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        private delegate void RaiseCollectionChangedEventHandler(NotifyCollectionChangedEventArgs e);

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        private delegate void RaisePropertyChangedEventHandler(PropertyChangedEventArgs e);

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            collection.Add(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public bool Contains(T item)
        {
            return collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return collection.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return collection.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)collection).GetEnumerator();
        }

        #endregion
    }
}
