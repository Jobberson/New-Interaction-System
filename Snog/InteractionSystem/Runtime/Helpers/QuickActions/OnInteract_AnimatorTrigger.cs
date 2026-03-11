using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Sets an Animator trigger when interaction fires.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Animator Trigger")]
    public class OnInteract_AnimatorTrigger : MonoBehaviour
    {
        [Tooltip("Animator to target. If null, tries to find one on this GameObject.")]
        public Animator animator;

        [Tooltip("Name of the trigger parameter to set.")]
        public string triggerName = "OnInteract";

        private void Reset() { animator = GetComponent<Animator>(); }

        public void Fire()
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
                animator.SetTrigger(triggerName);
        }
    }
}
