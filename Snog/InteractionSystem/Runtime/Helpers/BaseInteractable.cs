using UnityEngine;
using UnityEngine.Events;
using Snog.InteractionSystem.Runtime.Conditions;
using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Runtime.Helpers
{
    /// <summary>
    /// Convenience base class for interactable objects. Inherit this and override
    /// <see cref="PerformInteraction"/> to add your logic.
    ///
    /// Features handled automatically:
    /// <list type="bullet">
    ///   <item>Prompt label and isEnabled toggle</item>
    ///   <item>onInteract UnityEvent (fired once per interaction, never double-fired)</item>
    ///   <item>Per-object press/hold mode override</item>
    ///   <item>Cooldown between interactions</item>
    ///   <item>Max-use charge limit</item>
    ///   <item>ScriptableObject conditions via <see cref="RequiresConditions"/> component</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Interaction/Base Interactable")]
    public abstract class BaseInteractable : MonoBehaviour, IInteractable, IInteractableMode
    {
        // ── Core ──────────────────────────────────────────────────────────────────

        [Header("Interactable")]
        [Tooltip("Action label shown in the prompt, e.g. 'Open', 'Read'. " +
                 "Override GetInteractionPrompt() for dynamic labels.")]
        [SerializeField] private string prompt = "Interact";

        [Tooltip("Set false to disable this interactable without destroying it. " +
                 "Can also be toggled at runtime via SetEnabled().")]
        [SerializeField] private bool isEnabled = true;

        [Tooltip("Fired after PerformInteraction() completes. " +
                 "Do NOT call this inside PerformInteraction — the base class fires it automatically.")]
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        // ── Mode override ─────────────────────────────────────────────────────────

        [Header("Interaction Mode Override")]
        [Tooltip("Override the global press/hold setting for this object. " +
                 "Leave as InheritFromInteractor to respect the setting in InteractionSettings.")]
        [SerializeField] private InteractionMode interactionMode = InteractionMode.InheritFromInteractor;

        [Tooltip("If true, overrides the global holdDuration in InteractionSettings for this object.")]
        [SerializeField] private bool overrideHoldDuration = false;

        [Min(0f)]
        [SerializeField] private float holdDurationSeconds = 0.5f;

        // ── Cooldown & uses ───────────────────────────────────────────────────────

        [Header("Cooldown & Uses")]
        [Tooltip("Seconds before this interactable can be used again after each interaction. " +
                 "0 = no cooldown.")]
        [Min(0f)]
        [SerializeField] private float cooldown = 0f;

        [Tooltip("Maximum number of times this interactable can be used. " +
                 "-1 = unlimited.")]
        [SerializeField] private int maxUses = -1;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private float             cooldownEndTime  = 0f;
        private int               usesRemaining;
        private RequiresConditions conditionChecker;
        private bool              checkedForConditions = false;

        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called once when the object awakens. Override to add initialization,
        /// but always call <c>base.Awake()</c> first.
        /// </summary>
        protected virtual void Awake()
        {
            usesRemaining = maxUses;
        }

        // Lazily cache RequiresConditions so it works even if added at edit time after Awake.
        private RequiresConditions GetConditionChecker()
        {
            if (!checkedForConditions)
            {
                conditionChecker     = GetComponent<RequiresConditions>();
                checkedForConditions = true;
            }
            return conditionChecker;
        }

        // ── IInteractable ─────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public virtual string GetInteractionPrompt() => prompt;

        /// <inheritdoc/>
        public virtual bool CanInteract(GameObject interactor)
        {
            if (!isEnabled || !isActiveAndEnabled) return false;

            // Cooldown
            if (cooldown > 0f && Time.time < cooldownEndTime) return false;

            // Uses remaining
            if (maxUses >= 0 && usesRemaining <= 0) return false;

            // ScriptableObject conditions
            var checker = GetConditionChecker();
            if (checker != null && !checker.Evaluate(interactor)) return false;

            return true;
        }

        /// <inheritdoc/>
        public virtual void OnFocus(GameObject interactor)   { /* Override: highlight, outline, SFX */ }

        /// <inheritdoc/>
        public virtual void OnDefocus(GameObject interactor) { /* Override: remove highlight */ }

        /// <summary>
        /// Entry point called by PlayerInteractor. Validates CanInteract, runs PerformInteraction,
        /// fires onInteract, then applies cooldown and decrements uses.
        /// Do NOT override this — override <see cref="PerformInteraction"/> instead.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            PerformInteraction(interactor);
            onInteract?.Invoke();

            if (cooldown > 0f)
                cooldownEndTime = Time.time + cooldown;

            if (maxUses >= 0)
                usesRemaining = Mathf.Max(0, usesRemaining - 1);
        }

        // ── IInteractableMode ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        public virtual InteractionMode GetInteractionMode() => interactionMode;

        /// <inheritdoc/>
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

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>UnityEvent fired after each successful interaction. Exposed for editor tooling.</summary>
        public UnityEvent OnInteractEvent => onInteract;

        /// <summary>Enable or disable this interactable at runtime.</summary>
        public void SetEnabled(bool value) => isEnabled = value;

        /// <summary>
        /// Seconds remaining on the current cooldown. 0 when not on cooldown.
        /// Useful for driving a UI fill bar or tooltip.
        /// </summary>
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

        /// <summary>
        /// How many uses remain. Returns -1 if uses are unlimited (<see cref="maxUses"/> == -1).
        /// </summary>
        public int UsesRemaining => maxUses < 0 ? -1 : usesRemaining;

        /// <summary>Whether this interactable has unlimited uses.</summary>
        public bool HasUnlimitedUses => maxUses < 0;

        /// <summary>Resets the cooldown timer immediately.</summary>
        public void ResetCooldown() => cooldownEndTime = 0f;

        /// <summary>Restores uses to the configured maximum. Has no effect if uses are unlimited.</summary>
        public void ResetUses() => usesRemaining = maxUses;

        // ── Abstract ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Implement your interaction logic here.
        /// The base class handles CanInteract(), onInteract, cooldown, and use-counting automatically.
        /// Do NOT fire onInteract or check CanInteract inside this method.
        /// </summary>
        protected abstract void PerformInteraction(GameObject interactor);
    }
}
