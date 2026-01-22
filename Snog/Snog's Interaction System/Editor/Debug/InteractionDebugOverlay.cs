using System;
using System.Reflection;
using UnityEngine;

namespace Snog.InteractionSystem.Editor.Debug
{
    public class InteractionDebugOverlay : MonoBehaviour
    {
        public PlayerInteractor interactor;
        public bool showOverlay = true;
        public KeyCode toggleKey = KeyCode.F9;

        private FieldInfo fCurrent;
        private FieldInfo fHoldTimer;
        private FieldInfo fCam;
        private FieldInfo fSettings;

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = GetComponent<PlayerInteractor>();
            }

            var t = typeof(PlayerInteractor);
            fCurrent = t.GetField("current", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            fHoldTimer = t.GetField("holdTimer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            fCam = t.GetField("playerCamera", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            fSettings = t.GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showOverlay = !showOverlay;
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || interactor == null || !Application.isPlaying)
                return;

            var cam = fCam?.GetValue(interactor) as Camera;
            var settingsObj = fSettings?.GetValue(interactor) as ScriptableObject;

            object currentObj = fCurrent?.GetValue(interactor);
            float holdTimer = fHoldTimer != null ? (float)fHoldTimer.GetValue(interactor) : 0f;

            string focusedName = "—";
            float distance = 0f;
            bool canInteract = false;
            float holdPct = 0f;
            bool holdMode = false;
            float holdDuration = 0.5f;

            if (settingsObj != null)
            {
                var so = new UnityEditor.SerializedObject(settingsObj);
                holdMode = so.FindProperty("holdToInteract")?.boolValue ?? false;
                holdDuration = so.FindProperty("holdDuration")?.floatValue ?? 0.5f;
            }

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

                    var canMethod = currentObj.GetType().GetMethod("CanInteract");
                    if (canMethod != null)
                    {
                        canInteract = (bool)canMethod.Invoke(currentObj, new object[] { interactor.gameObject });
                    }
                }
            }

            if (holdMode && holdDuration > 0f)
            {
                holdPct = Mathf.Clamp01(holdTimer / holdDuration);
            }

            // Simple panel
            var r = new Rect(12, 12, 320, 108);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(r, GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(r);
            GUILayout.Label("<b>Interaction Debug</b>", GetRichLabel());
            GUILayout.Label($"Focused: <b>{focusedName}</b>", GetRichLabel());
            GUILayout.Label($"Distance: <b>{distance:0.00} m</b>", GetRichLabel());
            GUILayout.Label($"CanInteract: <b>{(canInteract ? "Yes" : "No")}</b>", GetRichLabel());
            if (holdMode)
            {
                GUILayout.Label($"Hold: <b>{holdPct * 100f:0}%</b>", GetRichLabel());
            }
            GUILayout.EndArea();
        }

        private GUIStyle GetRichLabel()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.richText = true;
            s.normal.textColor = Color.white;
            return s;
        }
    }
}