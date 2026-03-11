using UnityEngine;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Runtime.Debug
{
    /// <summary>
    /// Runtime debug HUD. Attach to the same GameObject as PlayerInteractor.
    /// Shows the focused object name, distance, CanInteract state, and hold progress.
    ///
    /// Safe to leave in builds. Toggle visibility with F9 (configurable).
    /// Tip: disable or remove this component before your final release build.
    /// </summary>
    [AddComponentMenu("Interaction/Interaction Debug Overlay")]
    [RequireComponent(typeof(Core.PlayerInteractor))]
    public class InteractionDebugOverlay : MonoBehaviour
    {
        [SerializeField] private Core.PlayerInteractor interactor;

        [Tooltip("Whether the overlay is currently visible.")]
        [SerializeField] private bool showOverlay = true;

        [Tooltip("Key that toggles the overlay on and off.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;

        private GUIStyle labelStyle;

        private void Reset()
        {
            interactor = GetComponent<Core.PlayerInteractor>();
        }

        private void Awake()
        {
            if (interactor == null)
                interactor = GetComponent<Core.PlayerInteractor>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                showOverlay = !showOverlay;
        }

        private void OnGUI()
        {
            if (!showOverlay || interactor == null) return;

            IInteractable current  = interactor.CurrentInteractable;
            var           settings = interactor.Settings;
            Camera        cam      = interactor.PlayerCamera;

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
                canInteract = current.CanInteract(interactor.gameObject);
            }

            bool  holdMode    = settings != null && settings.holdToInteract;
            float holdDur     = settings != null ? settings.holdDuration : 0.5f;
            float holdPct     = holdMode && holdDur > 0f
                ? Mathf.Clamp01(interactor.HoldTimer / holdDur)
                : 0f;

            EnsureStyle();

            int   lines  = holdMode ? 6 : 5;
            float panelH = lines * 20f + 12f;
            var   r      = new Rect(12, 12, 300, panelH);

            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.Box(r, GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(r);
            GUILayout.Label("<b>Interaction Debug</b>", labelStyle);
            GUILayout.Label($"Focused:      <b>{focusedName}</b>", labelStyle);
            GUILayout.Label($"Distance:     <b>{distance:0.00} m</b>", labelStyle);
            GUILayout.Label($"Can Interact: <b>{(canInteract ? "<color=#55ff55>Yes</color>" : "<color=#ff5555>No</color>")}</b>", labelStyle);
            if (holdMode)
                GUILayout.Label($"Hold:         <b>{holdPct * 100f:0}%</b>", labelStyle);
            GUILayout.Label($"<color=#888888>[{toggleKey}] toggle overlay</color>", labelStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyle()
        {
            if (labelStyle != null) return;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 12,
                normal   = { textColor = Color.white },
            };
        }
    }
}
