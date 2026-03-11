using Snog.InteractionSystem.Runtime.Data;

namespace Snog.InteractionSystem.Runtime.Interfaces
{
    /// <summary>
    /// Optional interface for interactables that need to override the global press/hold mode
    /// or specify a custom hold duration per object.
    ///
    /// Implement this alongside <see cref="IInteractable"/> (without inheriting BaseInteractable)
    /// to get full per-object mode control. BaseInteractable already implements this interface.
    /// </summary>
    public interface IInteractableMode
    {
        /// <summary>Returns the interaction mode for this object, or InheritFromInteractor to use the global setting.</summary>
        InteractionMode GetInteractionMode();

        /// <summary>
        /// If this object overrides the hold duration, returns true and sets <paramref name="seconds"/>.
        /// Return false to use the global hold duration from InteractionSettings.
        /// </summary>
        bool TryGetHoldDuration(out float seconds);
    }
}
