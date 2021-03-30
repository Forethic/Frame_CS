using System;
using System.Collections.Generic;

namespace Caliburn.Micro
{
    /// <summary>
    /// A implementation of <see cref="IResult"/> the enables sequential execution of multiple results.
    /// </summary>
    public class SequentialResult : IResult
    {
        readonly IEnumerator<IResult> enumerator;
        ActionExecutionContext context;

        /// <summary>
        /// Occurs when execution has completed.
        /// </summary>
        public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };

        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialResult"/> class.
        /// </summary>
        /// <param name="enumerator"></param>
        public SequentialResult(IEnumerator<IResult> enumerator)
        {
            this.enumerator = enumerator;
        }

        /// <summary>
        /// Executes the result using the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute(ActionExecutionContext context)
        {
            this.context = context;
            ChildCompleted(null, new ResultCompletionEventArgs());
        }

        void ChildCompleted(object sender, ResultCompletionEventArgs e)
        {
            if (e.Error != null || e.WasCancelled)
            {
                OnComplete(e.Error, e.WasCancelled);
                return;
            }

            if (sender is IResult previous)
            {
                previous.Completed -= ChildCompleted;
            }

            bool moveNextSucceeded = false;
            try
            {
                moveNextSucceeded = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                OnComplete(ex, false);
                return;
            }

            if (moveNextSucceeded)
            {
                try
                {
                    var next = enumerator.Current;
                    IoC.BuildUp(next);
                    next.Completed += ChildCompleted;
                    next.Execute(context);
                }
                catch (Exception ex)
                {
                    OnComplete(ex, false);
                    return;
                }
            }
            else
            {
                OnComplete(null, false);
            }
        }

        void OnComplete(Exception error, bool wasCancelled)
        {
            Completed(this, new ResultCompletionEventArgs { Error = error, WasCancelled = wasCancelled });
        }
    }
}