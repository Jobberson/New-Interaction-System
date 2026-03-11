using UnityEditor;
using UnityEngine;
using Snog.InteractionSystem.Runtime.Data;

namespace Snog.InteractionSystem.Editor.Inspectors
{
    [CustomEditor(typeof(InteractionSettings))]
    public class InteractionSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var settings = (InteractionSettings)target;

            // Warn about broad layer mask
            bool maskIsEverything = settings.interactableMask == ~0 ||
                                    settings.interactableMask == -1;
            if (maskIsEverything)
            {
                EditorGUILayout.HelpBox(
                    "Interactable Mask is set to Everything.\n\n" +
                    "This means the SphereCast will hit the player's own collider, terrain, and all " +
                    "other objects before it reaches interactables.\n\n" +
                    "Recommended: Create a dedicated 'Interactable' layer, assign your interactable " +
                    "objects to it, and select only that layer here.",
                    MessageType.Warning);
            }

            bool maskIsZero = settings.interactableMask == 0;
            if (maskIsZero)
            {
                EditorGUILayout.HelpBox(
                    "Interactable Mask is set to Nothing — no objects will ever be detected.",
                    MessageType.Error);
            }

            // Warn about hold duration being very short
            if (settings.holdToInteract && settings.holdDuration < 0.15f)
            {
                EditorGUILayout.HelpBox(
                    "Hold Duration is very short (< 0.15s). Players may trigger interactions unintentionally.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(2);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
