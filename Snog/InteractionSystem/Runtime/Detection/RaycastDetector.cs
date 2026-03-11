using UnityEngine;
using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Runtime.Detection
{
    /// <summary>
    /// The default interaction detector. Performs a forward SphereCast from the player's camera.
    /// Best suited for first-person and over-the-shoulder third-person games.
    ///
    /// Assign this component (on the same or any GameObject) to the "Detector" slot on
    /// PlayerInteractor to use it. If no detector is assigned, PlayerInteractor falls back
    /// to its own built-in SphereCast logic using the same settings.
    /// </summary>
    [AddComponentMenu("Interaction/Detection/Raycast Detector")]
    public class RaycastDetector : MonoBehaviour, IInteractionDetector
    {
        [Tooltip("Camera to cast the ray from. Defaults to Camera.main if left empty.")]
        [SerializeField] private Camera detectorCamera;

        [Tooltip("Shared InteractionSettings asset. Must match the one on PlayerInteractor.")]
        [SerializeField] private InteractionSettings settings;

        private void Reset()
        {
            detectorCamera = Camera.main;
        }

        /// <inheritdoc/>
        public IInteractable FindInteractable()
        {
            if (detectorCamera == null || settings == null) return null;

            var ray = new Ray(detectorCamera.transform.position, detectorCamera.transform.forward);

            bool didHit = Physics.SphereCast(
                ray,
                settings.sphereRadius,
                out RaycastHit hit,
                settings.interactDistance,
                settings.interactableMask,
                settings.triggerInteraction);

            return didHit ? hit.collider.GetComponentInParent<IInteractable>() : null;
        }

        private void OnDrawGizmosSelected()
        {
            if (detectorCamera == null || settings == null) return;

            var origin  = detectorCamera.transform.position;
            var forward = detectorCamera.transform.forward;
            var end     = origin + forward * settings.interactDistance;

            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.5f);
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, settings.sphereRadius);
        }
    }
}
