using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Caliburn.Micro
{
    /// <summary>
    /// Instantiate this class in order to configure the framework.
    /// </summary>
    public class Bootstraper
    {
        /// <summary>
        /// The application.
        /// </summary>
        public Application Application { get; protected set; }

        private static bool? isInDesignMode;

        /// <summary>
        /// Indicates whether or not the framework is in design-time mode.
        /// </summary>
        public static bool IsInDesignMode
        {
            get
            {
                if (isInDesignMode == null)
                {
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;

                    // Debug 的时候 运行程序名称开头 : devenv
                    if (!isInDesignMode.GetValueOrDefault(false) && Process.GetCurrentProcess().ProcessName.StartsWith("devenv", StringComparison.Ordinal))
                        isInDesignMode = true;
                }

                return isInDesignMode.GetValueOrDefault(false);
            }
        }

        /// <summary>
        /// Creates an instance of the bootstrapper.
        /// </summary>
        public Bootstraper()
        {
            if (IsInDesignMode)
                StartDesignTime();
            else StartRuntime();
        }

        /// <summary>
        /// Called by the bootstarpper's constructor at design time to start the framework.
        /// </summary>
        protected virtual void StartDesignTime() { }

        /// <summary>
        /// Called by the bootstrapper's constructor at runtime to start the framework.
        /// </summary>
        protected virtual void StartRuntime()
        {
            Execute.InitializeWithDispatcher();
            AssemblySource.Instance.AddRange(SelectAssemblies());

            Application = Application.Current;
            PrepareApplication();

            Configure();
            IoC.GetInstance = GetInstance;
            IoC.GetAllInstances = GetAllInstances;
            IoC.BuildUp = BuildUp;
        }

        /// <summary>
        /// Override to tell the framework where to find assemblies to inspect for views, etc.
        /// </summary>
        /// <returns>A list of assemblies to inspect.</returns>
        protected virtual IEnumerable<Assembly> SelectAssemblies() => new[] { Assembly.GetEntryAssembly() };

        /// <summary>
        /// Provides an opportunity to hook into the application object.
        /// </summary>
        protected virtual void PrepareApplication()
        {
            Application.Startup += OnStartup;
            Application.DispatcherUnhandledException += OnUnhandledException;
            Application.Exit += OnExit;
        }

        /// <summary>
        /// Override to configure the framework and setup your IOC container.
        /// </summary>
        protected virtual void Configure() { }

        /// <summary>
        /// Override this to provide an IoC specific implementation.
        /// </summary>
        /// <param name="service">The service to locate.</param>
        /// <param name="key">The key to locate.</param>
        /// <returns>The located service.</returns>
        protected virtual object GetInstance(Type service, string key) => Activator.CreateInstance(service);

        /// <summary>
        /// Override this to provide an IoC specific implementation.
        /// </summary>
        /// <param name="service">The service to locate.</param>
        /// <returns>The located services.</returns>
        protected virtual IEnumerable<object> GetAllInstances(Type service) => new[] { Activator.CreateInstance(service) };

        /// <summary>
        /// Override this to provide an IoC specific implementation.
        /// </summary>
        /// <param name="instance">The instance to perform injection on.</param>
        protected virtual void BuildUp(object instance) { }

        /// <summary>
        /// Override this to add custom behavior to execute after the application starts.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnStartup(object sender, StartupEventArgs e) => DisplayRootView();

        /// <summary>
        /// Override this to add custom behavior for unhandled exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) { }

        /// <summary>
        /// Override this to add custom behavior on exit
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnExit(object sender, ExitEventArgs e) { }

        /// <summary>
        /// Override to display your UI at startup.
        /// </summary>
        protected virtual void DisplayRootView() { }
    }

    /// <summary>
    /// A strongly-typed version of <see cref="Bootstraper"/> that specifies the type of root model to create for the application.
    /// </summary>
    /// <typeparam name="TRootModel"></typeparam>
    public class Bootstrapper<TRootModel> : Bootstraper
    {
        /// <summary>
        /// Override to display your UI at startup.
        /// </summary>
        protected override void DisplayRootView()
        {
            var viewModel = IoC.Get<TRootModel>();
            IWindowManager windowManager;

            try
            {
                windowManager = IoC.Get<IWindowManager>();
            }
            catch
            {
                windowManager = new WindowManager();
            }

            windowManager.ShowWindow(viewModel);

        }
    }
}