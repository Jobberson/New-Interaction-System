using UnityEngine;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Runtime.Detection
{
    /// <summary>
    /// Detects the nearest IInteractable within a radius around this GameObject.
    /// The one with the smallest distance to the origin is returned.
    ///
    /// Best suited for:
    /// <list type="bullet">
    ///   <item>Top-down RPGs and isometric games</item>
    ///   <item>Mobile games with a single "interact" button</item>
    ///   <item>Any game where looking direction doesn't matter for interaction</item>
    /// </list>
    ///
    /// Assign this component to the "Detector" slot on PlayerInteractor.
    /// </summary>
    [AddComponentMenu("Interaction/Detection/Proximity Detector")]
    public class ProximityDetector : MonoBehaviour, IInteractionDetector
    {
        [Tooltip("How far from this GameObject to search for interactables.")]
        [Min(0.01f)]
        [SerializeField] private float radius = 2f;

        [Tooltip("Only objects on these layers are considered.")]
        [SerializeField] private LayerMask interactableMask = 1;

        [Tooltip("Whether to consider trigger colliders.")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("If true, only interactables whose CanInteract() returns true are considered. " +
                 "Disable this to return unavailable interactables as well (so the prompt still shows).")]
        [SerializeField] private bool requireCanInteract = false;

        // Reusable buffer to avoid per-frame allocation
        private static readonly Collider[] overlapBuffer = new Collider[32];

        /// <inheritdoc/>
        public IInteractable FindInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, overlapBuffer,
                interactableMask, triggerInteraction);

            IInteractable best     = null;
            float         bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var candidate = overlapBuffer[i].GetComponentInParent<IInteractable>();
                if (candidate == null) continue;
                if (requireCanInteract && !candidate.CanInteract(gameObject)) continue;

                // Use component transform for distance
                float dist = float.MaxValue;
                if (candidate is Component comp)
                    dist = Vector3.Distance(transform.position, comp.transform.position);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = candidate;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
