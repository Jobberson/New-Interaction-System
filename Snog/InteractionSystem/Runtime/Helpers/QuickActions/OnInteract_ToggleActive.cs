using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Toggles the active state of one or more GameObjects when interaction fires.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Toggle Active")]
    public class OnInteract_ToggleActive : MonoBehaviour
    {
        [Tooltip("GameObjects to toggle.")]
        public GameObject[] targets;

        public void Apply()
        {
            if (targets == null) return;
            foreach (var t in targets)
                if (t != null) t.SetActive(!t.activeSelf);
        }
    }
}
