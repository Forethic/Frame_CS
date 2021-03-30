using System;

namespace Caliburn.Micro
{
    /// <summary>
    /// Executes the result using the specified context.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Executes the result using specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        void Execute(ActionExecutionContext context);

        /// <summary>
        /// Occurs when execution has completed.
        /// </summary>
        event EventHandler<ResultCompletionEventArgs> Completed;
    }
}