using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Reloads the currently active scene when interaction fires.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Reload Scene")]
    public class OnInteract_ReloadScene : MonoBehaviour
    {
        public void Reload()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }
}
