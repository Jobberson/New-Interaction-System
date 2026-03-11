using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Runtime.Core
{
    /// <summary>
    /// Main driver. Attach to your player GameObject.
    /// Each frame: detects the nearest interactable, drives focus/defocus, updates the prompt UI,
    /// and dispatches interactions on press or hold.
    ///
    /// <para><b>Extending detection:</b> Assign any MonoBehaviour that implements
    /// <see cref="IInteractionDetector"/> to the "Detector" slot. Built-in options are
    /// RaycastDetector and ProximityDetector. If left empty, the built-in SphereCast is used.</para>
    ///
    /// <para><b>Extending the UI:</b> Assign any MonoBehaviour that implements
    /// <see cref="IPromptDisplay"/> to the "Prompt Display" slot. InteractionPromptUI is the
    /// built-in implementation.</para>
    ///
    /// <para><b>Reacting to events:</b> Subscribe to <see cref="OnFocusChanged"/> and
    /// <see cref="OnInteracted"/> for zero-coupling integration with quest systems,
    /// audio managers, tutorials, and achievement trackers.</para>
    /// </summary>
    [AddComponentMenu("Interaction/Player Interactor")]
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera used for built-in SphereCast detection. " +
                 "Not needed when a custom IInteractionDetector is assigned.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("MonoBehaviour that implements IPromptDisplay. " +
                 "Assign an InteractionPromptUI or any custom prompt component.")]
        [SerializeField] private MonoBehaviour promptDisplaySource;

        [SerializeField] private InteractionSettings settings;

        [Header("Custom Detector (optional)")]
        [Tooltip("MonoBehaviour that implements IInteractionDetector. " +
                 "Leave empty to use the built-in forward SphereCast.")]
        [SerializeField] private MonoBehaviour detectorSource;

#if ENABLE_INPUT_SYSTEM
        [Header("Input (New Input System)")]
        [Tooltip("Assign an InputActionReference to use the New Input System. " +
                 "Leave empty to fall back to the legacy KeyCode set in InteractionSettings.")]
        [SerializeField] private InputActionReference interactAction;
#endif

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the focused interactable changes (including when focus is lost).
        /// Parameters: (previousInteractable, newInteractable) — either may be null.
        ///
        /// <code>
        /// playerInteractor.OnFocusChanged += (prev, next) =>
        /// {
        ///     if (next != null) Debug.Log("Now looking at: " + (next as Component)?.name);
        /// };
        /// </code>
        /// </summary>
        public event Action<IInteractable, IInteractable> OnFocusChanged;

        /// <summary>
        /// Fired immediately after an interaction completes (after the interactable's
        /// Interact() method returns).
        ///
        /// <code>
        /// playerInteractor.OnInteracted += interactable =>
        ///     questManager.NotifyInteraction(interactable);
        /// </code>
        /// </summary>
        public event Action<IInteractable> OnInteracted;

        // ── Private state ─────────────────────────────────────────────────────────

        private IInteractable       current;
        private float               holdTimer;
        private IPromptDisplay      promptDisplay;
        private IInteractionDetector customDetector;

        // ── Public read-only state ────────────────────────────────────────────────

        /// <summary>The interactable currently in focus, or null.</summary>
        public IInteractable CurrentInteractable => current;

        /// <summary>Seconds the interact button has been held this cycle.</summary>
        public float HoldTimer => holdTimer;

        /// <summary>The active InteractionSettings asset.</summary>
        public InteractionSettings Settings => settings;

        /// <summary>The camera used for built-in detection.</summary>
        public Camera PlayerCamera => playerCamera;

        // ──────────────────────────────────────────────────────────────────────────

        private void Reset() { playerCamera = Camera.main; }

        private void Awake()
        {
            promptDisplay  = promptDisplaySource  as IPromptDisplay;
            customDetector = detectorSource       as IInteractionDetector;

            if (promptDisplaySource != null && promptDisplay == null)
                Debug.LogWarning($"[PlayerInteractor] '{promptDisplaySource.GetType().Name}' does not implement IPromptDisplay.", this);

            if (detectorSource != null && customDetector == null)
                Debug.LogWarning($"[PlayerInteractor] '{detectorSource.GetType().Name}' does not implement IInteractionDetector.", this);
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            interactAction?.action?.Enable();
#endif
        }

        private void OnDisable()
        {
            if (current != null)
            {
                var prev = current;
                current.OnDefocus(gameObject);
                current = null;
                OnFocusChanged?.Invoke(prev, null);
            }
            promptDisplay?.Hide();
            holdTimer = 0f;

#if ENABLE_INPUT_SYSTEM
            interactAction?.action?.Disable();
#endif
        }

        private void Update()
        {
            if (settings == null) return;
            if (customDetector == null && playerCamera == null) return;
            UpdateFocus();
            UpdateInteraction();
        }

        // ── Focus ─────────────────────────────────────────────────────────────────

        private void UpdateFocus()
        {
            IInteractable found = customDetector != null
                ? customDetector.FindInteractable()
                : BuiltInSphereCast();

            if (!ReferenceEquals(found, current))
            {
                var prev = current;
                current?.OnDefocus(gameObject);
                current = found;
                current?.OnFocus(gameObject);
                OnFocusChanged?.Invoke(prev, current);
            }

            if (current == null)
            {
                promptDisplay?.Hide();
                holdTimer = 0f;
                return;
            }

            bool canInteract = current.CanInteract(gameObject);

            string label             = current.GetInteractionPrompt();
            bool   showWhenUnavail   = false;
            bool   isFullSentence    = false;
            Sprite availableIcon     = null;
            Sprite unavailableIcon   = null;

            if (current is ICustomPrompt customPrompt)
            {
                InteractionPromptData data = customPrompt.GetPromptData();
                showWhenUnavail  = data.showWhenUnavailable;
                isFullSentence   = data.isFullSentence;
                label            = canInteract ? data.label : data.unavailableLabel;

                Sprite fallback  = data.icon;
                availableIcon    = data.availableIcon   ?? fallback ?? data.unavailableIcon;
                unavailableIcon  = data.unavailableIcon ?? fallback ?? data.availableIcon;
            }

            if (!canInteract && !showWhenUnavail)
            {
                promptDisplay?.Hide();
                holdTimer = 0f;
                return;
            }

            promptDisplay?.SetCrosshair(canInteract ? availableIcon : unavailableIcon, canInteract);

            bool useHold = ResolveUseHold();
            string message = isFullSentence
                ? label
                : string.Format(
                    useHold ? settings.holdPromptFormat : settings.pressPromptFormat,
                    GetPromptKeyGlyph(),
                    label);

            promptDisplay?.Show(message);
        }

        // ── Interaction ───────────────────────────────────────────────────────────

        private void UpdateInteraction()
        {
            if (current == null || !current.CanInteract(gameObject)) return;

            bool  useHold       = ResolveUseHold();
            float effectiveHold = settings.holdDuration;

            if (current is IInteractableMode modeProvider
                && modeProvider.TryGetHoldDuration(out float customHold))
            {
                effectiveHold = customHold;
            }

            if (useHold)
            {
                if (IsInteractHeld())
                {
                    holdTimer += Time.deltaTime;
                    float progress = Mathf.Clamp01(holdTimer / Mathf.Max(0.0001f, effectiveHold));
                    promptDisplay?.SetHoldProgress(progress);

                    if (holdTimer >= effectiveHold)
                    {
                        var interacted = current;
                        current.Interact(gameObject);
                        holdTimer = 0f;
                        promptDisplay?.SetHoldProgress(0f);
                        OnInteracted?.Invoke(interacted);
                    }
                }
                else
                {
                    holdTimer = 0f;
                    promptDisplay?.SetHoldProgress(0f);
                }
            }
            else
            {
                if (WasInteractPressedThisFrame())
                {
                    var interacted = current;
                    current.Interact(gameObject);
                    promptDisplay?.SetHoldProgress(0f);
                    OnInteracted?.Invoke(interacted);
                }
            }
        }

        // ── Detection ─────────────────────────────────────────────────────────────

        private IInteractable BuiltInSphereCast()
        {
            if (playerCamera == null || settings == null) return null;

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            bool didHit = Physics.SphereCast(
                ray,
                settings.sphereRadius,
                out RaycastHit hit,
                settings.interactDistance,
                settings.interactableMask,
                settings.triggerInteraction);

            return didHit ? hit.collider.GetComponentInParent<IInteractable>() : null;
        }

        private bool ResolveUseHold()
        {
            bool useHold = settings != null && settings.holdToInteract;
            if (current is IInteractableMode modeProvider)
            {
                var mode = modeProvider.GetInteractionMode();
                if (mode == InteractionMode.Press) useHold = false;
                else if (mode == InteractionMode.Hold) useHold = true;
            }
            return useHold;
        }

        // ── Input abstraction ─────────────────────────────────────────────────────

        private bool WasInteractPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (HasValidInputAction()) return interactAction.action.triggered;
#endif
            return Input.GetKeyDown(settings.interactKey);
        }

        private bool IsInteractHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (HasValidInputAction()) return interactAction.action.IsPressed();
#endif
            return Input.GetKey(settings.interactKey);
        }

        private string GetPromptKeyGlyph()
        {
#if ENABLE_INPUT_SYSTEM
            if (HasValidInputAction()) return interactAction.action.GetBindingDisplayString();
#endif
            return settings?.interactKey.ToString() ?? "E";
        }

#if ENABLE_INPUT_SYSTEM
        private bool HasValidInputAction() =>
            interactAction != null && interactAction.action != null;
#endif
    }
}
