using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Logs a message to the Console when interaction fires. Useful for prototyping.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Debug Log")]
    public class OnInteract_DebugLog : MonoBehaviour
    {
        [Tooltip("Message to print to the Console.")]
        public string message = "OnInteract_DebugLog fired.";

        public void Log() => UnityEngine.Debug.Log(message, this);
    }
}
