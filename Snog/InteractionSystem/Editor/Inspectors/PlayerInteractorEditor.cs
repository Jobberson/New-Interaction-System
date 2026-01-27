using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Snog.InteractionSystem.Editor
{
    [CustomEditor(typeof(PlayerInteractor))]
    public class PlayerInteractorEditor : UnityEditor.Editor
    {
        private SerializedProperty spCamera;
        private SerializedProperty spPromptUI;
        private SerializedProperty spSettings;

        // Reflection handles
        private FieldInfo fCurrent;
        private FieldInfo fHoldTimer;

        private FieldInfo fCam;
        private FieldInfo fSettings;

        private Type tIInteractable;

        private void OnEnable()
        {
            spCamera = serializedObject.FindProperty("playerCamera");
            spPromptUI = serializedObject.FindProperty("promptUI");
            spSettings = serializedObject.FindProperty("settings");

            var t = typeof(PlayerInteractor);
            fCurrent = t.GetField("current", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            fHoldTimer = t.GetField("holdTimer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            fCam = t.GetField("playerCamera", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            fSettings = t.GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // IInteractable type
            tIInteractable = FindTypeByName("IInteractable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spCamera);
            EditorGUILayout.PropertyField(spPromptUI);
            EditorGUILayout.PropertyField(spSettings);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Live Status shows while the game is running.", MessageType.Info);

            if (Application.isPlaying)
            {
                DrawLiveStatus();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLiveStatus()
        {
            var it = (PlayerInteractor)target;

            object currentObj = fCurrent?.GetValue(it);
            var cam = fCam?.GetValue(it) as Camera;
            var settingsObj = fSettings?.GetValue(it) as ScriptableObject;
            float holdTimer = fHoldTimer != null ? (float)fHoldTimer.GetValue(it) : 0f;

            string focusedName = "—";
            float distance = 0f;
            bool canInteract = false;
            float holdPct = 0f;

            if (currentObj != null)
            {
                var comp = currentObj as Component;
                if (comp != null)
                {
                    focusedName = comp.gameObject.name;
                    if (cam != null)
                    {
                        distance = Vector3.Distance(cam.transform.position, comp.transform.position);
                    }

                    // Call CanInteract via reflection (current.CanInteract(interactorGO))
                    var canMethod = currentObj.GetType().GetMethod("CanInteract");
                    if (canMethod != null)
                    {
                        canInteract = (bool)canMethod.Invoke(currentObj, new object[] { it.gameObject });
                    }
                }
            }

            // Read settings values via SerializedObject to be safe
            float holdDuration = 0.5f;
            bool holdToInteract = false;
            if (spSettings != null && spSettings.objectReferenceValue != null)
            {
                var so = new SerializedObject(spSettings.objectReferenceValue);
                var pHold = so.FindProperty("holdToInteract");
                var pDur = so.FindProperty("holdDuration");
                if (pHold != null) holdToInteract = pHold.boolValue;
                if (pDur != null) holdDuration = pDur.floatValue;
            }

            if (holdToInteract && holdDuration > 0f)
            {
                holdPct = Mathf.Clamp01(holdTimer / holdDuration);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Focused", focusedName);
                EditorGUILayout.LabelField("Distance", distance.ToString("0.00") + " m");
                EditorGUILayout.LabelField("Can Interact", canInteract ? "Yes" : "No");
                if (holdToInteract)
                {
                    EditorGUILayout.LabelField("Hold", $"{(holdPct * 100f):0}%");
                }
            }
        }

        private static Type FindTypeByName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        // Scene gizmos
        private void OnSceneGUI()
        {
            var it = (PlayerInteractor)target;

            var cam = fCam?.GetValue(it) as Camera;
            var settingsObj = spSettings?.objectReferenceValue as ScriptableObject;

            if (cam == null || settingsObj == null)
                return;

            var so = new SerializedObject(settingsObj);
            float distance = so.FindProperty("interactDistance")?.floatValue ?? 3.0f;
            float radius = so.FindProperty("sphereRadius")?.floatValue ?? 0.05f;

            var origin = cam.transform.position;
            var forward = cam.transform.forward;
            var end = origin + forward * distance;

            Handles.color = new Color(0.2f, 1f, 0.6f, 0.8f);
            Handles.DrawLine(origin, end, 2f);

            // Draw a few discs to suggest the SphereCast radius
            Handles.color = new Color(0.2f, 1f, 0.6f, 0.3f);
            for (int i = 1; i <= 6; i++)
            {
                float t = i / 6f;
                var p = Vector3.Lerp(origin, end, t);
                Handles.DrawWireDisc(p, cam.transform.right, radius);
                Handles.DrawWireDisc(p, cam.transform.up, radius);
            }

            // Draw current focus line if any
            object currentObj = fCurrent?.GetValue(it);
            var comp = currentObj as Component;
            if (comp != null)
            {
                Handles.color = new Color(1f, 0.8f, 0.2f, 0.9f);
                Handles.DrawLine(cam.transform.position, comp.transform.position, 2f);
                Handles.SphereHandleCap(0, comp.transform.position, Quaternion.identity, 0.06f, EventType.Repaint);
                Handles.Label(comp.transform.position + Vector3.up * 0.15f, "FOCUSED");
            }
        }
    }
}