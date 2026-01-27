using UnityEngine;

namespace Snog.InteractionSystem.Samples.Examples
{
    public class KeycardDoorInteractable : BaseInteractable, ICustomPrompt
    {
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = true;
        [SerializeField] private string requiredItem = "Red Keycard";

        [SerializeField] private Sprite openHandIcon;
        [SerializeField] private Sprite lockIcon;

        protected override void PerformInteraction(GameObject interactor)
        {
            if (isLocked)
                return;

            isOpen = !isOpen;
        }

        public override bool CanInteract(GameObject interactor)
        {
            if (isLocked)
            {
                return false;
            }

            return base.CanInteract(interactor);
        }

        public override string GetInteractionPrompt()
        {
            return isOpen ? "Close" : "Open";
        }

        public InteractionPromptData GetPromptData()
        {
            return new InteractionPromptData
            {
                label = isOpen ? "Close" : "Open",
                isFullSentence = false,

                showWhenUnavailable = isLocked,
                unavailableLabel = $"Locked — Requires {requiredItem}",

                availableIcon = openHandIcon,
                unavailableIcon = lockIcon
            };
        }
    }
}