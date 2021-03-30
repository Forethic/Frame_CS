using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Caliburn.Micro
{
    /// <summary>
    /// The context used during the execution of an Action or its guard
    /// </summary>
    public class ActionExecutionContext : IDisposable
    {
        /// <summary>
        /// Determines whether the action can execute.
        /// </summary>
        /// <remarks>Returns true if the action can execute, false otherwise.</remarks>
        public Func<bool> CanExecute;

        /// <summary>
        /// Any event arguments associated with the action's invocation.
        /// </summary>
        public object EventArgs;

        /// <summary>
        /// The actual method info to be invoked.
        /// </summary>
        public MethodInfo Method;

        /// <summary>
        /// The message being executed.
        /// </summary>
        public ActionMessage Message
        {
            get => message == null ? null : message.Target as ActionMessage;
            set => message = new WeakReference(value);
        }

        /// <summary>
        /// The source from which the message originates.
        /// </summary>
        public FrameworkElement Source
        {
            get => source == null ? null : source.Target as FrameworkElement;
            set => source = new WeakReference(value);
        }

        /// <summary>
        /// The instance on which the action is invoked.
        /// </summary>
        public object Target
        {
            get => target == null ? null : target.Target;
            set => target = new WeakReference(value);
        }

        /// <summary>
        /// The view associated with the target.
        /// </summary>
        public DependencyObject View
        {
            get => view == null ? null : view.Target as DependencyObject;
            set => view = new WeakReference(value);
        }


        WeakReference message;
        WeakReference source;
        WeakReference target;
        WeakReference view;
        Dictionary<string, object> values;

        /// <summary>
        /// Gets or sets additional data needed to invoke the action.
        /// </summary>
        /// <param name="key">The data key.</param>
        /// <returns>Custom data associated with the context.</returns>
        public object this[string key]
        {
            get
            {
                if (values == null)
                    values = new Dictionary<string, object>();
                return values[key];
            }
            set
            {
                if (values == null)
                    values = new Dictionary<string, object>();

                values[key] = value;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Disposing(this, System.EventArgs.Empty);
        }

        /// <summary>
        /// Called when the execution context is disposed
        /// </summary>
        public event EventHandler Disposing = delegate { };
    }
}
