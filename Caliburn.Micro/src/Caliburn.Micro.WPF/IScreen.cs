using System;

namespace Caliburn.Micro
{
    public interface IActivate
    {
        /// <summary>
        /// Indicates whether or not this instance is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Activates this instance.
        /// </summary>
        void Activate();

        /// <summary>
        /// Raised after activation occurs.
        /// </summary>
        event EventHandler<ActivationEventArgs> Activated;
    }

    /// <summary>
    /// Denotes an instance which requires deactivation.
    /// </summary>
    public interface IDeactivate
    {
        /// <summary>
        /// Raised before deactivation.
        /// </summary>
        event EventHandler<DeactivationEventArgs> AttemptingDeactivation;

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        /// <param name="close"></param>
        void Deactivate(bool close);

        /// <summary>
        /// Raised after deactivation.
        /// </summary>
        event EventHandler<DeactivationEventArgs> Deactivated;
    }

    /// <summary>
    /// Denotes an object that can be closed.
    /// </summary>
    public interface IClose
    {
        /// <summary>
        /// Tries to close this instance.
        /// </summary>
        void TryClose();
    }

    /// <summary>
    /// Denotes an instance which may prevent closing.
    /// </summary>
    public interface IGuardClose : IClose
    {
        /// <summary>
        /// Called to check whether or not this instance can close.
        /// </summary>
        /// <param name="callback">The implementer calls this action with the result of the close check.</param>
        void CanClose(Action<bool> callback);
    }

    public interface IScreen
    {

    }

    /// <summary>
    /// Denotes an instance which has a display name.
    /// </summary>
    public interface IHaveDisplayName
    {
        /// <summary>
        /// Gets or Sets the Display Name
        /// </summary>
        string DisplayName { get; set; }
    }

    /// <summary>
    /// Contains details about the success or failure of an item's activation through an <see cref="IConductor"/>.
    /// </summary>
    public class ActivationEventArgs : EventArgs
    {
        /// <summary>
        /// The item whose activation was processed.
        /// </summary>
        public object Item;

        /// <summary>
        /// Gets or Sets a value indicating whether the activation was a success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c></value>
        public bool Success;
    }

    /// <summary>
    /// EventArgs sent during deactivation.
    /// </summary>
    public class DeactivationEventArgs : EventArgs
    {
        /// <summary>
        /// Indicates whether the sender was closed in addition to being deactivated.
        /// </summary>
        public bool WasClosed;
    }
}