using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Interfaces
{
    /// <summary>
    /// Core interface for any interactable object. Implement this directly for full control,
    /// or inherit <see cref="Snog.InteractionSystem.Runtime.Helpers.BaseInteractable"/> for
    /// the convenience base-class workflow.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>The label shown in the prompt, e.g. "Open", "Read", "Pick up".</summary>
        string GetInteractionPrompt();

        /// <summary>Whether the player can interact right now (e.g. locked, out of charges).</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>Called when the player begins aiming at this object.</summary>
        void OnFocus(GameObject interactor);

        /// <summary>Called when the player stops aiming at this object.</summary>
        void OnDefocus(GameObject interactor);

        /// <summary>Called when the interaction completes (press or hold finished).</summary>
        void Interact(GameObject interactor);
    }
}
