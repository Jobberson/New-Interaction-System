using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Sets one or more GameObjects active/inactive when interaction fires.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Set Active")]
    public class OnInteract_SetActive : MonoBehaviour
    {
        [Tooltip("GameObjects to affect.")]
        public GameObject[] targets;

        [Tooltip("The active state to apply.")]
        public bool activeValue = true;

        public void Apply()
        {
            if (targets == null) return;
            foreach (var t in targets)
                if (t != null) t.SetActive(activeValue);
        }
    }
}
