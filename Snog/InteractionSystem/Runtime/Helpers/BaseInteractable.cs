using UnityEngine;
using UnityEngine.Events;

using Snog.InteractionSystem.Scripts.Data;
using Snog.InteractionSystem.Scripts.Interfaces;

namespace Snog.InteractionSystem.Runtime.Helpers
{
    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interactable")]
        [SerializeField] private string prompt = "Interact";
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        [Header("Interaction Mode (Override)")]
        [SerializeField] private InteractionMode interactionMode = InteractionMode.InheritFromInteractor;

        [Tooltip("If true and mode is Hold, this value overrides the interactor's hold duration.")]
        [SerializeField] private bool overrideHoldDuration = false;

        [Min(0f)]
        [SerializeField] private float holdDurationSeconds = 0.5f;

        public virtual string GetInteractionPrompt()
        {
            return prompt;
        }

        public virtual bool CanInteract(GameObject interactor)
        {
            return isEnabled && isActiveAndEnabled;
        }

        public virtual void OnFocus(GameObject interactor)
        {
            // Optional: highlight, outline, sfx
        }

        public virtual void OnDefocus(GameObject interactor)
        {
            // Optional: remove highlight
        }

        public UnityEvent OnInteractEvent
        {
            get
            {
                return onInteract;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
                return;

            PerformInteraction(interactor);
            onInteract?.Invoke();
        }

        protected abstract void PerformInteraction(GameObject interactor);

        public void SetEnabled(bool value)
        {
            isEnabled = value;
        }

        public virtual InteractionMode GetInteractionMode()
        {
            return interactionMode;
        }

        public virtual bool TryGetHoldDuration(out float seconds)
        {
            if (overrideHoldDuration)
            {
                seconds = Mathf.Max(0f, holdDurationSeconds);
                return true;
            }

            seconds = 0f;
            return false;
        }
    }
}