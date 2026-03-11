using UnityEngine;
using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Helpers;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Samples.Examples
{
    /// <summary>
    /// Example interactable demonstrating:
    ///  - Inheriting BaseInteractable
    ///  - Implementing ICustomPrompt for icons and an unavailable-state label
    ///  - Using CanInteract() to gate the interaction
    ///  - Dynamic prompt text that changes based on state
    /// </summary>
    public class KeycardDoorInteractable : BaseInteractable, ICustomPrompt
    {
        [Header("Door State")]
        [SerializeField] private bool isOpen   = false;
        [SerializeField] private bool isLocked = true;

        [Header("Keycard")]
        [Tooltip("Name of the item the player needs to unlock this door.")]
        [SerializeField] private string requiredItem = "Red Keycard";

        [Header("Icons")]
        [SerializeField] private Sprite openHandIcon;
        [SerializeField] private Sprite lockIcon;

        // ── BaseInteractable overrides ─────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake(); // required to initialise cooldown/uses state
        }

        protected override void PerformInteraction(GameObject interactor)
        {
            // CanInteract() already blocks entry when locked, but guard defensively.
            if (isLocked) return;
            isOpen = !isOpen;
            // Extend here: drive an Animator, play audio, etc.
        }

        public override bool CanInteract(GameObject interactor)
        {
            if (isLocked) return false;
            return base.CanInteract(interactor);
        }

        public override string GetInteractionPrompt()
        {
            return isOpen ? "Close" : "Open";
        }

        // ── ICustomPrompt ─────────────────────────────────────────────────────────

        public InteractionPromptData GetPromptData()
        {
            return new InteractionPromptData
            {
                label             = isOpen ? "Close" : "Open",
                isFullSentence    = false,

                showWhenUnavailable = isLocked,
                unavailableLabel    = $"Locked — requires {requiredItem}",

                availableIcon   = openHandIcon,
                unavailableIcon = lockIcon,
            };
        }

        // ── Public API (for inventory system, etc.) ────────────────────────────────

        /// <summary>Call this from your inventory system when the player uses the keycard.</summary>
        public void Unlock()
        {
            isLocked = false;
        }
    }
}
