using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Enables one or more Behaviour components when interaction fires.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Enable Component")]
    public class OnInteract_EnableComponent : MonoBehaviour
    {
        [Tooltip("Components to enable.")]
        public Behaviour[] componentsToEnable;

        public void Enable()
        {
            if (componentsToEnable == null) return;
            foreach (var comp in componentsToEnable)
                if (comp != null) comp.enabled = true;
        }
    }
}
