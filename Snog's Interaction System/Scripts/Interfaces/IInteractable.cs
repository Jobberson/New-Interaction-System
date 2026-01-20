using UnityEngine;

namespace Snog.InteractionSystem.Scripts.Interfaces
{
    public interface IInteractable
    {
        /// <summary>
        /// The string shown in the prompt, e.g., "Open", "Read", "Pick up".
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Whether the player can interact right now (e.g., locked, out of charges, etc.).
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Called when the player starts focusing this object (aiming at it).
        /// </summary>
        void OnFocus(GameObject interactor);

        /// <summary>
        /// Called when the player stops focusing this object.
        /// </summary>
        void OnDefocus(GameObject interactor);

        /// <summary>
        /// Called when the interaction actually happens (press or hold completed).
        /// </summary>
        void Interact(GameObject interactor);
    }
}