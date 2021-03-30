using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Caliburn.Micro
{
    public static class View
    {
        #region IsLoaded

        /// <summary>
        /// A dependency property which allow the framework to track whether a certain element has already been loaded in certain scenarios.
        /// </summary>
        public static DependencyProperty IsLoadedProperty = DependencyProperty.RegisterAttached("IsLoaded", typeof(bool), typeof(View), new PropertyMetadata(false));

        #endregion

        #region IsScopeRoot

        /// <summary>
        /// A dependency property which marks an element as a name scope root.
        /// </summary>
        public static DependencyProperty IsScopeRootProperty = DependencyProperty.RegisterAttached("IsScopeRoot", typeof(bool), typeof(View), new PropertyMetadata(false));

        #endregion

        #region ApplyConventions

        /// <summary>
        /// Gets the convention appliction behavior.
        /// </summary>
        /// <param name="d">The element the property is attached to.</param>
        /// <returns>Whether or not to apply conventions.</returns>
        public static bool? GetApplyConventions(DependencyObject d) => (bool?)d.GetValue(ApplyConventionsProperty);

        /// <summary>
        /// Sets the convention application behavior.
        /// </summary>
        /// <param name="d">The element to attach the property to.</param>
        /// <param name="value">Whether or not to apply conventions.</param>
        public static void SetApplyConventions(DependencyObject d, bool? value) => d.SetValue(ApplyConventionsProperty, value);

        /// <summary>
        /// A dependency property which allows the override of convention application behavior.
        /// </summary>
        public static DependencyProperty ApplyConventionsProperty = DependencyProperty.RegisterAttached("ApplyConventions", typeof(bool?), typeof(View));

        #endregion

        #region Model

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <param name="d">The element the model is attached to.</param>
        /// <returns>The model.</returns>
        public static object GetModel(DependencyObject d) => (object)d.GetValue(ModelProperty);

        /// <summary>
        /// Sets the model.
        /// </summary>
        /// <param name="d">The element to attach the model to.</param>
        /// <param name="value">The model.</param>
        public static void SetModel(DependencyObject d, object value) => d.SetValue(ModelProperty, value);

        /// <summary>
        /// A dependency property for attaching a model to the UI.
        /// </summary>
        public static DependencyProperty ModelProperty = DependencyProperty.RegisterAttached("Model", typeof(object), typeof(View), new PropertyMetadata(OnModelChanged));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;

            if (e.NewValue != null)
            {
                var context = GetContext(d);
                var view = ViewLocator.LocateForModel(e.NewValue, d, context);

                ViewModelBinder.Bind(e.NewValue, view, context);
                SetContentProperty(d, view);
            }
            else
            {
                SetContentProperty(d, e.NewValue);
            }
        }

        private static void SetContentProperty(object targetLocation, object view)
        {
            if (view is FrameworkElement element && element.Parent != null)
                SetContentProperty(element.Parent, null);

            SetContentPropertyCore(targetLocation, view);
        }

        private static void SetContentPropertyCore(object targetLocation, object view)
        {
            var type = targetLocation.GetType();
            var contentProperty = type.GetAttributes<ContentPropertyAttribute>(true)
                .FirstOrDefault() ?? new ContentPropertyAttribute("Content");

            type.GetProperty(contentProperty.Name)
                .SetValue(targetLocation, view, null);
        }

        #endregion

        #region Context

        public static object GetContext(DependencyObject obj) => (object)obj.GetValue(ContextProperty);
        public static void SetContext(DependencyObject obj, object value) => obj.SetValue(ContextProperty, value);

        /// <summary>
        /// A dependency property for assigning a context to a particular portion of the UI.
        /// </summary>
        public static DependencyProperty ContextProperty = DependencyProperty.RegisterAttached("Context", typeof(object), typeof(View), new PropertyMetadata(OnContextChanged));

        private static void OnContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {

        }

        #endregion

        #region IsGeneratedProperty

        /// <summary>
        /// Used by framework to indicate that this element was generated.
        /// </summary>
        public static DependencyProperty IsGeneratedPropertyProperty = DependencyProperty.RegisterAttached("IsGeneratedProperty", typeof(bool), typeof(View), new PropertyMetadata(false));

        #endregion

        /// <summary>
        /// Used to retrieve the root, non-framework-created view.
        /// </summary>
        /// <param name="view">The view to search.</param>
        /// <returns>The root element that was not created by the framework.</returns>
        /// <remarks>In certain instances the services create UI elements.
        /// For example, if you ask the window manager to show a UserControl as a dialog, it creates a window to host the UserControl in.
        /// The WindowManager marks that element as a framework-created element so that it can determine what it creatd vs. what was intended by the developer.
        /// Calling GetFirstNonGeneratedView allows the framework to discover what the original element was.
        /// </remarks>
        public static Func<DependencyObject, DependencyObject> GetFirstNonGeneratedView =
            view =>
            {
                if ((bool)view.GetValue(IsGeneratedPropertyProperty))
                {
                    if (view is ContentControl content)
                        return (DependencyObject)content.Content;

                    var type = view.GetType();
                    var contentProperty = type.GetAttributes<ContentPropertyAttribute>(true).FirstOrDefault() ?? new ContentPropertyAttribute("Content");

                    return (DependencyObject)type.GetProperty(contentProperty.Name).GetValue(view, null);
                }

                return view;
            };
    }
}
