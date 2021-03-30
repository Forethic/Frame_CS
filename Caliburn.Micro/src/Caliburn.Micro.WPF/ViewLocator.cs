using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Caliburn.Micro
{
    /// <summary>
    /// A strategy for determining which view to use for a given model.
    /// </summary>
    public class ViewLocator
    {
        static readonly ILog Log = LogManager.GetLog(typeof(ViewLocator));

        /// <summary>
        /// Locates the view for the specified model instance.
        /// </summary>
        /// <returns>The view.</returns>
        /// <remarks>Pass the model instance, display location (or null) and the context (or null) as parameters and receive a view instance.</remarks>
        public static Func<object, DependencyObject, object, UIElement> LocateForModel =
            (model, displayLocation, context) =>
            {
                if (model is IViewAware viewAware)
                {
                    if (viewAware.GetView(context) is UIElement view)
                    {
                        if (!(view is Window windowCheck) || (!windowCheck.IsLoaded && (new WindowInteropHelper(windowCheck).Handle == IntPtr.Zero)))
                        {
                            Log.Info($"Using cached view for {model}.");
                            return view;
                        }
                    }
                }

                return LocateForModelType(model.GetType(), displayLocation, context);
            };

        /// <summary>
        /// Locates the view for the specified model type.
        /// </summary>
        /// <returns>The view.</returns>
        /// <remarks>Pass the model type, display location (or null) and the context instance (or null) as parameters and receive a view instance.</remarks>
        public static Func<Type, DependencyObject, object, UIElement> LocateForModelType =
            (modelType, displayLocation, context) =>
            {
                var viewTypeName = modelType.FullName.Replace("Model", string.Empty);
                if (context != null)
                {
                    viewTypeName = viewTypeName.Remove(viewTypeName.Length - 4, 4);
                    viewTypeName = viewTypeName + "." + context;
                }

                var viewType = (from assembly in AssemblySource.Instance
                                from type in assembly.GetExportedTypes()
                                where type.FullName == viewTypeName
                                select type).FirstOrDefault();

                return viewType == null
                       ? new TextBlock { Text = $"{viewTypeName} not found." }
                       : GetOrCreateViewType(viewType);
            };

        /// <summary>
        /// Retrieves the view from the IoC container or tries to create it if not found.
        /// </summary>
        /// <remarks>Pass nthe type of view as a parameter and recieve an instance of the view.</remarks>
        public static Func<Type, UIElement> GetOrCreateViewType =
            viewType =>
            {
                var view = IoC.GetAllInstances(viewType).FirstOrDefault() as UIElement;

                if (view != null)
                {
                    InitializeComponent(view);
                    return view;
                }

                if (viewType.IsInterface || viewType.IsAbstract || !typeof(UIElement).IsAssignableFrom(viewType))
                    return new TextBlock { Text = $"Cannot create {viewType.FullName}." };

                view = (UIElement)Activator.CreateInstance(viewType);
                InitializeComponent(view);
                return view;
            };

        /// <summary>
        /// When a view does not contain a code-behind file, we need to automatically call InitializeComponent.
        /// </summary>
        /// <param name="element">The element to inialize.</param>
        public static void InitializeComponent(object element)
        {
            var initializeComponentMethod = element.GetType().GetMethod("InitializeComponent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (initializeComponentMethod == null)
                return;

            try
            {
                initializeComponentMethod.Invoke(element, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}