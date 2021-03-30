using System;

namespace Caliburn.Micro
{
    /// <summary>
    /// A logger
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Logs the message as a info.
        /// </summary>
        /// <param name="format">A formatted message.</param>
        /// <param name="args">Parameters to be injected into the formated message.</param>
        void Info(string format, params object[] args);

        /// <summary>
        /// Logs the message as a warning.
        /// </summary>
        /// <param name="format">A formatted message.</param>
        /// <param name="args">Parameters to be injected into the formated message.</param>
        void Warn(string format, params object[] args);

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        void Error(Exception exception);
    }

    public static class LogManager
    {
        static readonly ILog NullLogInstance = new NullLog();

        /// <summary>
        /// Creates an <see cref="ILog"/> for the provided type.
        /// </summary>
        public static Func<Type, ILog> GetLog = type => NullLogInstance;

        private class NullLog : ILog
        {
            public void Error(Exception exception) { }

            public void Info(string format, params object[] args) { }

            public void Warn(string format, params object[] args) { }
        }
    }
}