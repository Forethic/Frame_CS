using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Markup;
using System.Linq;
using EventTrigger = System.Windows.Interactivity.EventTrigger;
using TriggerBase = System.Windows.Interactivity.TriggerBase;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Data;

namespace Caliburn.Micro
{
    /// <summary>
    /// Used to send a message from the UI to a presentation model class, indicating that a particular Action should be invoked. 
    /// </summary>
    [DefaultTrigger(typeof(FrameworkElement), typeof(EventTrigger), "MouseLeftButtonDown")]
    [DefaultTrigger(typeof(ButtonBase), typeof(EventTrigger), "Click")]
    [ContentProperty("Parameters")]
    [TypeConstraint(typeof(FrameworkElement))]
    public class ActionMessage : TriggerAction<FrameworkElement>, IHaveParameters
    {
        static readonly ILog Log = LogManager.GetLog(typeof(ActionMessage));

        /// <summary>
        /// Causes the action invocation to "double check" if the action should be invoked by executing the guard immediately before hand.
        /// <remarks>This is diabled by default. If multiple actions are attached to the same element, you may want to enable this so that each individaul action checks its guard reardless of how the UI state appears </remarks>
        /// </summary>
        public static bool EnforceGuardsDuringInvocation = false;

        /// <summary>
        /// Occurs before the message detaches from the associated object.
        /// </summary>
        public event EventHandler Detaching = delegate { };

        ActionExecutionContext context;

        #region DependencyProperty

        #region Handler

        public static object GetHandler(DependencyObject obj) => (object)obj.GetValue(HandlerProperty);
        public static void SetHandler(DependencyObject obj, object value) => obj.SetValue(HandlerProperty, value);
        public static DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached("Handler", typeof(object), typeof(ActionMessage), new PropertyMetadata(HandlerPropertyChanged));

        private static void HandlerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ActionMessage)d).UpdateContext();
        }

        void UpdateContext()
        {
            if (context != null)
                context.Dispose();

            context = new ActionExecutionContext
            {
                Message = this,
                Source = AssociatedObject
            };

            PrepareContext(context);
            UpdateAvailabilityCore();
        }

        /// <summary>
        /// Prepares the action executtion context for use.
        /// </summary>
        public static Action<ActionExecutionContext> PrepareContext =
            context =>
            {
                SetMethodBinding(context);
                if (context.Target == null || context.Method == null)
                    return;

                var guardName = "Can" + context.Method.Name;
                var targetType = context.Target.GetType();
                var guard = TryFindGuardMethod(context);

                if (guard == null)
                {
                    if (context.Target is INotifyPropertyChanged inpc)
                    {
                        guard = targetType.GetMethod("get_" + guardName);
                        if (guard == null) return;

                        PropertyChangedEventHandler handler = (s, e) =>
                        {
                            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == guardName)
                                context.Message.UpdateAvailability();
                        };

                        inpc.PropertyChanged += handler;
                        context.Disposing += delegate { inpc.PropertyChanged -= handler; };
                        context.Message.Detaching += delegate { inpc.PropertyChanged -= handler; };
                    }
                }

                context.CanExecute = () => (bool)guard.Invoke(context.Target,
                    MessageBinder.DetermineParameter(context, guard.GetParameters()));
            };

        /// <summary>
        /// Sets the target, method and view on the context. Uses a bubbling strategy by default.
        /// </summary>
        public static Action<ActionExecutionContext> SetMethodBinding =
            context =>
            {
                DependencyObject currentElemnt = context.Source;

                while (currentElemnt != null)
                {
                    if (Action.HasTargetSet(currentElemnt))
                    {
                        var target = Message.GetHandler(currentElemnt);
                        if (target != null)
                        {
                            var method = GetTargetMethod(context.Message, target);
                            if (method != null)
                            {
                                context.Method = method;
                                context.Target = target;
                                context.View = currentElemnt;
                                return;
                            }
                        }
                        else
                        {
                            context.View = currentElemnt;
                            return;
                        }
                    }

                    currentElemnt = VisualTreeHelper.GetParent(currentElemnt);
                }

                if (context.Source.DataContext != null)
                {
                    var target = context.Source.DataContext;
                    var method = GetTargetMethod(context.Message, target);

                    if (method != null)
                    {
                        context.Target = target;
                        context.Method = method;
                        context.View = context.Source;
                    }
                }
            };

        /// <summary>
        /// Finds the method on the target matching the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="target">The target.</param>
        /// <returns>The matching method, if available.</returns>
        public static Func<ActionMessage, object, MethodInfo> GetTargetMethod =
            (message, target) =>
            {
                return (from method in target.GetType().GetMethods()
                        where method.Name == message.MethodName
                        let methodParameters = method.GetParameters()
                        where message.Parameters.Count == methodParameters.Length
                        select method).FirstOrDefault();
            };

        /// <summary>
        /// Try to find a condidate for guard function, having:
        ///     - a name in the form "CanXXX"
        ///     - no generic parameters
        ///     - a bool return type
        ///     - no parameters or a set of parameters corresponding to the action method
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>A MethodInfo, if found; null otherwise.</returns>
        private static MethodInfo TryFindGuardMethod(ActionExecutionContext context)
        {
            var guardName = "Can" + context.Method.Name;
            var targetType = context.Target.GetType();
            var guard = targetType.GetMethod(guardName);

            if (guard == null) return null;
            if (guard.ContainsGenericParameters) return null;
            if (!typeof(bool).Equals(guard.ReturnType)) return null;

            var guardPars = guard.GetParameters();
            var actionPars = context.Method.GetParameters();
            if (guardPars.Length == 0) return guard;
            if (guardPars.Length != actionPars.Length) return null;

            var comparisons = guardPars.Zip(context.Method.GetParameters(), (x, y) => x.ParameterType.Equals(y.ParameterType));
            if (comparisons.Any(x => !x)) return null;

            return guard;
        }

        /// <summary>
        /// Forces an update of the UI's Enabled/Disabled state based on the preconditions associated with the method.
        /// </summary>
        public void UpdateAvailability()
        {
            if (context == null) return;

            if (context.Target == null || context.View == null)
                PrepareContext(context);

            UpdateAvailabilityCore();
        }

        bool UpdateAvailabilityCore()
        {
            Log.Info($"{this} availability update.");
            return ApplyAvailabilityEffect(context);
        }

        /// <summary>
        /// Applies an availability effect, such as IsEnabled, to an element.
        /// </summary>
        /// <remarks>Returns a value indicating whether or not the action is available.</remarks>
        public static Func<ActionExecutionContext, bool> ApplyAvailabilityEffect =
            context =>
            {
                var source = context.Source;
                if (context.CanExecute != null)
                    source.IsEnabled = context.CanExecute();
                return source.IsEnabled;
            };

        #endregion

        #region MethodName

        /// <summary>
        /// Gets or sets the name of the method to be invoked on the presentation model class.
        /// </summary>
        /// <value>The name of the method.</value>
        [Category("Common Properties")]
        public string MethodName
        {
            get => (string)GetValue(MethodNameProperty);
            set => SetValue(MethodNameProperty, value);
        }

        /// <summary>
        /// Represents the method name of an action message.
        /// </summary>
        public static DependencyProperty MethodNameProperty = DependencyProperty.Register("MethodName", typeof(string), typeof(ActionMessage));

        #endregion

        #region Parameters

        /// <summary>
        /// Gets the parameters to pass as part of the method invocation.
        /// </summary>
        [Category("Common Properties")]
        public AttachedCollection<Parameter> Parameters
        {
            get => (AttachedCollection<Parameter>)GetValue(ParametersProperty);
        }

        /// <summary>
        /// Represents the parameters of an action message.
        /// </summary>
        public static DependencyProperty ParametersProperty = DependencyProperty.Register("Parameters", typeof(AttachedCollection<Parameter>), typeof(ActionMessage));

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an instance of <see cref="ActionMessage"/>.
        /// </summary>
        public ActionMessage()
        {
            SetValue(ParametersProperty, new AttachedCollection<Parameter>());
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Invokes the action using the specified <see cref="ActionExecutionContext"/>.
        /// </summary>
        public static Action<ActionExecutionContext> InvokeAction =
            context =>
            {
                var values = MessageBinder.DetermineParameter(context, context.Method.GetParameters());
                var returnValue = context.Method.Invoke(context.Target, values);

                if (returnValue is IResult)
                    returnValue = new[] { returnValue as IResult };

                if (returnValue is IEnumerable<IResult> resultEnumerable)
                    Coroutine.Execute(resultEnumerable.GetEnumerator(), context);
                else if (returnValue is IEnumerator<IResult> resultEnumerator)
                    Coroutine.Execute(resultEnumerator, context);
            };

        protected override void Invoke(object eventArgs)
        {
            Log.Info($"Invoking {this}.");

            if (context.Target == null || context.View == null)
            {
                PrepareContext(context);
                if (context.Target == null)
                {
                    var ex = new Exception($"No target found for method {context.Message}");
                    Log.Error(ex);
                    throw ex;
                }
                if (!UpdateAvailabilityCore())
                    return;
            }

            if (context.Method == null)
            {
                var ex = new Exception($"Method {context.Message.MethodName} not found on target of type {context.Target.GetType()}");
                Log.Error(ex);
                throw ex;
            }

            context.EventArgs = eventArgs;
            InvokeAction(context);
        }

        protected override void OnAttached()
        {
            if (!Bootstraper.IsInDesignMode)
            {
                Parameters.Attach(AssociatedObject);
                Parameters.Apply(p => p.MakeAwareOf(this));

                if ((bool)AssociatedObject.GetValue(View.IsLoadedProperty))
                {
                    ElementLoaded(null, null);

                    var trigger = Interaction.GetTriggers(AssociatedObject).FirstOrDefault(t => t.Actions.Contains(this)) as EventTrigger;
                    if (trigger != null && trigger.EventName == "Loaded")
                        Invoke(new RoutedEventArgs());
                }
                else
                {
                    AssociatedObject.Loaded += ElementLoaded;
                }
            }

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            if (!Bootstraper.IsInDesignMode)
            {
                Detaching(this, EventArgs.Empty);
                AssociatedObject.Loaded -= ElementLoaded;
                Parameters.Detach();
            }

            base.OnDetaching();
        }

        void ElementLoaded(object sender, RoutedEventArgs e)
        {
            UpdateContext();

            DependencyObject currentElement;
            if (context.View == null)
            {
                currentElement = AssociatedObject;
                while (currentElement != null)
                {
                    if (Action.HasTargetSet(currentElement))
                        break;

                    currentElement = VisualTreeHelper.GetParent(currentElement);
                }
            }
            else
            {
                currentElement = context.View;
            }

            var binding = new Binding
            {
                Path = new PropertyPath(Message.HandlerProperty),
                Source = currentElement,
            };

            BindingOperations.SetBinding(this, HandlerProperty, binding);
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="object"/>
        /// </summary>
        /// <returns>
        /// a <see cref="string"/> that represents the current <see cref="object"/>.
        /// </returns>
        public override string ToString()
        {
            return "Action: " + MethodName;
        }
    }
}