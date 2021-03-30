using System;
using System.Windows;
using System.Windows.Interactivity;

namespace Caliburn.Micro
{
    public static class Message
    {
        #region Handler

        /// <summary>
        /// Gets the message handler for this element.
        /// </summary>
        /// <param name="d">The element.</param>
        /// <returns>The message handler.</returns>
        public static object GetHandler(DependencyObject d) => d.GetValue(HandlerProperty);

        /// <summary>
        /// Places a message handler on this element.
        /// </summary>
        /// <param name="d">The element.</param>
        /// <param name="value">The message handler.</param>
        public static void SetHandler(DependencyObject d, object value) => d.SetValue(HandlerProperty, value);

        /// <summary>
        /// Host's attached properties related to routed UI messaging.
        /// </summary>
        public static DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached("Handler", typeof(object), typeof(Message));

        #endregion

        #region Attach

        /// <summary>
        /// Gets the attached triggers and messages.
        /// </summary>
        /// <param name="d">The element that was attached to.</param>
        /// <returns>The parsable attachment text.</returns>
        public static string GetAttach(DependencyObject d) => (string)d.GetValue(AttachProperty);

        /// <summary>
        /// Sets the attached triggers and messagss.
        /// </summary>
        /// <param name="d">The element to attach to.</param>
        /// <param name="value">The parsable attachment text.</param>
        public static void SetAttach(DependencyObject d, string value) => d.SetValue(AttachProperty, value);

        /// <summary>
        /// A property definition representing attached triggers and messages.
        /// </summary>
        public static DependencyProperty AttachProperty = DependencyProperty.RegisterAttached("Attach", typeof(string), typeof(Message), new PropertyMetadata(OnAttachChanged));

        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;

            var text = e.NewValue as string;
            if (string.IsNullOrEmpty(text))
                return;

            var triggers = Interaction.GetTriggers(d);
            Parser.Parse(d, text).Apply(triggers.Add);
        }

        #endregion
    }
}