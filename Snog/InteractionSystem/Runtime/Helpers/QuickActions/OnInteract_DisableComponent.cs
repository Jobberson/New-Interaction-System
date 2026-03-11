using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Disables one or more Behaviour components when interaction fires.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Disable Component")]
    public class OnInteract_DisableComponent : MonoBehaviour
    {
        [Tooltip("Components to disable.")]
        public Behaviour[] componentsToDisable;

        public void Disable()
        {
            if (componentsToDisable == null) return;
            foreach (var comp in componentsToDisable)
                if (comp != null) comp.enabled = false;
        }
    }
}
