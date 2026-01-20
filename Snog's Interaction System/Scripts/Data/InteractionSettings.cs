using UnityEngine;

namespace Snog.InteractionSystem.Scripts.Data
{
    [CreateAssetMenu(fileName = "InteractionSettings", menuName = "Snog/InteractionSystem/Settings")]
    public class InteractionSettings : ScriptableObject
    {
        [Header("Detection")]
        public float interactDistance = 3.0f;
        public LayerMask interactableMask = ~0;  
        public float sphereRadius = 0.05f;       

        [Header("Input")]
        public KeyCode interactKey = KeyCode.E;
        public bool holdToInteract = false;
        public float holdDuration = 0.5f;
        
        [Header("Prompt")]
        public string pressPromptFormat = "Press {0} to {1}";
        public string holdPromptFormat  = "Hold {0} to {1}";
    }
}