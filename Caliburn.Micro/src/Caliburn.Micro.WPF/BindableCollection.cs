using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caliburn.Micro
{
    /// <summary>
    /// Represents a collection that is observable.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the collection.</typeparam>
    public interface IObservaleCollection<T> : IList<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="items">The items.</param>
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Removes the range.
        /// </summary>
        /// <param name="items">The items.</param>
        void RemoveRange(IEnumerable<T> items);
    }

    public class BindableCollection<T> : ObservableCollection<T>, IObservaleCollection<T>
    {
        public bool IsNotifying { get; set; }

        #region Constructor

        public BindableCollection()
        {
            IsNotifying = true;
        }

        public BindableCollection(IEnumerable<T> collection)
        {
            IsNotifying = true;
            AddRange(collection);
        }

        #endregion

        /// <summary>
        /// Notifies subscribers of the property change.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public void NotifyPropertyChange(string propertyName)
        {
            if (IsNotifying)
                Execute.OnUIThread(() => RaisePropertyChangedEventImmediately(new PropertyChangedEventArgs(propertyName)));
        }

        /// <summary>
        /// Raises the <see cref="ObservableCollection{T}.CollectionChanged"/> event with the provide arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (IsNotifying) base.OnCollectionChanged(e);
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event with the provided arguments.
        /// </summary>
        /// <param name="e">The event data to report in the event.</param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (IsNotifying) base.OnPropertyChanged(e);
        }
        void RaisePropertyChangedEventImmediately(PropertyChangedEventArgs e) => OnPropertyChanged(e);

        /// <summary>
        /// Raises a change notification indicating that all bindings should be refreshed.
        /// </summary>
        public void Refresh()
        {
            Execute.OnUIThread(() =>
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
            });
        }

        /// <summary>
        /// Inserts the item to the specified position.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="item">The item to be inserts.</param>
        protected override void InsertItem(int index, T item) => Execute.OnUIThread(() => InsertItemBase(index, item));

        /// <summary>
        /// Exposes the base implementation of the <see cref="InsertItem"/> function.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <remarks>Used to avoid compiler warning regarding unverificable code.</remarks>
        void InsertItemBase(int index, T item) => base.InsertItem(index, item);

        /// <summary>
        /// Moves the item within the collection.
        /// </summary>
        /// <param name="oldIndex">The old position of the item.</param>
        /// <param name="newIndex">The new position of the item.</param>
        protected override void MoveItem(int oldIndex, int newIndex) => Execute.OnUIThread(() => MoveItemBase(oldIndex, newIndex));

        /// <summary>
        /// Exposes the base implementation of the <see cref="MoveItem"/> function.
        /// </summary>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        /// <remarks>Used to avoid compiler warning regarding unverificable code.</remarks>
        void MoveItemBase(int oldIndex, int newIndex) => base.MoveItem(oldIndex, newIndex);

        /// <summary>
        /// Sets the item at the specified position.
        /// </summary>
        /// <param name="index">The index to set the item at.</param>
        /// <param name="item">The item to set.</param>
        protected override void SetItem(int index, T item) => Execute.OnUIThread(() => SetItemBase(index, item));

        /// <summary>
        /// Exposes the base implementation of the <see cref="SetItem"/> function.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <remarks>Used to avoid compiler warning regarding unverificable code.</remarks>
        void SetItemBase(int index, T item) => base.SetItem(index, item);

        /// <summary>
        /// Removes the item at the specified position.
        /// </summary>
        /// <param name="index">The position used to identify the item to remove.</param>
        protected override void RemoveItem(int index) => Execute.OnUIThread(() => RemoveItem(index));

        /// <summary>
        /// Exposes the base implemetation of the <see cref="RemoveItem"/> function.
        /// </summary>
        /// <remarks>Used to avoid compiler warning regarding unverifiable code.</remarks>
        void RemoveItemBase(int index) => base.RemoveItem(index);

        /// <summary>
        /// Clears the items contained by the collection.
        /// </summary>
        protected override void ClearItems() => ClearItemsBase();

        /// <summary>
        /// Exposes the base implemetation of the <see cref="ClearItems"/> function.
        /// </summary>
        /// <remarks>Used to avoid compiler warning regarding unverifiable code.</remarks>
        void ClearItemsBase() => base.ClearItems();

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="items">The items.</param>
        public void AddRange(IEnumerable<T> items)
        {
            Execute.OnUIThread(() =>
            {
                IsNotifying = false;

                var index = Count;
                foreach (var item in items)
                {
                    InsertItemBase(index, item);
                    index++;
                }
                IsNotifying = true;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
            });
        }

        /// <summary>
        /// Removes the range.
        /// </summary>
        /// <param name="items">The items.</param>
        public void RemoveRange(IEnumerable<T> items)
        {
            Execute.OnUIThread(() =>
            {
                IsNotifying = false;
                foreach (var item in items)
                {
                    var index = IndexOf(item);
                    RemoveItemBase(index);
                }
                IsNotifying = true;

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
                OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
            });
        }
    }
}