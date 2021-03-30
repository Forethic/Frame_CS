using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interactivity;

namespace Caliburn.Micro
{
    public class Parameter : Freezable, IAttachedObject
    {
        WeakReference owner;

        ActionMessage Owner
        {
            get => owner == null ? null : owner.Target as ActionMessage;
            set => owner = new WeakReference(Value);
        }

        #region IAttachedObject

        DependencyObject associateObject;
        DependencyObject IAttachedObject.AssociatedObject
        {
            get
            {
                ReadPreamble();
                return associateObject;
            }
        }

        void IAttachedObject.Attach(DependencyObject dependencyObject)
        {
            WritePreamble();
            associateObject = dependencyObject;
            WritePostscript();
        }

        void IAttachedObject.Detach()
        {
            WritePreamble();
            associateObject = null;
            WritePostscript();
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new Parameter();
        }

        #region Value

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        [Category("Common Properties")]
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(Parameter), new PropertyMetadata(OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var parameter = (Parameter)d;

            if (parameter.Owner != null)
                parameter.Owner.UpdateAvailability();
        }

        #endregion

        /// <summary>
        /// Makes the parameter aware of the <see cref="ActionMessage"/> that it's attached to.
        /// </summary>
        /// <param name="owner">The action message.</param>
        internal void MakeAwareOf(ActionMessage owner)
        {
            Owner = owner;
        }
    }
}
