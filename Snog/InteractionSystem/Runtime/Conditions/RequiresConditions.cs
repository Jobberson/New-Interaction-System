using UnityEngine;
using Snog.InteractionSystem.Runtime.Conditions;

namespace Snog.InteractionSystem.Runtime.Conditions
{
    /// <summary>
    /// Add this component to any interactable GameObject alongside a <see cref="Helpers.BaseInteractable"/>
    /// to gate the interaction behind one or more <see cref="InteractionCondition"/> assets.
    ///
    /// <para>BaseInteractable automatically checks this component during CanInteract().</para>
    ///
    /// <para>No code required — create condition assets via the Assets › Create menu,
    /// then drag them into the conditions list here.</para>
    /// </summary>
    [AddComponentMenu("Interaction/Requires Conditions")]
    public class RequiresConditions : MonoBehaviour
    {
        [Tooltip("List of conditions that must be met for the interaction to be allowed.")]
        [SerializeField] private InteractionCondition[] conditions = new InteractionCondition[0];

        [Tooltip("If true (AND), ALL conditions must pass. " +
                 "If false (OR), ANY single condition passing is enough.")]
        [SerializeField] private bool requireAll = true;

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates all conditions using the configured AND/OR logic.
        /// Returns true if the interaction should be allowed.
        /// </summary>
        public bool Evaluate(GameObject interactor)
        {
            if (conditions == null || conditions.Length == 0) return true;

            if (requireAll)
            {
                // AND: every condition must pass
                foreach (var condition in conditions)
                    if (condition != null && !condition.Evaluate(interactor)) return false;
                return true;
            }
            else
            {
                // OR: at least one condition must pass
                foreach (var condition in conditions)
                    if (condition != null && condition.Evaluate(interactor)) return true;
                return false;
            }
        }

        /// <summary>
        /// Returns the failure reason from the first failing condition, or empty string if all pass.
        /// Useful for displaying a context-aware "why can't I interact?" message.
        /// </summary>
        public string GetFirstFailureReason(GameObject interactor)
        {
            if (conditions == null) return string.Empty;
            foreach (var condition in conditions)
            {
                if (condition != null && !condition.Evaluate(interactor))
                    return condition.GetFailureReason();
            }
            return string.Empty;
        }

        /// <summary>Read-only access for editor tooling and debug overlays.</summary>
        public InteractionCondition[] Conditions => conditions;

        /// <summary>Whether the evaluation mode is AND (true) or OR (false).</summary>
        public bool RequireAll => requireAll;
    }
}
