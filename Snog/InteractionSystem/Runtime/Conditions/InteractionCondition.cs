using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Conditions
{
    /// <summary>
    /// Base class for all interaction conditions. Create a ScriptableObject asset from a
    /// subclass and attach it to a <see cref="RequiresConditions"/> component on any interactable.
    ///
    /// <para>Built-in conditions:</para>
    /// <list type="bullet">
    ///   <item><see cref="GameObjectActiveCondition"/> — passes when a target GameObject is active/inactive</item>
    ///   <item><see cref="InvertCondition"/> — inverts any other condition</item>
    /// </list>
    ///
    /// <para>To create your own condition:</para>
    /// <code>
    /// [CreateAssetMenu(menuName = "Interaction/Conditions/My Condition")]
    /// public class MyCondition : InteractionCondition
    /// {
    ///     public override bool Evaluate(GameObject interactor) => /* your logic */;
    ///     public override string GetFailureReason() => "Not ready yet";
    /// }
    /// </code>
    /// </summary>
    public abstract class InteractionCondition : ScriptableObject
    {
        /// <summary>
        /// Returns true if this condition is met and the interaction should be allowed.
        /// Called by <see cref="RequiresConditions"/> from <c>CanInteract()</c>.
        /// </summary>
        /// <param name="interactor">The GameObject attempting to interact.</param>
        public abstract bool Evaluate(GameObject interactor);

        /// <summary>
        /// Optional human-readable reason shown in the prompt when this condition fails.
        /// Return null or empty to use the interactable's default unavailable label.
        /// </summary>
        public virtual string GetFailureReason() => string.Empty;
    }
}
