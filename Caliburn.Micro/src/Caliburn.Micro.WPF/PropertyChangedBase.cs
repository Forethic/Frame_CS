using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Caliburn.Micro
{
    /// <summary>
    /// Extends <see cref="INotifyPropertyChanged"/> such that the change event can be raised by external parties.
    /// </summary>
    public interface INotifyPropertyChangedEx : INotifyPropertyChanged
    {
        /// <summary>
        /// Enables/Disables property change notification.
        /// </summary>
        bool IsNotifying { get; set; }

        /// <summary>
        /// Notifies subscribers of the property change.
        /// </summary>
        /// <param name="propertyName"></param>
        void NotifyOfPropertyChange(string propertyName);

        /// <summary>
        /// Raises a change notification indicating that all bindings should be refreshed.
        /// </summary>
        void Refresh();
    }

    [Serializable]
    public class PropertyChangedBase : INotifyPropertyChangedEx
    {
        [field: NonSerialized]
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public PropertyChangedBase()
        {
            IsNotifying = true;
        }

        /// <summary>
        /// Enables/Disables proproperty change notification.
        /// </summary>
        public bool IsNotifying { get; set; }

        /// <summary>
        /// Notifies subscribers of the property change.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public void NotifyOfPropertyChange(string propertyName)
        {
            if (IsNotifying)
                Execute.OnUIThread(() => RaisePropertyChangedEventCore(propertyName));
        }

        /// <summary>
        /// Raises a change notification indicting that all bindings should be refreshed.
        /// </summary>
        public void Refresh()
        {
            NotifyOfPropertyChange(string.Empty);
        }

        /// <summary>
        /// Raises the property changed event immediately.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public virtual void RaisePropertyChangedEventImmediately(string propertyName)
        {
            if (IsNotifying)
                RaisePropertyChangedEventCore(propertyName);
        }

        /// <summary>
        /// Notifies subscribers of the property change.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="property">The property expression.</param>
        public virtual void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            NotifyOfPropertyChange(property.GetMemberInfo().Name);
        }

        void RaisePropertyChangedEventCore(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}