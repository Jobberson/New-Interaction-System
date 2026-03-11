using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Conditions
{
    /// <summary>
    /// Passes when the target GameObject's active state matches <see cref="requireActive"/>.
    ///
    /// Example use cases:
    /// <list type="bullet">
    ///   <item>A lever can only be pulled once a generator is active (requireActive = true)</item>
    ///   <item>A door can only open when a gate barrier is inactive (requireActive = false)</item>
    /// </list>
    ///
    /// Create via: Assets › Create › Interaction › Conditions › GameObject Active
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameObjectActiveCondition",
        menuName  = "Snog/InteractionSystem/Conditions/GameObject Active")]
    public class GameObjectActiveCondition : InteractionCondition
    {
        [Tooltip("The GameObject whose active state is checked.")]
        [SerializeField] private GameObject target;

        [Tooltip("If true, the condition passes when the target IS active. " +
                 "If false, the condition passes when the target is NOT active.")]
        [SerializeField] private bool requireActive = true;

        [Tooltip("Optional message shown in the prompt when this condition fails. " +
                 "Leave empty to use the interactable's default unavailable label.")]
        [SerializeField] private string failureReason = "";

        /// <inheritdoc/>
        public override bool Evaluate(GameObject interactor)
        {
            if (target == null) return true; // no target configured = always pass
            return target.activeSelf == requireActive;
        }

        /// <inheritdoc/>
        public override string GetFailureReason() => failureReason;
    }
}
