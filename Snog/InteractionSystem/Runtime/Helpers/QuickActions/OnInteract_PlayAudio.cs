using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Helpers.QuickActions
{
    /// <summary>Plays an AudioSource when interaction fires. Wire this to the onInteract UnityEvent.</summary>
    [AddComponentMenu("Interaction/Quick Actions/Play Audio")]
    public class OnInteract_PlayAudio : MonoBehaviour
    {
        [Tooltip("AudioSource to play. If null, tries to find one on this GameObject.")]
        public AudioSource target;

        private void Reset() { target = GetComponent<AudioSource>(); }

        public void Play()
        {
            if (target != null) target.Play();
        }
    }
}
