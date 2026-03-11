using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Conditions
{
    /// <summary>
    /// Inverts any other <see cref="InteractionCondition"/>. Passes when the inner condition
    /// fails, and fails when it passes.
    ///
    /// Create via: Assets › Create › Interaction › Conditions › Invert
    /// </summary>
    [CreateAssetMenu(
        fileName = "InvertCondition",
        menuName  = "Snog/InteractionSystem/Conditions/Invert")]
    public class InvertCondition : InteractionCondition
    {
        [Tooltip("The condition whose result will be flipped.")]
        [SerializeField] private InteractionCondition condition;

        [Tooltip("Optional message shown when this (inverted) condition fails.")]
        [SerializeField] private string failureReason = "";

        /// <inheritdoc/>
        public override bool Evaluate(GameObject interactor)
        {
            if (condition == null) return true;
            return !condition.Evaluate(interactor);
        }

        /// <inheritdoc/>
        public override string GetFailureReason() => failureReason;
    }
}
