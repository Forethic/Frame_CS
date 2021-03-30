using System.Reflection;

namespace Caliburn.Micro
{
    /// <summary>
    /// A source of assemblies that are inspectable by the framework.
    /// </summary>
    public class AssemblySource
    {
        /// <summary>
        /// The singleton instance of the AssemblySource used by the framework.
        /// </summary>
        public static readonly IObservaleCollection<Assembly> Instance = new BindableCollection<Assembly>();
    }
}
