using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Caliburn.Micro
{
    /// <summary>
    /// A service that manages windows.
    /// </summary>
    public interface IWindowManager
    {
        /// <summary>
        /// Shows an modal dialog for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <returns>The dialog result.</returns>
        bool? ShowDialog(object rootModel, object context = null);

        /// <summary>
        /// Shows a non-modal window for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        void ShowWindow(object rootModel, object context = null);

        /// <summary>
        /// Shows a popup at the current mouse position.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The view context.</param>
        /// <param name="settings">The optional popup settings.</param>
        void ShowPopup(object rootModel, object context = null, IDictionary<string, object> settings = null);
    }

    public class WindowManager : IWindowManager
    {
        /// <summary>
        /// Shows an modal dialog for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <returns>The dialog result.</returns>
        public bool? ShowDialog(object rootModel, object context = null) => CreateWindow(rootModel, true, context).ShowDialog();

        /// <summary>
        /// Shows a non-modal window for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        public void ShowWindow(object rootModel, object context = null) => CreateWindow(rootModel, false, context).Show();

        /// <summary>
        /// Shows a popup at the current mouse position.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The view context.</param>
        /// <param name="settings">The optional popup settings.</param>
        public void ShowPopup(object rootModel, object context = null, IDictionary<string, object> settings = null)
        {
            throw new NotImplementedException();
        }

        protected virtual Window CreateWindow(object rootModel, bool isDialog, object context)
        {
            var view = EnsureWindow(rootModel, ViewLocator.LocateForModel(rootModel, null, context), isDialog);
            ViewModelBinder.Bind(rootModel, view, context);

            if (rootModel is IHaveDisplayName haveDisplayName && !ConventionManager.HasBinding(view, Window.TitleProperty))
            {
                var binding = new Binding("DisplayName") { Mode = BindingMode.TwoWay };
                view.SetBinding(Window.TitleProperty, binding);
            }

            new WindowConductor(rootModel, view);

            return view;
        }

        protected virtual Window EnsureWindow(object model, object view, bool isDialog)
        {
            if (view is Window window)
            {
                var owner = InferOwnerOf(window);
                if (owner != null && isDialog)
                    window.Owner = owner;
            }
            else
            {
                window = new Window
                {
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight,
                };

                window.SetValue(View.IsGeneratedPropertyProperty, true);

                var owner = InferOwnerOf(window);
                if (owner != null)
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = owner;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }

            return window;
        }

        protected virtual Window InferOwnerOf(Window window)
        {
            if (Application.Current == null)
                return null;

            var active = Application.Current.Windows.OfType<Window>()
                .Where(x => x.IsActive)
                .FirstOrDefault();
            active = active ?? Application.Current.MainWindow;
            return active == window ? null : active;
        }

        class WindowConductor
        {
            bool deactivatingFromView;
            bool deactivateFromViewModel;
            bool actuallyClosing;
            readonly Window view;
            readonly object model;

            public WindowConductor(object model, Window view)
            {
                this.model = model;
                this.view = view;

                if (model is IActivate activatable)
                    activatable.Activate();

                if (model is IDeactivate deactivatable)
                {
                    view.Closed += Closed;
                    deactivatable.Deactivated += Deactivated;
                }

                if (model is IGuardClose guard)
                    view.Closing += Closing;
            }

            private void Closed(object sender, EventArgs e)
            {
                view.Closed -= Closed;
                view.Closing -= Closing;

                if (deactivateFromViewModel)
                    return;

                if (model is IDeactivate deactivatable)
                {
                    deactivatingFromView = true;
                    deactivatable.Deactivate(true);
                    deactivatingFromView = false;
                }
            }

            private void Deactivated(object sender, DeactivationEventArgs e)
            {
                if (!e.WasClosed) return;

                ((IDeactivate)model).Deactivated -= Deactivated;

                if (deactivatingFromView) return;

                deactivateFromViewModel = true;
                actuallyClosing = true;
                view.Close();
                actuallyClosing = false;
                deactivateFromViewModel = false;
            }

            private void Closing(object sender, CancelEventArgs e)
            {
                if (e.Cancel) return;

                var guard = (IGuardClose)model;

                if (actuallyClosing)
                {
                    actuallyClosing = false;
                    return;
                }

                bool runningAsync = false, shouldEnd = false;

                guard.CanClose(canClose =>
                {
                    Execute.OnUIThread(() =>
                    {
                        if (runningAsync && canClose)
                        {
                            actuallyClosing = true;
                            view.Close();
                        }
                        else e.Cancel = !canClose;
                    });
                });

                if (shouldEnd) return;

                runningAsync = e.Cancel = true;
            }
        }
    }
}