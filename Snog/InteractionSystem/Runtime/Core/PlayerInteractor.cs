using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Snog.InteractionSystem.Runtime.Core;
using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Helpers;       // BaseInteractable
using Snog.InteractionSystem.Runtime.Interfaces;   // IInteractable, ICustomPrompt
using Snog.InteractionSystem.Scripts.Data;         // InteractionMode (consider moving to Runtime.Data for consistency)

namespace Snog.InteractionSystem.Runtime.Core
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private InteractionPromptUI promptUI;
        [SerializeField] private InteractionSettings settings;

#if ENABLE_INPUT_SYSTEM
        [Header("Input (New Input System)")]
        [Tooltip("If assigned, uses this action for interaction; otherwise falls back to Legacy Input (KeyCode).")]
        [SerializeField] private InputActionReference interactAction;
#endif

        [Header("Advanced (Optional)")]
        [Tooltip("If provided, overrides how prompts/crosshair are rendered. Leave empty to use the built-in UI.")]
        [SerializeField] private MonoBehaviour promptRendererOverride; // must implement IPromptRenderer

        private IInteractable current;
        private float holdTimer = 0f;

        private IPromptRenderer PromptRenderer
        {
            get
            {
                // If a custom renderer is assigned and valid, use it; otherwise adapt to the built-in InteractionPromptUI.
                if (promptRendererOverride is IPromptRenderer custom)
                {
                    return custom;
                }
                return builtInRenderer ?? (builtInRenderer = new BuiltInPromptRenderer(promptUI));
            }
        }

        private BuiltInPromptRenderer builtInRenderer;

        private void Reset()
        {
            playerCamera = Camera.main;
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.Enable();
            }
#endif
        }

        private void OnDisable()
        {
            if (current != null)
            {
                current.OnDefocus(gameObject);
                current = null;
            }
            PromptRenderer.Hide();
            holdTimer = 0f;

#if ENABLE_INPUT_SYSTEM
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.Disable();
            }
#endif
        }

        private void Update()
        {
            if (playerCamera == null || settings == null)
            {
                return;
            }

            UpdateFocus();
            UpdateUIAndInteraction();
        }

        #region Focus & Detection

        private void UpdateFocus()
        {
            IInteractable found = FindInteractableInView();
            if (!ReferenceEquals(found, current))
            {
                if (current != null)
                {
                    current.OnDefocus(gameObject);
                }

                current = found;

                if (current != null)
                {
                    current.OnFocus(gameObject);
                }
            }
        }

        private IInteractable FindInteractableInView()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(
                ray,
                settings.sphereRadius,
                out RaycastHit hit,
                settings.interactDistance,
                settings.interactableMask,
                QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<IInteractable>();
            }
            return null;
        }

        #endregion

        #region UI & Interaction

        private void UpdateUIAndInteraction()
        {
            if (current == null)
            {
                PromptRenderer.Hide();
                PromptRenderer.SetHoldProgress(0f);
                PromptRenderer.ResetCrosshair();
                return;
            }

            bool canInteract = current.CanInteract(gameObject);

            // Resolve prompt data (default + optional custom)
            string label = current.GetInteractionPrompt();
            bool showWhenUnavailable = false;
            bool isFullSentence = false;
            Sprite availableIcon = null;
            Sprite unavailableIcon = null;

            if (current is ICustomPrompt customPrompt)
            {
                var data = customPrompt.GetPromptData();
                showWhenUnavailable = data.showWhenUnavailable;
                isFullSentence = data.isFullSentence;
                label = canInteract ? data.label : data.unavailableLabel;

                // Fallback logic (fixes the typo present in the old version)
                Sprite fallback = data.icon;
                availableIcon = data.availableIcon != null ? data.availableIcon : (fallback != null ? fallback : data.unavailableIcon);
                unavailableIcon = data.unavailableIcon != null ? data.unavailableIcon : (fallback != null ? fallback : data.availableIcon);
            }

            if (!canInteract && !showWhenUnavailable)
            {
                PromptRenderer.Hide();
                PromptRenderer.SetHoldProgress(0f);
                PromptRenderer.ResetCrosshair();
                return;
            }

            Sprite chosenIcon = canInteract ? availableIcon : unavailableIcon;
            PromptRenderer.SetCrosshair(chosenIcon, canInteract);

            // Determine effective interaction mode (global vs per-object override)
            bool useHold = settings.holdToInteract;
            InteractionMode mode = InteractionMode.InheritFromInteractor;

            if (current is BaseInteractable baseInt)
            {
                mode = baseInt.GetInteractionMode();
            }
            if (mode == InteractionMode.Press) useHold = false;
            else if (mode == InteractionMode.Hold) useHold = true;

            // Compose prompt
            string message = isFullSentence
                ? label
                : string.Format(useHold ? settings.holdPromptFormat : settings.pressPromptFormat, GetPromptKeyGlyph(), label);

            PromptRenderer.Show(message);

            // Execute (press/hold) and update progress
            HandleInteraction(current, useHold);
        }

        private void HandleInteraction(IInteractable target, bool useHold)
        {
            if (target == null || !target.CanInteract(gameObject))
            {
                return;
            }

            float effectiveHold = settings.holdDuration;

            if (target is BaseInteractable baseInt && baseInt.TryGetHoldDuration(out float customHold))
            {
                effectiveHold = customHold;
            }

            if (useHold)
            {
                if (IsInteractHeld())
                {
                    holdTimer += Time.deltaTime;
                    float progress = Mathf.Clamp01(holdTimer / Mathf.Max(0.0001f, effectiveHold));
                    PromptRenderer.SetHoldProgress(progress);

                    if (holdTimer >= effectiveHold)
                    {
                        target.Interact(gameObject);
                        holdTimer = 0f;
                        PromptRenderer.SetHoldProgress(0f);
                    }
                }
                else
                {
                    holdTimer = 0f;
                    PromptRenderer.SetHoldProgress(0f);
                }
            }
            else
            {
                if (WasInteractPressedThisFrame())
                {
                    target.Interact(gameObject);
                }
                PromptRenderer.SetHoldProgress(0f);
            }
        }

        #endregion

        #region Input helpers

        private bool WasInteractPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (interactAction != null && interactAction.action != null)
            {
                // For button-type actions, .triggered is true on the frame performed
                return interactAction.action.triggered;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
            return Input.GetKeyDown(settings.interactKey);
#else
            return false;
#endif
        }

        private bool IsInteractHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (interactAction != null && interactAction.action != null)
            {
                return interactAction.action.IsPressed();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
            return Input.GetKey(settings.interactKey);
#else
            return false;
#endif
        }

        private string GetPromptKeyGlyph()
        {
#if ENABLE_INPUT_SYSTEM
            if (interactAction != null && interactAction.action != null)
            {
                return interactAction.action.GetBindingDisplayString();
            }
#endif
            return settings != null ? settings.interactKey.ToString() : "E";
        }

        #endregion

        #region Built-in Prompt Renderer (uses your InteractionPromptUI)

        private sealed class BuiltInPromptRenderer : IPromptRenderer
        {
            private readonly InteractionPromptUI ui;

            public BuiltInPromptRenderer(InteractionPromptUI ui)
            {
                this.ui = ui;
            }

            public void Show(string message)
            {
                ui?.Show(message);
            }

            public void Hide()
            {
                ui?.Hide();
            }

            public void SetHoldProgress(float progress01)
            {
                ui?.SetHoldProgress(progress01);
            }

            public void SetCrosshair(Sprite sprite, bool isAvailable)
            {
                ui?.SetCrosshair(sprite, isAvailable);
            }

            public void ResetCrosshair()
            {
                ui?.ResetCrosshair();
            }

            public void SetImmediate(bool visible)
            {
                ui?.SetImmediate(visible);
            }
        }

        #endregion
    }
}