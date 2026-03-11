using UnityEditor;
using UnityEngine;
using Snog.InteractionSystem.Runtime.Core;
using Snog.InteractionSystem.Runtime.Debug;
using Snog.InteractionSystem.Runtime.Detection;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Editor.Inspectors
{
    [CustomEditor(typeof(PlayerInteractor))]
    public class PlayerInteractorEditor : UnityEditor.Editor
    {
        private SerializedProperty spCamera;
        private SerializedProperty spPromptDisplay;
        private SerializedProperty spSettings;
        private SerializedProperty spDetector;

        private void OnEnable()
        {
            spCamera        = serializedObject.FindProperty("playerCamera");
            spPromptDisplay = serializedObject.FindProperty("promptDisplaySource");
            spSettings      = serializedObject.FindProperty("settings");
            spDetector      = serializedObject.FindProperty("detectorSource");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── References ─────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spCamera);
            EditorGUILayout.PropertyField(spPromptDisplay, new GUIContent(
                "Prompt Display",
                "A MonoBehaviour that implements IPromptDisplay. " +
                "Assign an InteractionPromptUI or your own custom implementation."));
            EditorGUILayout.PropertyField(spSettings);

            // Validate prompt display
            var promptComp = spPromptDisplay.objectReferenceValue as MonoBehaviour;
            if (promptComp != null && !(promptComp is IPromptDisplay))
            {
                EditorGUILayout.HelpBox(
                    $"'{promptComp.GetType().Name}' does not implement IPromptDisplay. " +
                    "The prompt will not work.",
                    MessageType.Error);
            }

            // Warn when settings missing
            if (spSettings.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "No InteractionSettings assigned.\n" +
                    "Create one via: Assets › Create › Snog › InteractionSystem › Settings",
                    MessageType.Warning);
            }

            // Draw remaining serialized fields (e.g. InputActionReference if New Input System is active)
            DrawRemainingFields();

            EditorGUILayout.Space(6);

            // ── Custom Detector ────────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Detection", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(spDetector, new GUIContent(
                    "Detector (optional)",
                    "A MonoBehaviour that implements IInteractionDetector. " +
                    "Leave empty to use the built-in forward SphereCast."));

                var detectorComp = spDetector.objectReferenceValue as MonoBehaviour;
                if (detectorComp != null && !(detectorComp is IInteractionDetector))
                {
                    EditorGUILayout.HelpBox(
                        $"'{detectorComp.GetType().Name}' does not implement IInteractionDetector.",
                        MessageType.Error);
                }

                var it = (PlayerInteractor)target;

                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool hasRaycast  = it.GetComponent<RaycastDetector>()  != null;
                    bool hasProximity = it.GetComponent<ProximityDetector>() != null;

                    using (new EditorGUI.DisabledScope(hasRaycast))
                    {
                        if (GUILayout.Button(hasRaycast ? "Raycast Detector added" : "+ Raycast Detector"))
                        {
                            var d = Undo.AddComponent<RaycastDetector>(it.gameObject);
                            spDetector.objectReferenceValue = d;
                        }
                    }
                    using (new EditorGUI.DisabledScope(hasProximity))
                    {
                        if (GUILayout.Button(hasProximity ? "Proximity Detector added" : "+ Proximity Detector"))
                        {
                            var d = Undo.AddComponent<ProximityDetector>(it.gameObject);
                            spDetector.objectReferenceValue = d;
                        }
                    }
                }
            }

            EditorGUILayout.Space(4);

            // ── Debug Tools ────────────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
                var it = (PlayerInteractor)target;
                bool hasOverlay = it.GetComponent<InteractionDebugOverlay>() != null;

                using (new EditorGUI.DisabledScope(hasOverlay))
                {
                    if (GUILayout.Button(hasOverlay
                            ? "Debug Overlay already added"
                            : "Add Debug Overlay (Runtime HUD)"))
                    {
                        Undo.AddComponent<InteractionDebugOverlay>(it.gameObject);
                    }
                }
            }

            // ── Live status ────────────────────────────────────────────────────────
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                DrawLiveStatus();
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Live Status panel appears during Play Mode.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRemainingFields()
        {
            var it    = serializedObject.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                string p = it.propertyPath;
                if (p == "m_Script"           ||
                    p == "playerCamera"        ||
                    p == "promptDisplaySource" ||
                    p == "settings"            ||
                    p == "detectorSource") continue;
                EditorGUILayout.PropertyField(it, true);
            }
        }

        private void DrawLiveStatus()
        {
            var it       = (PlayerInteractor)target;
            var current  = it.CurrentInteractable;
            var cam      = it.PlayerCamera;
            var settings = it.Settings;

            string focusedName = "—";
            float  distance    = 0f;
            bool   canInteract = false;

            if (current != null)
            {
                var comp = current as Component;
                if (comp != null)
                {
                    focusedName = comp.gameObject.name;
                    if (cam != null)
                        distance = Vector3.Distance(cam.transform.position, comp.transform.position);
                }
                canInteract = current.CanInteract(it.gameObject);
            }

            bool  holdMode = settings != null && settings.holdToInteract;
            float holdDur  = settings != null ? settings.holdDuration : 0.5f;
            float holdPct  = holdMode && holdDur > 0f ? Mathf.Clamp01(it.HoldTimer / holdDur) : 0f;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Focused",       focusedName);
                EditorGUILayout.LabelField("Distance",      $"{distance:0.00} m");
                EditorGUILayout.LabelField("Can Interact",  canInteract ? "Yes" : "No");
                if (holdMode)
                    EditorGUILayout.LabelField("Hold",      $"{holdPct * 100f:0}%");
            }

            Repaint();
        }

        // ── Scene gizmos ───────────────────────────────────────────────────────────

        private void OnSceneGUI()
        {
            var it       = (PlayerInteractor)target;
            var cam      = it.PlayerCamera;
            var settings = it.Settings;
            if (cam == null || settings == null) return;

            float distance = settings.interactDistance;
            float radius   = settings.sphereRadius;

            var origin  = cam.transform.position;
            var forward = cam.transform.forward;
            var end     = origin + forward * distance;

            Handles.color = new Color(0.2f, 1f, 0.6f, 0.85f);
            Handles.DrawLine(origin, end, 2f);

            Handles.color = new Color(0.2f, 1f, 0.6f, 0.25f);
            for (int i = 1; i <= 5; i++)
            {
                float t = i / 5f;
                var   p = Vector3.Lerp(origin, end, t);
                Handles.DrawWireDisc(p, cam.transform.right, radius);
                Handles.DrawWireDisc(p, cam.transform.up, radius);
            }

            var comp = it.CurrentInteractable as Component;
            if (comp != null)
            {
                Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                Handles.DrawLine(cam.transform.position, comp.transform.position, 2f);
                Handles.SphereHandleCap(0, comp.transform.position, Quaternion.identity, 0.07f, EventType.Repaint);
                Handles.Label(comp.transform.position + Vector3.up * 0.18f, "FOCUSED");
            }
        }
    }
}
