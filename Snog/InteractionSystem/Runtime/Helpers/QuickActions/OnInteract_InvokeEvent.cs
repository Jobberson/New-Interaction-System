using UnityEngine;
using UnityEngine.Events;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>
    /// Fires a configurable UnityEvent when interaction fires.
    /// Use this when you need a standalone UnityEvent separate from BaseInteractable's built-in onInteract.
    /// </summary>
    [AddComponentMenu("Interaction/Quick Actions/Invoke Event")]
    public class OnInteract_InvokeEvent : MonoBehaviour
    {
        [Tooltip("Event to invoke when interaction fires.")]
        public UnityEvent onInteractEvent;

        public void Invoke() => onInteractEvent?.Invoke();
    }
}
