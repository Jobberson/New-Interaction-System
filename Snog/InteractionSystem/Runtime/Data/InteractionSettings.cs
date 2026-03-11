using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Data
{
    [CreateAssetMenu(fileName = "InteractionSettings", menuName = "Snog/InteractionSystem/Settings")]
    public class InteractionSettings : ScriptableObject
    {
        [Header("Detection")]
        [Tooltip("Maximum distance at which an interactable can be focused.")]
        public float interactDistance = 3.0f;

        [Tooltip("Only objects on these layers can be detected. Create a dedicated 'Interactable' layer and " +
                 "assign your interactive objects to it — using 'Everything' hurts performance and causes " +
                 "the player's own collider to interfere with detection.")]
        public LayerMask interactableMask = 1; // Default layer only — intentionally not ~0

        [Tooltip("Radius of the SphereCast used to detect interactables. Increase for easier aiming " +
                 "at small objects. 0 = exact raycast.")]
        [Min(0f)]
        public float sphereRadius = 0.05f;

        [Tooltip("Whether the SphereCast detects trigger colliders. Enable this if your interactables " +
                 "use trigger colliders (common for large zone-based interactions).")]
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Input")]
        [Tooltip("Fallback key used when the New Input System is not active, or when no InputActionReference is assigned.")]
        public KeyCode interactKey = KeyCode.E;

        [Tooltip("If true, the player must hold the interact button for holdDuration seconds to trigger interaction.")]
        public bool holdToInteract = false;

        [Tooltip("How long the player must hold the button when holdToInteract is enabled. " +
                 "Individual interactables can override this via IInteractableMode.")]
        [Min(0.05f)]
        public float holdDuration = 0.5f;

        [Header("Prompt Format")]
        [Tooltip("Format string for press-mode prompts. {0} = key name, {1} = action label.")]
        public string pressPromptFormat = "[{0}]  {1}";

        [Tooltip("Format string for hold-mode prompts. {0} = key name, {1} = action label.")]
        public string holdPromptFormat  = "Hold [{0}] to {1}";

        private void OnValidate()
        {
            interactDistance = Mathf.Max(0.1f, interactDistance);
            sphereRadius     = Mathf.Max(0f,   sphereRadius);
            holdDuration     = Mathf.Max(0.05f, holdDuration);
        }
    }
}
