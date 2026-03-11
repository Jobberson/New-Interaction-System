using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Quits the application when interaction fires. No effect in the Editor.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Quit Application")]
    public class OnInteract_QuitApplication : MonoBehaviour
    {
        public void Quit() => Application.Quit();
    }
}
