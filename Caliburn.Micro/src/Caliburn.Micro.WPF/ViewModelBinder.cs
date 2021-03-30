using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace Caliburn.Micro
{
    public class ViewModelBinder
    {
        static ILog Log = LogManager.GetLog(typeof(ViewModelBinder));

        public static bool ApplyConventionsByDefault = true;

        #region ConventionsApplied

        public static DependencyProperty ConventionsAppliedProperty = DependencyProperty.RegisterAttached("ConventionsApplied", typeof(bool), typeof(ViewModelBinder));

        #endregion


        public static Action<object, DependencyObject, object> Bind =
            (viewModel, view, context) =>
            {
                Log.Info($"Binding {view} and {viewModel}.");
                Action.SetTarget(view, viewModel);

                if (viewModel is IViewAware viewAware)
                {
                    Log.Info($"Attaching {view} to {viewAware}.");
                    viewAware.AttachView(view, context);
                }

                // 如果 view 已经使用命名约定的话，不需要再次进行绑定
                if ((bool)view.GetValue(ConventionsAppliedProperty))
                    return;

                if (View.GetFirstNonGeneratedView(view) is FrameworkElement element)
                {
                    if (!ShouldApplyConventions(element))
                    {
                        Log.Info($"Skipping conventions {element} and {viewModel}.");
                        return;
                    }

                    var viewModelType = viewModel.GetType();
                    var namedElements = ExtensionMethods.GetNamedElementsInScope(element);
                    var isLoaded = element.GetValue(View.IsLoadedProperty);

                    namedElements.Apply(x => x.SetValue(View.IsLoadedProperty, isLoaded));

                    BindActions(namedElements, viewModelType);
                    BindProperties(namedElements, viewModelType);

                    view.SetValue(ConventionsAppliedProperty, true);
                }
            };

        /// <summary>
        /// Determines whether a view should have conventions applied to it.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>Whether or not conventions should be applied to the view.</returns>
        private static bool ShouldApplyConventions(FrameworkElement view)
        {
            var overriden = View.GetApplyConventions(view);
            return overriden.GetValueOrDefault(ApplyConventionsByDefault);
        }

        /// <summary>
        /// Attaches instance of <see cref="ActionMessage"/> to the view's controls based on the provided methods.
        /// </summary>
        /// <remarks>Parameters include the named elements to search through and the type of view model to determine conventions for.</remarks>
        public static Action<IEnumerable<FrameworkElement>, Type> BindActions =
            (namedElements, viewModelType) =>
            {
                var methods = viewModelType.GetMethods();
                foreach (var method in methods)
                {
                    var foundControl = namedElements.FindName(method.Name);
                    if (foundControl == null)
                    {
                        Log.Info($"No bindable control for action {method.Name}.");
                        continue;
                    }

                    var triggers = Interaction.GetTriggers(foundControl);
                    if (triggers?.Count > 0)
                    {
                        Log.Info($"Interaction.Triggers already set on control {foundControl.Name}");
                        continue;
                    }

                    var message = method.Name;
                    var parameters = method.GetParameters();

                    if (parameters.Length > 0)
                    {
                        message += "(";

                        foreach (var parameter in parameters)
                        {
                            var paramName = parameter.Name;
                            var specialValue = "$" + paramName.ToLower();

                            if (MessageBinder.SpecialValues.Contains(specialValue))
                                paramName = specialValue;

                            message += paramName + ",";
                        }

                        message = message.Remove(message.Length - 1, 1);
                        message += ")";
                    }

                    Log.Info($"Added convention action for {method.Name} as {message}.");
                    Message.SetAttach(foundControl, message);
                }
            };

        /// <summary>
        /// Creates data bindings on the view's control's based on the provided properties.
        /// </summary>
        /// <remarks>Parameters include named Elements to search through and the tyepe of view model to determine conventions for.</remarks>
        public static Action<IEnumerable<FrameworkElement>, Type> BindProperties =
            (namedElements, viewModelType) =>
            {
                foreach (var element in namedElements)
                {
                    var cleanName = element.Name.Trim('_');
                    var parts = cleanName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

                    var property = viewModelType.GetPropertyCaseInsensitive(parts[0]);
                    var interpretedViewModelType = viewModelType;

                    for (int i = 1; i < parts.Length && property != null; i++)
                    {
                        interpretedViewModelType = property.PropertyType;
                        property = interpretedViewModelType.GetPropertyCaseInsensitive(parts[i]);
                    }

                    if (property == null)
                    {
                        Log.Info($"No convention applied to {element.Name}.");
                        continue;
                    }

                    var convention = ConventionManager.GetElementConvention(element.GetType());
                    if (convention == null)
                    {
                        Log.Warn($"No convention configured for {element.GetType()}.");
                        continue;
                    }

                    convention.ApplyBinding(interpretedViewModelType, cleanName.Replace('_', '.'), property, element, convention);
                    Log.Info($"Added convention binding for {element.GetType()}");
                }
            };

    }
}